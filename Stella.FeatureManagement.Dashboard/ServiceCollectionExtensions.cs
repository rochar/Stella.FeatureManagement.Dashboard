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

    /// <summary>
    ///     Adds a feature filter that has a settings type but no default settings. The dashboard opens a blank
    ///     parameters editor for it instead of pre-filling one.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Parameters saved for this filter are still validated against <typeparamref name="TSettings" /> on
    ///         every create and update, exactly as for <see cref="AddFeatureFilter{T}(object)" />. Null parameters
    ///         validate as <c>{}</c>, so a filter saved with no parameters is accepted; an empty or whitespace-only
    ///         string is refused.
    ///     </para>
    ///     <para>
    ///         The first registration of a filter type wins, whichever overload made it.
    ///     </para>
    ///     <para>
    ///         The default implementation throws <see cref="NotSupportedException" />; it exists only so that
    ///         implementations of this interface written before this member was added still compile.
    ///         <see cref="FeatureManagerDashboardBuilder" /> implements it.
    ///     </para>
    /// </remarks>
    /// <typeparam name="TFilter">The type of the feature filter to add.</typeparam>
    /// <typeparam name="TSettings">
    ///     The concrete type the filter's parameters are validated against. Must not be abstract or an interface.
    /// </typeparam>
    /// <returns>The <see cref="IFeatureManagerDashboardBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="NotSupportedException">The implementation does not support filters without defaults.</exception>
    /// <exception cref="ArgumentException">
    ///     <typeparamref name="TSettings" /> is abstract or an interface (thrown by
    ///     <see cref="FeatureManagerDashboardBuilder" />; nothing is registered).
    /// </exception>
    IFeatureManagerDashboardBuilder AddFeatureFilter<TFilter, TSettings>()
        where TFilter : IFeatureFilterMetadata
        where TSettings : class
    {
        // A default member rather than a plain one keeps the change additive: an abstract member would break
        // every consumer that implements this public interface.
        throw new NotSupportedException(
            $"{GetType().FullName} does not support feature filters without default settings. " +
            $"Use {nameof(AddFeatureFilter)}<T>(defaultSettings) instead.");
    }
}