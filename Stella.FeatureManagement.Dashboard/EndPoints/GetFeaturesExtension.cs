using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class GetFeaturesExtension
{
    public static RouteGroupBuilder MapGetFeatures(this RouteGroupBuilder routeGroup)
    {
        routeGroup.MapGet("", async (IFeatureManager featureManager) =>
        {
            var features = new List<FeatureState>();
            await foreach (var featureName in featureManager.GetFeatureNamesAsync()
                               .WithCancellation(CancellationToken.None))
            {
                var isEnabled = await featureManager.IsEnabledAsync(featureName);
                features.Add(new FeatureState(featureName, isEnabled));
            }

            return features;
        }).Produces<List<FeatureState>>(200);

        routeGroup.MapGet("{featureName}", async (string featureName, IFeatureManager featureManager) =>
        {
            var isEnabled = await featureManager.IsEnabledAsync(featureName);
            return isEnabled;
        }).Produces<bool>(200);

        return routeGroup;
    }
}

internal record FeatureState(string FeatureName, bool IsEnabled);