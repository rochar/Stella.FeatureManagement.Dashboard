using Microsoft.FeatureManagement;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Extension methods for <see cref="IFeatureManagementBuilder"/> to add the Feature Management Dashboard.
/// </summary>
public static class FeatureManagementBuilderExtensions
{
    /// <summary>
    /// Adds the Feature Management Dashboard services.
    /// </summary>
    /// <param name="builder">The <see cref="IFeatureManagementBuilder"/> to add services to.</param>
    /// <returns>The <see cref="IFeatureManagementBuilder"/> so that additional calls can be chained.</returns>
    public static IFeatureManagementBuilder AddDashboard(this IFeatureManagementBuilder builder)
    {
        // TODO: Add dashboard services registration
        return builder;
    }
}
