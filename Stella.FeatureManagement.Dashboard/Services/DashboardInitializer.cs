using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard.Services;

/// <summary>
/// Service for applying feature flag options to the database.
/// </summary>
internal sealed class DashboardInitializer(IManagedFeatureRegistration managedFeatureRegistration, IDbContextFactory<FeatureFlagDbContext> contextFactory, ILogger<DashboardInitializer> logger)
    : IDashboardInitializer
{

    /// <inheritdoc />
    public async Task RegisterFeatureAsync(string name, string description, bool isEnabled,
        FilterOptions? filterOptions,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        if (!managedFeatureRegistration.TryAdd(name, filterOptions))
        {
            logger.LogWarning("Feature {FeatureName} is already registered", name);
            return;
        }

        var exists = await context.FeatureFlags.AnyAsync(f => f.Name == name, cancellationToken);
        if (!exists)
        {
            var featureFlag = new FeatureFlag
            {
                Name = name,
                IsEnabled = isEnabled,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (filterOptions is not null)
                featureFlag.Filters.Add(new FeatureFilter
                {
                    FilterType = filterOptions.TypeName,
                    Parameters = ToJson(filterOptions.DefaultSettings)
                });

            context.FeatureFlags.Add(featureFlag);
            logger.LogInformation("Adding feature flag: {FeatureName} with IsEnabled={IsEnabled}", name,
                isEnabled);
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException e)
            {
                logger.LogInformation(e, "Feature flag {FeatureName} already exists in the database, skipping", name);
            }

        }
    }

    public async Task RunMigrationsAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        logger.LogInformation("Applying Feature Management Dashboard database migrations...");
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Feature Management Dashboard database migrations applied successfully.");
    }

    private static string? ToJson(object? filterParameters)
    {
        return filterParameters is null ? null : System.Text.Json.JsonSerializer.Serialize(filterParameters);
    }


}