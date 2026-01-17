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
                .AnyAsync(f => f.Name == request.FeatureName);

            if (exists)
            {
                return Results.Conflict(new { message = $"Feature '{request.FeatureName}' already exists." });
            }

            var feature = new FeatureFlag
            {
                Name = request.FeatureName,
                IsEnabled = request.IsEnabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.FeatureFlags.Add(feature);
            await context.SaveChangesAsync();

            return Results.Created($"/features/{feature.Name}", new FeatureState(feature.Name, feature.IsEnabled));
        })
        .Produces<FeatureState>(201)
        .Produces(409);

        return routeGroup;
    }
}

internal record CreateFeatureRequest(string FeatureName, bool IsEnabled);
