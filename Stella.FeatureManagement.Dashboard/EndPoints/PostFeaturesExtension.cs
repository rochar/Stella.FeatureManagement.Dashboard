using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stella.FeatureManagement.Dashboard.Data;
using Stella.FeatureManagement.Dashboard.Services;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class PostFeaturesExtension
{
    public static RouteGroupBuilder MapPostFeatures(this RouteGroupBuilder routeGroup)
    {
        routeGroup.MapPost("", async (
            CreateFeatureRequest request,
            IFeatureChangeValidation featureChangeValidation,
            IDbContextFactory<FeatureFlagDbContext> contextFactory,
            ILogger<FeatureFlagDbContext> logger) =>
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var exists = await context.FeatureFlags
                .AnyAsync(f => f.Name == request.Name);

            if (exists)
            {
                return Results.Conflict(new { message = $"Feature '{request.Name}' already exists." });
            }

            var canProceed = featureChangeValidation.CanProceed(request.ToDto(), FeatureChangeType.Create);

            if (canProceed.Cancel)
            {
                logger.LogWarning("Create operation cancelled for feature {FeatureName}: {CancellationMessage}", request.Name, canProceed.CancellationMessage);
                return Results.BadRequest(canProceed.CancellationMessage);
            }

            var now = FeatureFlag.UtcNowForStorage();
            var feature = new FeatureFlag
            {
                Name = request.Name,
                IsEnabled = request.IsEnabled,
                Description = request.Description,
                Application = request.Application,
                CreatedAt = now,
                UpdatedAt = now
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

            return Results.Created($"/features/{feature.Name}", feature.ToDto());
        })
        .Produces<FeatureFlagDto>(201)
        .Produces(409)
        .Produces(400);

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
/// <param name="Application">The application this feature belongs to. Defaults to "Default".</param>
internal record CreateFeatureRequest(string Name, bool IsEnabled, string? Description = null, List<FeatureFilterDto>? Filters = null, string Application = "Default")
{
    public FeatureFlagDto ToDto()
    {
        return new FeatureFlagDto(Name, IsEnabled, Description, Filters, Application);
    }
}
