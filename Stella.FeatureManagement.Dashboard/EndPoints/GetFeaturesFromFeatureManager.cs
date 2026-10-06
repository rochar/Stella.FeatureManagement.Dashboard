using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement;
using Stella.FeatureManagement.Dashboard.Services;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class GetFeaturesFromFeatureManager
{
    /// <summary>
    ///     Endpoint to get a feature and its state from Feature Management.
    ///     Query string parameters are captured into <see cref="IFeatureEvaluationRequestContext" />
    ///     so custom feature filters can access them via dependency injection.
    /// </summary>
    public static RouteGroupBuilder MapGetFeaturesFromFeatureManager(this RouteGroupBuilder routeGroup)
    {
        routeGroup.MapGet("{featureName}", async (string featureName, IFeatureManager featureManager,
            IFeatureUsageRecorder usageRecorder, HttpContext httpContext) =>
        {
            var parameters = httpContext.Request.Query
                .ToDictionary(q => q.Key, q => q.Value.ToString(), StringComparer.OrdinalIgnoreCase)
                .AsReadOnly();

            httpContext.Items[FeatureEvaluationRequestContext.HttpContextItemsKey] = parameters;

            var isEnabled = await featureManager.IsEnabledAsync(featureName);
            usageRecorder.Record(featureName, isEnabled);
            return isEnabled;
        }).Produces<bool>(200);

        return routeGroup;
    }
}
