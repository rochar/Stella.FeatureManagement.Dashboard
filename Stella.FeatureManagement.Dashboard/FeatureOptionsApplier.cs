using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Service for applying feature flag options to the database.
/// </summary>
public class FeatureOptionsApplier(FeatureFlagDbContext context, ILogger<FeatureOptionsApplier> logger)
    : IFeatureOptionsApplier
{
    /// <inheritdoc />
    public async Task ApplyAsync(DashboardOptions options, CancellationToken cancellationToken = default)
    {
        await DeleteFeatureAsync(options, cancellationToken);

        await AddFeaturesIfNotExists(options, cancellationToken);

        await AddOrUpdateFeatures(options, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task AddOrUpdateFeatures(DashboardOptions options, CancellationToken cancellationToken)
    {
        foreach (var (featureName, isEnabled) in options.AddOrUpdate)
        {
            var existing =
                await context.FeatureFlags.FirstOrDefaultAsync(f => f.Name == featureName, cancellationToken);
            if (existing is not null)
            {
                existing.IsEnabled = isEnabled;
                existing.UpdatedAt = DateTime.UtcNow;
                logger.LogInformation("Updating feature flag: {FeatureName} with IsEnabled={IsEnabled}", featureName,
                    isEnabled);
            }
            else
            {
                context.FeatureFlags.Add(new FeatureFlag
                {
                    Name = featureName,
                    IsEnabled = isEnabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                logger.LogInformation("Adding feature flag: {FeatureName} with IsEnabled={IsEnabled}", featureName,
                    isEnabled);
            }
        }
    }

    private async Task AddFeaturesIfNotExists(DashboardOptions options, CancellationToken cancellationToken)
    {
        foreach (var (featureName, isEnabled) in options.AddIfNotExists)
        {
            var exists = await context.FeatureFlags.AnyAsync(f => f.Name == featureName, cancellationToken);
            if (!exists)
            {
                context.FeatureFlags.Add(new FeatureFlag
                {
                    Name = featureName,
                    IsEnabled = isEnabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                logger.LogInformation("Adding feature flag: {FeatureName} with IsEnabled={IsEnabled}", featureName,
                    isEnabled);
            }
        }
    }

    private async Task DeleteFeatureAsync(DashboardOptions options, CancellationToken cancellationToken)
    {
        foreach (var featureName in options.Delete)
        {
            var existing =
                await context.FeatureFlags.FirstOrDefaultAsync(f => f.Name == featureName, cancellationToken);
            if (existing is not null)
            {
                context.FeatureFlags.Remove(existing);
                logger.LogInformation("Deleting feature flag: {FeatureName}", featureName);
            }
        }
    }
}