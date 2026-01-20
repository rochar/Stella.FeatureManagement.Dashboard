using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class GetFeaturesExtension
{
    public static RouteGroupBuilder MapGetFeatures(this RouteGroupBuilder routeGroup)
    {
        routeGroup.MapGet("", async (IDbContextFactory<FeatureFlagDbContext> contextFactory) =>
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var features = await context.FeatureFlags
                .Include(f => f.Filters)
                .Select(f => new FeatureFlagDto(
                    f.Name,
                    f.IsEnabled,
                    f.Description,
                    f.Filters.Select(filter => new FeatureFilterDto(filter.FilterType, filter.Parameters)).ToList()))
                .ToListAsync();

            return features;
        }).Produces<List<FeatureFlagDto>>(200);

        routeGroup.MapGet("{featureName}", async (string featureName, IDbContextFactory<FeatureFlagDbContext> contextFactory) =>
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var feature = await context.FeatureFlags
                .Include(f => f.Filters)
                .Where(f => f.Name == featureName)
                .Select(f => new FeatureFlagDto(
                    f.Name,
                    f.IsEnabled,
                    f.Description,
                    f.Filters.Select(filter => new FeatureFilterDto(filter.FilterType, filter.Parameters)).ToList()))
                .FirstOrDefaultAsync();

            return feature is null ? Results.NotFound() : Results.Ok(feature);

        }).Produces<FeatureFlagDto>(200)
        .Produces(404);

        return routeGroup;
    }
}

/// <summary>
/// Represents a feature flag with its configuration.
/// </summary>
/// <param name="Name">The feature flag name.</param>
/// <param name="IsEnabled">Whether the feature is enabled.</param>
/// <param name="Description">Optional description of the feature.</param>
/// <param name="Filters">The filters applied to this feature flag.</param>
internal record FeatureFlagDto(string Name, bool IsEnabled, string? Description, List<FeatureFilterDto> Filters);

/// <summary>
/// Represents a filter configuration for a feature flag.
/// </summary>
/// <param name="FilterType">The filter type (e.g., "Microsoft.Percentage", "Microsoft.TimeWindow").</param>
/// <param name="Parameters">The filter parameters as JSON.</param>
internal record FeatureFilterDto(string FilterType, string? Parameters);