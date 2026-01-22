using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/> to configure the Feature Management Dashboard endpoints.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Add Feature Management Dashboard endpoints to the specified route group.
    /// Creates two route groups:
    /// <list type="bullet">
    ///   <item><description><c>{group}/dashboard</c> - Serves the static dashboard UI.</description></item>
    ///   <item><description><c>{group}/dashboardapi/features</c> - Exposes the feature flags CRUD API.</description></item>
    ///   <item><description><c>{group}</c> - Exposes the feature state from feature management.</description></item>
    /// </list>
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IEndpointRouteBuilder"/> to add dashboard endpoints to.</param>
    /// <param name="group">The route prefix for the dashboard endpoints. Defaults to "/features".</param>
    /// <param name="configureCors">Optional action to configure CORS policy for the dashboard endpoints.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> so that additional calls can be chained.</returns>
    public static IFeatureDashboardBuilder UseFeaturesDashboard(this IEndpointRouteBuilder routeBuilder,
        string group = "/features",
        Action<CorsPolicyBuilder>? configureCors = null)
    {
        var builder = new FeatureDashboardBuilder(routeBuilder);
        builder.UseFeaturesDashboard(group, configureCors);
        return builder;
    }
}