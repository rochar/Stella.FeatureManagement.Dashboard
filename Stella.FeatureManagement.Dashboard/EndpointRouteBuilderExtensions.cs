using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stella.FeatureManagement.Dashboard.EndPoints;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/> to configure the Feature Management Dashboard endpoints.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Applies pending database migrations for the Feature Management Dashboard.
    /// Creates the database if it does not exist.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IEndpointRouteBuilder"/> to access services.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous migration operation.</returns>
    public static async Task MigrateFeaturesDatabaseAsync(this IEndpointRouteBuilder routeBuilder,
        CancellationToken cancellationToken = default)
    {
        await using var scope = routeBuilder.ServiceProvider.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(EndpointRouteBuilderExtensions));

        var initializer = scope.ServiceProvider.GetService<IDashboardInitializer>();

        if (initializer is null)
        {
            logger.LogError("Cannot apply migrations: IDashboardInitializer is not registered.");
            return;
        }

        await initializer.RunMigrationsAsync(cancellationToken);
    }

    /// <summary>
    /// Ensure Feature Management Dashboard is initialized with the specified options.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IEndpointRouteBuilder"/> to access services.</param>
    /// <param name="configure">Action to configure dashboard options for seeding features.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous seed operation.</returns>
    public static async Task InitializeFeaturesDashboardAsync(this IEndpointRouteBuilder routeBuilder,
        Action<DashboardOptions> configure,
        CancellationToken cancellationToken = default)
    {
        await using var scope = routeBuilder.ServiceProvider.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(EndpointRouteBuilderExtensions));

        var options = new DashboardOptions();
        configure(options);

        var initializer = scope.ServiceProvider.GetService<IDashboardInitializer>();

        if (initializer is null)
        {
            logger.LogError("Cannot seed features: IDashboardInitializer is not registered.");
            return;
        }

        await initializer.ApplyDashboardOptionsAsync(options, cancellationToken);
    }

    /// <summary>
    /// Creates a route group and maps the Feature Management Dashboard endpoints to it.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IEndpointRouteBuilder"/> to add dashboard endpoints to.</param>
    /// <param name="group">The route prefix for the dashboard endpoints. Defaults to "/features".</param>
    /// <param name="configureCors">Optional action to configure CORS policy for the dashboard endpoints.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> so that additional calls can be chained.</returns>
    public static IEndpointRouteBuilder MapFeaturesDashboardEndpoints(this IEndpointRouteBuilder routeBuilder,
        string group = "/features",
        Action<CorsPolicyBuilder>? configureCors = null)
    {
        var routeGroup = routeBuilder.MapGroup(group);

        if (configureCors is not null)
        {
            routeGroup.RequireCors(configureCors);
        }

        routeGroup
            .MapStaticDashboard()
            .MapGetFeatures()
            .MapPostFeatures()
            .MapPutFeatures()
            .MapDeleteFeatures();

        return routeBuilder;
    }
}