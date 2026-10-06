using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard.Services;

/// <summary>
///     Aggregates feature evaluations in memory and periodically adds them to the database with an atomic upsert.
///     Each application instance flushes only its own deltas, so several instances can share one database.
/// </summary>
internal sealed partial class FeatureUsageRecorder(
    IDbContextFactory<FeatureFlagDbContext> contextFactory,
    TimeProvider timeProvider,
    ILogger<FeatureUsageRecorder> logger) : IFeatureUsageRecorder, IDisposable
{
    // Rows are ordered by (FeatureFlagId, Date) so concurrent flushes from different instances
    // lock rows in the same order and cannot deadlock each other.
    private const string UpsertSql = """
        INSERT INTO features."FeatureFlagUsages" ("FeatureFlagId", "Date", "EnabledCount", "DisabledCount")
        SELECT f."Id", u.day, u.enabled, u.disabled
        FROM unnest(@names, @days, @enabled, @disabled) AS u(name, day, enabled, disabled)
        JOIN features."FeatureFlags" f ON f."Name" = u.name
        ORDER BY f."Id", u.day
        ON CONFLICT ("FeatureFlagId", "Date") DO UPDATE SET
            "EnabledCount" = "FeatureFlagUsages"."EnabledCount" + EXCLUDED."EnabledCount",
            "DisabledCount" = "FeatureFlagUsages"."DisabledCount" + EXCLUDED."DisabledCount"
        """;

    private readonly ConcurrentDictionary<(string Name, DateOnly Day), UsageCounter> _counters = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);

    public void Record(string featureName, bool isEnabled)
    {
        var day = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var counter = _counters.GetOrAdd((featureName, day), static _ => new UsageCounter());
        if (isEnabled)
            Interlocked.Increment(ref counter.Enabled);
        else
            Interlocked.Increment(ref counter.Disabled);
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await _flushLock.WaitAsync(cancellationToken);
        try
        {
            var batch = Drain();
            if (batch.Count == 0)
            {
                LogNothingToFlush(logger);
                return;
            }

            var started = timeProvider.GetTimestamp();
            int rows;
            try
            {
                rows = await WriteAsync(batch, cancellationToken);
            }
            // A feature deleted while the statement runs fails the FK check; the retry re-reads FeatureFlags,
            // so the JOIN drops that name instead of the whole batch being lost.
            catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.DeadlockDetected
                                                   or PostgresErrorCodes.SerializationFailure
                                                   or PostgresErrorCodes.ForeignKeyViolation)
            {
                LogRetrying(logger, ex.SqlState, batch.Count);
                rows = await WriteAsync(batch, cancellationToken);
            }

            LogFlushed(logger, rows, batch.Count, batch.Sum(b => b.Enabled + b.Disabled),
                timeProvider.GetElapsedTime(started).TotalMilliseconds);
            // Rows whose name matches no stored feature are dropped by the JOIN.
            if (rows < batch.Count)
                LogUnknownFeaturesSkipped(logger, batch.Count - rows);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Usage counters are best effort: drop the batch rather than retrying forever.
            LogFlushFailed(logger, ex);
        }
        finally
        {
            _flushLock.Release();
        }
    }

    private List<UsageDelta> Drain()
    {
        var batch = new List<UsageDelta>();

        // Detach every counter so the map only holds names seen since the last flush; unknown names sent
        // to the endpoint therefore can't grow it without bound. New increments go to a fresh counter.
        // An increment on a just-detached counter after it was read is lost, which is accepted.
        // Enumerate the map directly: unlike Keys, enumeration takes no locks, so Record is never blocked.
        foreach (var (key, _) in _counters)
        {
            if (!_counters.TryRemove(key, out var counter))
                continue;

            var enabled = Interlocked.Read(ref counter.Enabled);
            var disabled = Interlocked.Read(ref counter.Disabled);
            if (enabled != 0 || disabled != 0)
                batch.Add(new UsageDelta(key.Name, key.Day, enabled, disabled));
        }

        return batch;
    }

    private async Task<int> WriteAsync(List<UsageDelta> batch, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Database.ExecuteSqlRawAsync(UpsertSql,
            [
                new NpgsqlParameter("names", NpgsqlDbType.Array | NpgsqlDbType.Text)
                    { Value = batch.Select(b => b.Name).ToArray() },
                new NpgsqlParameter("days", NpgsqlDbType.Array | NpgsqlDbType.Date)
                    { Value = batch.Select(b => b.Day).ToArray() },
                new NpgsqlParameter("enabled", NpgsqlDbType.Array | NpgsqlDbType.Bigint)
                    { Value = batch.Select(b => b.Enabled).ToArray() },
                new NpgsqlParameter("disabled", NpgsqlDbType.Array | NpgsqlDbType.Bigint)
                    { Value = batch.Select(b => b.Disabled).ToArray() }
            ],
            cancellationToken);
    }

    public void Dispose() => _flushLock.Dispose();

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to persist feature usage counters; the batch was dropped.")]
    private static partial void LogFlushFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No feature usage counters to flush.")]
    private static partial void LogNothingToFlush(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Flushed feature usage: upserted {RowCount} of {CounterCount} (feature, day) counters, " +
                  "{EvaluationCount} evaluations, in {ElapsedMs:0.0} ms.")]
    private static partial void LogFlushed(ILogger logger, int rowCount, int counterCount, long evaluationCount,
        double elapsedMs);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Skipped {SkippedCount} feature usage counters for features that don't exist in the database.")]
    private static partial void LogUnknownFeaturesSkipped(ILogger logger, int skippedCount);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Feature usage flush failed with SQL state {SqlState}; retrying {CounterCount} counters once.")]
    private static partial void LogRetrying(ILogger logger, string sqlState, int counterCount);

    private sealed record UsageDelta(string Name, DateOnly Day, long Enabled, long Disabled);

    private sealed class UsageCounter
    {
        public long Enabled;
        public long Disabled;
    }
}
