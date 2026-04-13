using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stella.FeatureManagement.Dashboard.EndPoints;
using Stella.FeatureManagement.Dashboard.Services;

namespace Stella.FeatureManagement.Dashboard;

internal class FeatureManagerDashboardAppBuilder(IEndpointRouteBuilder routeBuilder) : IFeatureManagerDashboardAppBuilder
{
    public void UseFeaturesDashboard(string group = "/features",
        Action<CorsPolicyBuilder>? configureCors = null,
        string? rateLimitingPolicy = null)
    {
        var featuresGroup = routeBuilder.MapGroup($"{group}");
        var dashboardGroup = routeBuilder.MapGroup($"{group}/dashboard");
        var dashboardApiFilters = routeBuilder.MapGroup($"{group}/dashboardapi/filters");
        var dashboardApiFeatures = routeBuilder.MapGroup($"{group}/dashboardapi/features");
        var dashboardApiApplications = routeBuilder.MapGroup($"{group}/dashboardapi/applications");

        if (configureCors is not null)
        {
            dashboardGroup.RequireCors(configureCors);
            dashboardApiFilters.RequireCors(configureCors);
            dashboardApiFeatures.RequireCors(configureCors);
            dashboardApiApplications.RequireCors(configureCors);
            featuresGroup.RequireCors(configureCors);
        }

        if (rateLimitingPolicy is not null)
        {
            featuresGroup.RequireRateLimiting(rateLimitingPolicy);
        }

        dashboardGroup
            .MapStaticDashboard();
        dashboardApiFilters
            .MapGetFilters();
        dashboardApiFeatures
            .MapGetFeatures()
            .MapPostFeatures()
            .MapPutFeatures()
            .MapDeleteFeatures();
        dashboardApiApplications
            .MapGetApplications();

        featuresGroup
            .MapGetFeaturesFromFeatureManager();
    }

    public IFeatureManagerDashboardAppBuilder OnFeatureChanging(Func<FeatureFlagDto, FeatureChangeType, FeatureChangeValidationResult> featureChangeValidator)
    {
        using var scope = routeBuilder.ServiceProvider.CreateScope();
        var featureValidation = scope.ServiceProvider.GetRequiredService<IFeatureChangeValidation>();
        featureValidation.RegisterCustomValidation(featureChangeValidator);
        return this;
    }

    /// <summary>
    /// Applies pending database migrations for the Feature Management Dashboard.
    /// Creates the database if it does not exist.
    /// should be executed before registering any features.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous migration operation.</returns>
    public async Task MigrateFeaturesDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = routeBuilder.ServiceProvider.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(FeatureManagerDashboardAppBuilder));

        var initializer = scope.ServiceProvider.GetService<IDashboardInitializer>();

        if (initializer is null)
        {
            logger.LogError("Cannot apply migrations: IDashboardInitializer is not registered.");
            return;
        }

        await initializer.RunMigrationsAsync(cancellationToken);
    }

    public async Task RegisterManagedFeatureAsync(string name, string description, bool isEnabled,
        FilterOptions? filterOptions = null,
        string application = "Default",
        CancellationToken cancellationToken = default)
    {
        await using var scope = routeBuilder.ServiceProvider.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(FeatureManagerDashboardAppBuilder));

        var initializer = scope.ServiceProvider.GetRequiredService<IDashboardInitializer>();
        await initializer.RegisterFeatureAsync(name, description, isEnabled, filterOptions, application, cancellationToken);
    }

    public async Task RegisterManagedFeaturesAsync(IEnumerable<ManagedFeature> features,
        CancellationToken cancellationToken = default)
    {
        foreach (var feature in features)
            await RegisterManagedFeatureAsync(
                feature.Name,
                feature.Description,
                feature.IsEnabled,
                feature.FilterOptions,
                feature.Application,
                cancellationToken);
    }
}

public sealed record FilterOptions(string TypeName, object DefaultSettings);