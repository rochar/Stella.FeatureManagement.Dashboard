using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class PutFeaturesExtension
{
    public static RouteGroupBuilder MapPutFeatures(this RouteGroupBuilder routeGroup)
    {
        routeGroup.MapPut("{featureName}", async (
            string featureName,
            UpdateFeatureRequest request,
            IDbContextFactory<FeatureFlagDbContext> contextFactory) =>
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var feature = await context.FeatureFlags
                .Include(f => f.Filters)
                .FirstOrDefaultAsync(f => f.Name == featureName);

            if (feature is null)
            {
                return Results.NotFound(new { message = $"Feature '{featureName}' not found." });
            }

            feature.IsEnabled = request.IsEnabled;
            feature.Description = request.Description;
            feature.UpdatedAt = DateTime.UtcNow;

            // Clear existing filters and add new one if provided
            feature.Filters.Clear();
            if (request.Filter is not null)
            {
                feature.Filters.Add(new FeatureFilter
                {
                    FilterType = request.Filter.FilterType,
                    Parameters = request.Filter.Parameters
                });
            }

            await context.SaveChangesAsync();

            var response = new FeatureFlagDto(
                feature.Name,
                feature.IsEnabled,
                feature.Description,
                feature.Filters.Select(f => new FeatureFilterDto(f.FilterType, f.Parameters)).ToList());

            return Results.Ok(response);
        })
        .Produces<FeatureFlagDto>(200)
        .Produces(404);

        return routeGroup;
    }
}

/// <summary>
/// Request to update an existing feature flag.
/// </summary>
/// <param name="IsEnabled">Whether the feature is enabled.</param>
/// <param name="Description">Optional description of the feature.</param>
/// <param name="Filter">Optional filter configuration for the feature.</param>
internal record UpdateFeatureRequest(bool IsEnabled, string? Description = null, FeatureFilterDto? Filter = null);