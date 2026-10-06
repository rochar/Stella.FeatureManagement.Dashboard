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
                .Select(FeatureFlagMapping.AsDto)
                .ToListAsync();

            return features;
        }).Produces<List<FeatureFlagDto>>(200);

        routeGroup.MapGet("{featureName}", async (string featureName, IDbContextFactory<FeatureFlagDbContext> contextFactory) =>
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var feature = await context.FeatureFlags
                .Where(f => f.Name == featureName)
                .Select(FeatureFlagMapping.AsDto)
                .FirstOrDefaultAsync();

            return feature is null ? Results.NotFound() : Results.Ok(feature);

        }).Produces<FeatureFlagDto>(200)
        .Produces(404);

        return routeGroup;
    }
}
