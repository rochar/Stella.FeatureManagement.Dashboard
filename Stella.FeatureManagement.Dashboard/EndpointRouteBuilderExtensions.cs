using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Stella.FeatureManagement.Dashboard.Data;

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
    /// <param name="configure">Optional action to configure dashboard options for seeding features.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> so that additional calls can be chained.</returns>
    public static async Task UseDashboardAsync(this IEndpointRouteBuilder routeBuilder,
        string group = "/features",
        Action<DashboardOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        await using var scope = routeBuilder.ServiceProvider.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(EndpointRouteBuilderExtensions));

        await RunMigrationsAsync(scope, logger, cancellationToken);
        await ApplyFeatureOptionsAsync(scope, logger, configure, cancellationToken);

        var routeGroup = routeBuilder.MapGroup(group)
            .RequireCors(policy => policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

        RegisterDashboardUiEndpoints(routeGroup);


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
    }

    private static async Task RunMigrationsAsync(AsyncServiceScope scope, ILogger logger,
        CancellationToken cancellationToken)
    {
        var factory = scope.ServiceProvider.GetService<IDbContextFactory<FeatureFlagDbContext>>();

        if (factory is not null)
        {
            await using var context = await factory.CreateDbContextAsync(cancellationToken);

            logger.LogInformation("Applying Feature Management Dashboard database migrations...");
            await context.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Feature Management Dashboard database migrations applied successfully.");
        }
    }

    private static async Task ApplyFeatureOptionsAsync(AsyncServiceScope scope, ILogger logger,
        Action<DashboardOptions>? configure, CancellationToken cancellationToken)
    {
        if (configure is null) return;

        var options = new DashboardOptions();
        configure(options);

        var applier = scope.ServiceProvider.GetService<IFeatureOptionsApplier>();

        if (applier is null)
        {
            logger.LogError("Cannot apply feature options: IFeatureOptionsApplier is not registered.");
            return;
        }

        await applier.ApplyAsync(options, cancellationToken);
    }

    private static void RegisterDashboardUiEndpoints(RouteGroupBuilder routeGroup)
    {
        // Serve React SPA from embedded files
        var embeddedProvider = new ManifestEmbeddedFileProvider(
            typeof(EndpointRouteBuilderExtensions).Assembly, "wwwroot");

        routeGroup.MapGet("dashboard/{**path}", (HttpContext context, string? path) =>
        {
            var encodedPath = context.Request.GetEncodedPathAndQuery(); // Path + query only

            if (string.IsNullOrEmpty(path))
                return Results.Redirect(encodedPath.EndsWith("/", StringComparison.CurrentCultureIgnoreCase)
                    ? "index.html"
                    : "dashboard/index.html");

            var file = embeddedProvider.GetFileInfo(path);

            // SPA fallback: serve index.html for client-side routing
            if (!file.Exists || file.IsDirectory) file = embeddedProvider.GetFileInfo("index.html");

            if (!file.Exists) return Results.NotFound();

            var contentTypeProvider = new FileExtensionContentTypeProvider();
            if (!contentTypeProvider.TryGetContentType(file.Name, out var contentType))
                contentType = "application/octet-stream";

            return Results.Stream(file.CreateReadStream(), contentType);
        }).ExcludeFromDescription();
    }

    private record FeatureState(string FeatureName, bool IsEnabled);
}