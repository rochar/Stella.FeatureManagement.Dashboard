using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    /// <example>
    /// <code>
    /// // SQL Server
    /// builder.Services.AddFeatureManagement()
    ///     .AddDashboard(options => options.UseSqlServer(connectionString));
    /// 
    /// // PostgreSQL
    /// builder.Services.AddFeatureManagement()
    ///     .AddDashboard(options => options.UseNpgsql(connectionString));
    /// 
    /// // SQLite (for development)
    /// builder.Services.AddFeatureManagement()
    ///     .AddDashboard(options => options.UseSqlite("Data Source=features.db"));
    /// </code>
    /// </example>
    public static IFeatureManagementBuilder AddDashboard(
        this IFeatureManagementBuilder builder,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        // Register DbContext with factory pattern for better async support
        builder.Services.AddDbContextFactory<FeatureFlagDbContext>(configureDbContext);

        // Register the database feature definition provider
        builder.Services.AddSingleton<IFeatureDefinitionProvider, DatabaseFeatureDefinitionProvider>();

        return builder;
    }

    /// <summary>
    /// Adds the Feature Management Dashboard services.
    /// Use this when DbContext is already registered (e.g., via Aspire's AddNpgsqlDbContext).
    /// </summary>
    /// <param name="builder">The <see cref="IFeatureManagementBuilder"/> to add services to.</param>
    /// <returns>The <see cref="IFeatureManagementBuilder"/> so that additional calls can be chained.</returns>
    public static IFeatureManagementBuilder AddDashboard(this IFeatureManagementBuilder builder)
    {
        // Check if FeatureFlagDbContext is already registered (e.g., via Aspire)
        // If so, register the database provider; otherwise, use configuration-only mode
        builder.Services.AddSingleton<IFeatureDefinitionProvider>(sp =>
        {
            var factory = sp.GetService<IDbContextFactory<FeatureFlagDbContext>>();
            if (factory is not null)
            {
                return new DatabaseFeatureDefinitionProvider(factory);
            }

            // Fallback: try to create factory from DbContext options
            var contextOptions = sp.GetService<DbContextOptions<FeatureFlagDbContext>>();
            if (contextOptions is not null)
            {
                // Create a simple factory wrapper
                return new DatabaseFeatureDefinitionProvider(
                    new SimpleDbContextFactory<FeatureFlagDbContext>(contextOptions));
            }

            // No database configured - return config-based provider
            return new Data.ConfigurationFeatureDefinitionProvider(
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>());
        });

        return builder;
    }

    /// <summary>
    /// Applies pending migrations for the feature flags database. Call this at startup.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async Task UseFeatureFlagDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();

        // Try factory first
        var factory = scope.ServiceProvider.GetService<IDbContextFactory<FeatureFlagDbContext>>();
        if (factory is not null)
        {
            await using var context = await factory.CreateDbContextAsync(cancellationToken);
            await context.Database.MigrateAsync(cancellationToken);
            return;
        }

        // Try direct context (Aspire registration)
        var directContext = scope.ServiceProvider.GetService<FeatureFlagDbContext>();
        if (directContext is not null)
        {
            await directContext.Database.MigrateAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Applies pending migrations for the feature flags database. Call this at startup.
    /// </summary>
    /// <param name="host">The application host.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The host for chaining.</returns>
    public static async Task<IHost> UseFeatureFlagDatabaseAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetService<IDbContextFactory<FeatureFlagDbContext>>();

        if (factory is not null)
        {
            await using var context = await factory.CreateDbContextAsync(cancellationToken);
            await context.Database.MigrateAsync(cancellationToken);
        }

        return host;
    }
}
