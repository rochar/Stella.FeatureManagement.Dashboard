using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Extension methods for <see cref="IFeatureManagementBuilder"/> to add the Feature Management Dashboard.
/// </summary>
public static class FeatureManagementBuilderExtensions
{

    /// <summary>
    /// Adds the Feature Management Dashboard services with database storage.
    /// </summary>
    /// <param name="builder">The <see cref="IFeatureManagementBuilder"/> to add services to.</param>
    /// <param name="configureDbContext">Action to configure the database context (SQL Server, PostgreSQL, etc.).</param>
    /// <returns>The <see cref="IFeatureManagementBuilder"/> so that additional calls can be chained.</returns>
    public static IFeatureManagementBuilder AddDashboard(this IFeatureManagementBuilder builder, Action<DbContextOptionsBuilder> configureDbContext)
    {
        builder.Services.AddDbContextFactory<FeatureFlagDbContext>(configureDbContext);

        builder.Services.AddSingleton<IFeatureDefinitionProvider, DatabaseFeatureDefinitionProvider>();

        return builder;
    }
}
