using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class PostFeaturesExtension
{
    public static RouteGroupBuilder MapPostFeatures(this RouteGroupBuilder routeGroup)
    {
        routeGroup.MapPost("", async (
            CreateFeatureRequest request,
            IDbContextFactory<FeatureFlagDbContext> contextFactory) =>
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var exists = await context.FeatureFlags
                .AnyAsync(f => f.Name == request.Name);

            if (exists)
            {
                return Results.Conflict(new { message = $"Feature '{request.Name}' already exists." });
            }

            var feature = new FeatureFlag
            {
                Name = request.Name,
                IsEnabled = request.IsEnabled,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (request.Filters is not null)
            {
                foreach (var filter in request.Filters)
                {
                    feature.Filters.Add(new FeatureFilter
                    {
                        FilterType = filter.FilterType,
                        Parameters = filter.Parameters
                    });
                }
            }

            context.FeatureFlags.Add(feature);
            await context.SaveChangesAsync();

            var response = new FeatureFlagDto(
                feature.Name,
                feature.IsEnabled,
                feature.Description,
                feature.Filters.Select(f => new FeatureFilterDto(f.FilterType, f.Parameters)).ToList());

            return Results.Created($"/features/{feature.Name}", response);
        })
        .Produces<FeatureFlagDto>(201)
        .Produces(409);

        return routeGroup;
    }
}

/// <summary>
/// Request to create a new feature flag.
/// </summary>
/// <param name="Name">The feature flag name.</param>
/// <param name="IsEnabled">Whether the feature is enabled.</param>
/// <param name="Description">Optional description of the feature.</param>
/// <param name="Filters">Optional filter configurations for the feature.</param>
internal record CreateFeatureRequest(string Name, bool IsEnabled, string? Description = null, List<FeatureFilterDto>? Filters = null);
