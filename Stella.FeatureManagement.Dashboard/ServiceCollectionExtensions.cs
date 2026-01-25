using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
///     Extension methods for <see cref="IFeatureManagementBuilder" /> to add the Feature Management Dashboard.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the Feature Management Dashboard services with database storage.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configureDbContext">Action to configure the database context (SQL Server, PostgreSQL, etc.).</param>
    /// <returns>The <see cref="IFeatureManagerDashboardBuilder" /> so that additional calls can be chained.</returns>
    public static IFeatureManagerDashboardBuilder AddFeaturesDashboard(this IServiceCollection serviceCollection,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        return new FeatureManagerDashboardBuilder(serviceCollection, configureDbContext);
    }
}

/// <summary>
///     A builder interface for configuring the Feature Management Dashboard.
/// </summary>
public interface IFeatureManagerDashboardBuilder
{
    /// <summary>
    ///     Adds a given feature filter to the list of feature filters that will be available to enable features during runtime.
    /// </summary>
    /// <typeparam name="T">The type of the feature filter to add.</typeparam>
    /// <returns>The <see cref="IFeatureManagerDashboardBuilder" /> so that additional calls can be chained.</returns>
    IFeatureManagerDashboardBuilder AddFeatureFilter<T>(object defaultSettings) where T : IFeatureFilterMetadata;
}