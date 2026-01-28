using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stella.FeatureManagement.Dashboard.Data;
using Stella.FeatureManagement.Dashboard.Services;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class PutFeaturesExtension
{
    public static RouteGroupBuilder MapPutFeatures(this RouteGroupBuilder routeGroup)
    {
        routeGroup.MapPut("{featureName}", async (
                string featureName,
                UpdateFeatureRequest request,
                IFeatureChangeValidation featureChangeValidation,
                IDbContextFactory<FeatureFlagDbContext> contextFactory,
                ILogger<FeatureFlagDbContext> logger) =>
            {
                logger.LogInformation("Received update request for feature {FeatureName}", featureName);

                await using var context = await contextFactory.CreateDbContextAsync();

                var feature = await context.FeatureFlags
                    .Include(f => f.Filters)
                    .FirstOrDefaultAsync(f => f.Name == featureName);

                if (feature is null)
                {
                    logger.LogWarning("Feature {FeatureName} not found", featureName);
                    return Results.NotFound(new { message = $"Feature '{featureName}' not found." });
                }

                var canProceed = featureChangeValidation.CanProceed(request.ToDto(featureName), FeatureChangeType.Update);

                if (canProceed.Cancel)
                {
                    logger.LogWarning("Update operation cancelled for feature {FeatureName}: {CancellationMessage}", featureName, canProceed.CancellationMessage);
                    return Results.BadRequest(canProceed.CancellationMessage);
                }

                var result = await UpdateFeature(feature, request, context);
                logger.LogInformation("Feature {FeatureName} updated successfully with IsEnabled={IsEnabled}", featureName, request.IsEnabled);
                return Results.Ok(result);
            })
            .Produces<FeatureFlagDto>(200)
            .Produces(404)
            .Produces(400);

        return routeGroup;
    }

    private static async Task<FeatureFlagDto> UpdateFeature(FeatureFlag feature, UpdateFeatureRequest request,
        FeatureFlagDbContext context)
    {
        feature.IsEnabled = request.IsEnabled;
        feature.Description = request.Description;
        feature.UpdatedAt = DateTime.UtcNow;

        // Clear existing filters and add new ones if provided
        feature.Filters.Clear();
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

        await context.SaveChangesAsync();

        var response = new FeatureFlagDto(
            feature.Name,
            feature.IsEnabled,
            feature.Description,
            feature.Filters.Select(f => new FeatureFilterDto(f.FilterType, f.Parameters)).ToList());
        return response;
    }
}

/// <summary>
/// Request to update an existing feature flag.
/// </summary>
/// <param name="IsEnabled">Whether the feature is enabled.</param>
/// <param name="Description">Optional description of the feature.</param>
/// <param name="Filters">Optional filter configurations for the feature.</param>
internal record UpdateFeatureRequest(bool IsEnabled, string? Description = null, List<FeatureFilterDto>? Filters = null)
{
    public FeatureFlagDto ToDto(string name)
    {
        return new FeatureFlagDto(name, IsEnabled, string.Empty, Filters);
    }
}