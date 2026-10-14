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
                FeatureFlagUpdater updater,
                IDbContextFactory<FeatureFlagDbContext> contextFactory,
                ILogger<FeatureFlagDbContext> logger,
                CancellationToken cancellationToken) =>
            {
                logger.LogInformation("Received update request for feature {FeatureName}", featureName);

                // Npgsql refuses a non-UTC DateTime for timestamptz, so it must be refused before EF sees it.
                if (request.ExpectedUpdatedAt is { Kind: not DateTimeKind.Utc })
                    return Results.BadRequest(new
                    {
                        message = "expectedUpdatedAt must be a UTC timestamp ending in 'Z', as returned by the API."
                    });

                await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

                var feature = await FeatureFlagUpdater.LoadAsync(context, featureName, cancellationToken);

                if (feature is null)
                {
                    logger.LogWarning("Feature {FeatureName} not found", featureName);
                    return Results.NotFound(new { message = $"Feature '{featureName}' not found." });
                }

                var result = await updater.UpdateAsync(context, feature, request.ToDto(feature),
                    request.ExpectedUpdatedAt, cancellationToken);

                switch (result.Status)
                {
                    case FeatureFlagUpdateStatus.Updated:
                        logger.LogInformation("Feature {FeatureName} updated successfully with IsEnabled={IsEnabled}", featureName, request.IsEnabled);
                        return Results.Ok(result.Feature);
                    case FeatureFlagUpdateStatus.Unchanged:
                        return Results.Ok(result.Feature);
                    case FeatureFlagUpdateStatus.Rejected:
                        logger.LogWarning("Update operation cancelled for feature {FeatureName}: {CancellationMessage}", featureName, result.Message);
                        return Results.BadRequest(result.Message);
                    case FeatureFlagUpdateStatus.Conflict:
                        logger.LogWarning("Update of feature {FeatureName} conflicted with a concurrent change", featureName);
                        return Results.Conflict(new { message = result.Message, feature = result.Feature });
                    case FeatureFlagUpdateStatus.NotFound:
                        // Deleted between this request's load and its save.
                        return Results.NotFound(new { message = result.Message });
                    default:
                        throw new InvalidOperationException($"Unhandled update status {result.Status}.");
                }
            })
            .Produces<FeatureFlagDto>(200)
            .Produces(404)
            .Produces(400)
            .Produces(409);

        return routeGroup;
    }
}

/// <summary>
/// Request to update an existing feature flag.
/// </summary>
/// <param name="IsEnabled">Whether the feature is enabled.</param>
/// <param name="Description">Optional description of the feature.</param>
/// <param name="Filters">Optional filter configurations for the feature.</param>
/// <param name="Application">The application this feature belongs to.</param>
/// <param name="ExpectedUpdatedAt">The feature's <c>updatedAt</c> the edit started from (UTC). When set, the
/// update applies only if it is still current (409 otherwise); when omitted, the last write wins.</param>
internal record UpdateFeatureRequest(bool IsEnabled, string? Description = null, List<FeatureFilterDto>? Filters = null, string? Application = null, DateTime? ExpectedUpdatedAt = null)
{
    public FeatureFlagDto ToDto(FeatureFlag current)
    {
        return new FeatureFlagDto(current.Name, IsEnabled, Description, Filters, Application ?? current.Application)
        {
            CreatedAt = current.CreatedAt,
            UpdatedAt = current.UpdatedAt
        };
    }
}