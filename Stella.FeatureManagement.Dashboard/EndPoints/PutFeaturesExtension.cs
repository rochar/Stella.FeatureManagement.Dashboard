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
                .FirstOrDefaultAsync(f => f.Name == featureName);

            if (feature is null)
            {
                return Results.NotFound(new { message = $"Feature '{featureName}' not found." });
            }

            feature.IsEnabled = request.IsEnabled;
            feature.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return Results.Ok(new FeatureState(feature.Name, feature.IsEnabled, "My feature description"));
        })
        .Produces<FeatureState>(200)
        .Produces(404);

        return routeGroup;
    }
}

internal record UpdateFeatureRequest(bool IsEnabled);