using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Stella.FeatureManagement.Dashboard.Services;

/// <summary>
///     Periodically persists the counters buffered by <see cref="IFeatureUsageRecorder" />.
/// </summary>
internal sealed partial class FeatureUsageFlushService(
    IFeatureUsageRecorder recorder,
    IOptions<FeatureUsageOptions> options,
    TimeProvider timeProvider,
    ILogger<FeatureUsageFlushService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.FlushInterval;
        try
        {
            // Jitter the first tick so instances deployed together don't flush in lockstep.
#pragma warning disable CA5394 // Random is fine here: not security sensitive
            var jitter = interval * (Random.Shared.NextDouble() * 0.2);
#pragma warning restore CA5394
            LogStarted(logger, interval, interval + jitter);
            await Task.Delay(interval + jitter, timeProvider, stoppingToken);
            // Flushes are not cancelled by shutdown: a cancelled flush would drop the counters it already
            // drained, which the final flush in StopAsync can no longer see. Each write is bounded by the
            // database command timeout.
            LogFlushTick(logger);
            await recorder.FlushAsync(CancellationToken.None);

            using var timer = new PeriodicTimer(interval, timeProvider);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                LogFlushTick(logger);
                await recorder.FlushAsync(CancellationToken.None);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        // Best-effort final flush on graceful shutdown.
        LogFinalFlush(logger);
        try
        {
            await recorder.FlushAsync(cancellationToken);
            LogStopped(logger);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The shutdown timeout elapsed; unflushed counters are lost, which is accepted.
            LogFinalFlushTimedOut(logger);
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Feature usage flush service started; flushing every {FlushInterval}, first flush in {FirstFlushDelay}.")]
    private static partial void LogStarted(ILogger logger, TimeSpan flushInterval, TimeSpan firstFlushDelay);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Feature usage flush tick.")]
    private static partial void LogFlushTick(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Feature usage flush service stopping; running final flush.")]
    private static partial void LogFinalFlush(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Feature usage flush service stopped.")]
    private static partial void LogStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Final feature usage flush did not finish before the shutdown timeout; unflushed counters were lost.")]
    private static partial void LogFinalFlushTimedOut(ILogger logger);
}
