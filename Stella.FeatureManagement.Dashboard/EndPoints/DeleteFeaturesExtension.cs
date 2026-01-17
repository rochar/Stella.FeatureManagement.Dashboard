using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class DeleteFeaturesExtension
{
    public static RouteGroupBuilder MapDeleteFeatures(this RouteGroupBuilder routeGroup)
    {
        routeGroup.MapDelete("{featureName}", async (
            string featureName,
            FeatureFlagDbContext context) =>
        {
            var feature = await context.FeatureFlags
                .FirstOrDefaultAsync(f => f.Name == featureName);

            if (feature is null)
            {
                return Results.NotFound(new { message = $"Feature '{featureName}' not found." });
            }

            context.FeatureFlags.Remove(feature);
            await context.SaveChangesAsync();

            return Results.NoContent();
        })
        .Produces(204)
        .Produces(404);

        return routeGroup;
    }
}
