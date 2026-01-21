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
        foreach (var (featureName, definition) in options.AddOrUpdate)
        {
            var existing = await context.FeatureFlags
                .Include(f => f.Filters)
                .FirstOrDefaultAsync(f => f.Name == featureName, cancellationToken);

            if (existing is not null)
            {
                existing.IsEnabled = definition.IsEnabled;
                existing.Description = definition.Description;
                existing.UpdatedAt = DateTime.UtcNow;

                // Clear existing filters and add new one if provided
                existing.Filters.Clear();
                if (definition.Filter is not null)
                {
                    existing.Filters.Add(new FeatureFilter
                    {
                        FilterType = definition.Filter.FilterType,
                        Parameters = ToJson(definition.Filter.Parameters)
                    });
                }

                logger.LogInformation("Updating feature flag: {FeatureName} with IsEnabled={IsEnabled}", featureName,
                    definition.IsEnabled);
            }
            else
            {
                var featureFlag = new FeatureFlag
                {
                    Name = featureName,
                    IsEnabled = definition.IsEnabled,
                    Description = definition.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (definition.Filter is not null)
                {
                    featureFlag.Filters.Add(new FeatureFilter
                    {
                        FilterType = definition.Filter.FilterType,
                        Parameters = ToJson(definition.Filter.Parameters)
                    });
                }

                context.FeatureFlags.Add(featureFlag);
                logger.LogInformation("Adding feature flag: {FeatureName} with IsEnabled={IsEnabled}", featureName,
                    definition.IsEnabled);
            }
        }
    }

    private static string? ToJson(object? filterParameters)
    {
        return filterParameters is null ? null : System.Text.Json.JsonSerializer.Serialize(filterParameters);
    }

    private async Task AddFeaturesIfNotExists(DashboardOptions options, CancellationToken cancellationToken)
    {
        foreach (var (featureName, definition) in options.AddIfNotExists)
        {
            var exists = await context.FeatureFlags.AnyAsync(f => f.Name == featureName, cancellationToken);
            if (!exists)
            {
                var featureFlag = new FeatureFlag
                {
                    Name = featureName,
                    IsEnabled = definition.IsEnabled,
                    Description = definition.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (definition.Filter is not null)
                {
                    featureFlag.Filters.Add(new FeatureFilter
                    {
                        FilterType = definition.Filter.FilterType,
                        Parameters = ToJson(definition.Filter.Parameters)
                    });
                }

                context.FeatureFlags.Add(featureFlag);
                logger.LogInformation("Adding feature flag: {FeatureName} with IsEnabled={IsEnabled}", featureName,
                    definition.IsEnabled);
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