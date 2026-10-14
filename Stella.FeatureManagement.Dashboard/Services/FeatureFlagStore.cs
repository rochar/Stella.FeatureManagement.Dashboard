using Microsoft.EntityFrameworkCore;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard.Services;

internal sealed class FeatureFlagStore(
    IDbContextFactory<FeatureFlagDbContext> contextFactory,
    FeatureFlagUpdater updater) : IFeatureFlagStore
{
    public async Task<FeatureFlagDto?> FindFeatureAsync(string featureName,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.FeatureFlags
            .Where(f => f.Name == featureName)
            .Select(FeatureFlagMapping.AsDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeatureFlagDto>> ListFeaturesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.FeatureFlags
            .OrderBy(f => f.Name)
            .Select(FeatureFlagMapping.AsDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<FeatureFlagUpdateResult> UpdateFeatureAsync(FeatureFlagDto feature,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(feature);
        if (feature.UpdatedAt is not { Kind: DateTimeKind.Utc } expectedUpdatedAt)
            throw new ArgumentException(
                $"{nameof(FeatureFlagDto.UpdatedAt)} is the concurrency token and must be a UTC value read from the store.",
                nameof(feature));
        if (feature.Filters is null)
            throw new ArgumentException(
                $"{nameof(FeatureFlagDto.Filters)} must not be null; pass an empty list to remove every filter.",
                nameof(feature));

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var stored = await FeatureFlagUpdater.LoadAsync(context, feature.Name, cancellationToken);

        return stored is null
            ? FeatureFlagUpdater.NotFound(feature.Name)
            : await updater.UpdateAsync(context, stored, feature, expectedUpdatedAt, cancellationToken);
    }

    public FeatureChangeValidationResult ValidateFeature(FeatureFlagDto feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        return updater.Validate(feature);
    }
}
