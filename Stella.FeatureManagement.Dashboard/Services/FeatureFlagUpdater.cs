using Microsoft.EntityFrameworkCore;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard.Services;

/// <summary>
/// The single write path for updating a stored feature, shared by the dashboard PUT and
/// <see cref="IFeatureFlagStore"/>: validation, the state comparison, the concurrency token and the
/// filter replacement.
/// </summary>
internal sealed class FeatureFlagUpdater(
    IFeatureChangeValidation featureChangeValidation,
    IDbContextFactory<FeatureFlagDbContext> contextFactory)
{
    /// <summary>
    /// Loads a feature for update, with its filters in <see cref="FeatureFilter.Id"/> order so the state
    /// comparison sees the same order every reader sees.
    /// </summary>
    public static Task<FeatureFlag?> LoadAsync(FeatureFlagDbContext context, string featureName,
        CancellationToken cancellationToken) =>
        context.FeatureFlags
            .Include(f => f.Filters.OrderBy(filter => filter.Id))
            .FirstOrDefaultAsync(f => f.Name == featureName, cancellationToken);

    public FeatureChangeValidationResult Validate(FeatureFlagDto requested) =>
        featureChangeValidation.CanProceed(requested, FeatureChangeType.Update);

    /// <summary>
    /// Applies <paramref name="requested"/> to <paramref name="feature"/> (loaded through
    /// <see cref="LoadAsync"/> on <paramref name="context"/>) and saves once.
    /// </summary>
    /// <param name="expectedUpdatedAt">The caller's token. Null means last-write-wins: the loaded value is
    /// used, so only a write landing between load and save conflicts.</param>
    public async Task<FeatureFlagUpdateResult> UpdateAsync(FeatureFlagDbContext context, FeatureFlag feature,
        FeatureFlagDto requested, DateTime? expectedUpdatedAt, CancellationToken cancellationToken)
    {
        var validation = Validate(requested);
        if (validation.Cancel)
            return new FeatureFlagUpdateResult
            {
                Status = FeatureFlagUpdateStatus.Rejected,
                Feature = feature.ToDto(),
                Message = validation.CancellationMessage
            };

        // Before the token on purpose: a caller whose state is already stored has lost nothing, so a
        // stale token must not turn that into a conflict.
        if (StateEquals(feature, requested))
            return new FeatureFlagUpdateResult { Status = FeatureFlagUpdateStatus.Unchanged, Feature = feature.ToDto() };

        var expected = expectedUpdatedAt ?? feature.UpdatedAt;
        context.Entry(feature).Property(f => f.UpdatedAt).OriginalValue = expected;

        var now = FeatureFlag.UtcNowForStorage();
        var next = FirstMicrosecondAfter(expected);
        feature.UpdatedAt = now > next ? now : next;
        feature.IsEnabled = requested.IsEnabled;
        feature.Description = requested.Description;
        feature.Application = requested.Application;

        feature.Filters.Clear();
        foreach (var filter in requested.Filters ?? [])
            feature.Filters.Add(new FeatureFilter { FilterType = filter.FilterType, Parameters = filter.Parameters });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return await ReadAfterConflictAsync(feature.Name, cancellationToken);
        }

        return new FeatureFlagUpdateResult { Status = FeatureFlagUpdateStatus.Updated, Feature = feature.ToDto() };
    }

    private async Task<FeatureFlagUpdateResult> ReadAfterConflictAsync(string featureName,
        CancellationToken cancellationToken)
    {
        // The failed context still tracks the rejected changes, so the current state comes from a fresh one.
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var current = await context.FeatureFlags
            .Where(f => f.Name == featureName)
            .Select(FeatureFlagMapping.AsDto)
            .FirstOrDefaultAsync(cancellationToken);

        return current is null
            ? NotFound(featureName)
            : new FeatureFlagUpdateResult
            {
                Status = FeatureFlagUpdateStatus.Conflict,
                Feature = current,
                Message = $"Feature '{featureName}' was changed by another writer; reload it and re-apply the change."
            };
    }

    public static FeatureFlagUpdateResult NotFound(string featureName) => new()
    {
        Status = FeatureFlagUpdateStatus.NotFound,
        Message = $"Feature '{featureName}' not found."
    };

    private static bool StateEquals(FeatureFlag stored, FeatureFlagDto requested)
    {
        if (stored.IsEnabled != requested.IsEnabled
            || !string.Equals(stored.Description, requested.Description, StringComparison.Ordinal)
            || !string.Equals(stored.Application, requested.Application, StringComparison.Ordinal))
            return false;

        var requestedFilters = requested.Filters ?? [];
        return stored.Filters.Count == requestedFilters.Count
               && stored.Filters.Zip(requestedFilters).All(pair =>
                   string.Equals(pair.First.FilterType, pair.Second.FilterType, StringComparison.Ordinal)
                   && string.Equals(pair.First.Parameters, pair.Second.Parameters, StringComparison.Ordinal));
    }

    // Rounds up to PostgreSQL's microsecond precision, so the value returned to the caller is exactly the
    // value later read back.
    private static DateTime FirstMicrosecondAfter(DateTime value) =>
        value.AddTicks(TimeSpan.TicksPerMicrosecond - value.Ticks % TimeSpan.TicksPerMicrosecond);
}
