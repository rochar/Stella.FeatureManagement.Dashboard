using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Service for applying feature flag options to the database.
/// </summary>
internal sealed class DashboardInitializer(FeatureFlagDbContext context, ILogger<DashboardInitializer> logger)
    : IDashboardInitializer
{
    /// <inheritdoc />
    public async Task ApplyDashboardOptionsAsync(DashboardOptions options, CancellationToken cancellationToken = default)
    {
        await DeleteFeatureAsync(options, cancellationToken);

        await AddFeaturesIfNotExists(options, cancellationToken);

        await AddOrUpdateFeatures(options, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
    public async Task RunMigrationsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying Feature Management Dashboard database migrations...");
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Feature Management Dashboard database migrations applied successfully.");
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