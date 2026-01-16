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
    /// Creates a route group and maps the Feature Management Dashboard endpoints to it.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IEndpointRouteBuilder"/> to add dashboard endpoints to.</param>
    /// <param name="configure">Action to configure dashboard options for seeding features.</param>
    /// <param name="group">The route prefix for the dashboard endpoints. Defaults to "/features".</param>
    /// <param name="configureCors">Optional action to configure CORS policy for the dashboard endpoints.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> so that additional calls can be chained.</returns>
    public static async Task UseFeaturesDashboardAsync(this IEndpointRouteBuilder routeBuilder,
        Action<DashboardOptions> configure,
        string group = "/features",
        Action<CorsPolicyBuilder>? configureCors = null,
        CancellationToken cancellationToken = default)
    {
        await using var scope = routeBuilder.ServiceProvider.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(EndpointRouteBuilderExtensions));

        await InitializeDashboardAsync(scope, logger, configure, cancellationToken);

        var routeGroup = routeBuilder.MapGroup(group);

        if (configureCors is not null)
        {
            routeGroup.RequireCors(configureCors);
        }

        routeGroup
            .MapStaticDashboard()
            .MapGetFeatures()
            .MapPutFeatures();
    }


    private static async Task InitializeDashboardAsync(AsyncServiceScope scope, ILogger logger,
        Action<DashboardOptions>? configure, CancellationToken cancellationToken)
    {
        if (configure is null) return;

        var options = new DashboardOptions();
        configure(options);

        var applier = scope.ServiceProvider.GetService<IDashboardInitializer>();

        if (applier is null)
        {
            logger.LogError("Cannot apply feature options: IFeatureOptionsApplier is not registered.");
            return;
        }

        await applier.RunMigrationsAsync(cancellationToken);
        await applier.ApplyDashboardOptionsAsync(options, cancellationToken);
    }
}