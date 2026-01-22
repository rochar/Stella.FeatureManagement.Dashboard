using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stella.FeatureManagement.Dashboard.Data;
using Stella.FeatureManagement.Dashboard.Services;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class DeleteFeaturesExtension
{
    public static RouteGroupBuilder MapDeleteFeatures(this RouteGroupBuilder routeGroup)
    {
        routeGroup.MapDelete("{featureName}", async (
                string featureName,
                IFeatureChangeValidation featureChangeValidation,
                IDbContextFactory<FeatureFlagDbContext> contextFactory,
                ILogger<FeatureFlagDbContext> logger) =>
            {
                await using var context = await contextFactory.CreateDbContextAsync();
                var feature = await context.FeatureFlags
                    .Include(f => f.Filters)
                    .FirstOrDefaultAsync(f => f.Name == featureName);

                if (feature is null)
                    return Results.NotFound(new { message = $"Feature '{featureName}' not found." });

                var canProceed =
                    featureChangeValidation.CanProceed(new FeatureFlagDto(featureName, false, string.Empty, null),
                        FeatureChangeType.Delete);

                if (canProceed.Cancel)
                {
                    logger.LogWarning("Delete operation cancelled for feature {FeatureName}: {CancellationMessage}", featureName, canProceed.CancellationMessage);
                    return Results.BadRequest(canProceed.CancellationMessage);
                }
                    

                await DeleteFeature(context, feature);

                return Results.NoContent();
            })
            .Produces(204)
            .Produces(404)
            .Produces(400);

        return routeGroup;
    }

    private static async Task DeleteFeature(FeatureFlagDbContext context, FeatureFlag feature)
    {
        context.FeatureFilters.RemoveRange(feature.Filters);
        context.FeatureFlags.Remove(feature);
        await context.SaveChangesAsync();
    }
}