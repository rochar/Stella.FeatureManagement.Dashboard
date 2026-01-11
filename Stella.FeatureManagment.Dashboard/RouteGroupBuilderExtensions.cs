using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Extension methods for <see cref="RouteGroupBuilder"/> to configure the Feature Management Dashboard endpoints.
/// </summary>
public static class RouteGroupBuilderExtensions
{
    /// <summary>
    /// Maps the Feature Management Dashboard endpoints to this route group.
    /// </summary>
    /// <param name="routeGroupBuilder">The <see cref="RouteGroupBuilder"/> to add dashboard endpoints to.</param>
    /// <returns>The <see cref="RouteGroupBuilder"/> so that additional calls can be chained.</returns>
    public static RouteGroupBuilder UseDashboard(this RouteGroupBuilder routeGroupBuilder)
    {
       routeGroupBuilder.MapGet("{featureName}", async (string featureName, IFeatureManager featureManager) => 
       {
           var isEnabled = await featureManager.IsEnabledAsync(featureName);
           return new { FeatureName = featureName, IsEnabled = isEnabled };
       });
        return routeGroupBuilder;
    }
}
