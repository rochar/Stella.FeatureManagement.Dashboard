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
                .Select(f => new FeatureState(f.Name, f.IsEnabled))
                .ToListAsync();

            return features;
        }).Produces<List<FeatureState>>(200);

        routeGroup.MapGet("{featureName}", async (string featureName, IDbContextFactory<FeatureFlagDbContext> contextFactory) =>
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var feature = await context.FeatureFlags
                .FirstOrDefaultAsync(f => f.Name == featureName);

            return feature?.IsEnabled ?? false;
        }).Produces<bool>(200);

        return routeGroup;
    }
}

internal record FeatureState(string FeatureName, bool IsEnabled);