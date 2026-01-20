using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class GetFeaturesFromFeatureManager
{
    /// <summary>
    /// Endpoint to get a feature and its state from Feature Management
    /// </summary>
    /// <param name="routeGroup"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapGetFeaturesFromFeatureManager(this RouteGroupBuilder routeGroup)
    {

        routeGroup.MapGet("{featureName}", async (string featureName, IFeatureManager featureManager) =>
            await featureManager.IsEnabledAsync(featureName)).Produces<bool>(200);

        return routeGroup;
    }
}
