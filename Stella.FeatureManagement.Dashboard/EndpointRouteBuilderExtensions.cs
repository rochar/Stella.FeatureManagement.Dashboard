using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/> to configure the Feature Management Dashboard endpoints.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Creates a route group and maps the Feature Management Dashboard endpoints to it.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IEndpointRouteBuilder"/> to add dashboard endpoints to.</param>
    /// <param name="group">The route prefix for the dashboard endpoints. Defaults to "/features".</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> so that additional calls can be chained.</returns>
    public static IEndpointRouteBuilder UseDashboard(this IEndpointRouteBuilder routeBuilder, string group = "/features")
    {
        var routeGroup = routeBuilder.MapGroup(group);

        routeGroup.MapGet("", async (IFeatureManager featureManager) =>
        {
            var features = new List<FeatureState>();
            await foreach (var featureName in featureManager.GetFeatureNamesAsync())
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

        return routeBuilder;
    }

    private record FeatureState(string FeatureName, bool IsEnabled);
}