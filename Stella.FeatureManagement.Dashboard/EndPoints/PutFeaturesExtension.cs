using Microsoft.AspNetCore.Routing;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class PutFeaturesExtension
{
    public static RouteGroupBuilder MapPutFeatures(this RouteGroupBuilder routeGroup)
    {
        //routeGroup.MapGet("", async (IFeatureManager featureManager) =>
        //{
        //    var features = new List<FeatureState>();
        //    await foreach (var featureName in featureManager.GetFeatureNamesAsync()
        //                       .WithCancellation(CancellationToken.None))
        //    {
        //        var isEnabled = await featureManager.IsEnabledAsync(featureName);
        //        features.Add(new FeatureState(featureName, isEnabled));
        //    }

        //    return features;
        //}).Produces<List<FeatureState>>(200);

        //routeGroup.MapGet("{featureName}", async (string featureName, IFeatureManager featureManager) =>
        //{
        //    var isEnabled = await featureManager.IsEnabledAsync(featureName);
        //    return isEnabled;
        //}).Produces<bool>(200);

        return routeGroup;
    }
}