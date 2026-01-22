using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Stella.FeatureManagement.Dashboard.Data;
using Stella.FeatureManagement.Dashboard.Services;

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
    public static IFeatureManagementBuilder AddFeaturesDashboard(this IFeatureManagementBuilder builder,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        builder.Services.AddDbContextFactory<FeatureFlagDbContext>(configureDbContext);
        builder.Services.AddSingleton<IFeatureDefinitionProvider, DatabaseFeatureDefinitionProvider>();
        builder.Services.AddSingleton<IDashboardInitializer, DashboardInitializer>();
        builder.Services.AddSingleton<IManagedFeatureRegistration, ManagedFeatureRegistration>();
        builder.Services.AddSingleton(typeof(IFeatureChangeValidation), new NoValidationFeatureChangeValidation());

        return builder;
    }

    /// <summary>
    /// Subscribes to feature flag change events for pre-save validation.
    /// The validator is invoked before any feature flag is created, updated, or deleted,
    /// allowing the subscriber to cancel the operation with a message.
    /// </summary>
    /// <param name="builder">The <see cref="IFeatureManagementBuilder"/> to add the interceptor to.</param>
    /// <param name="featureChangeValidator">
    /// A delegate that receives the feature data, change type, and cancellation token.
    /// Return <see cref="FeatureChangeValidationResult"/> with <c>Cancel = true</c> to reject the change.
    /// </param>
    /// <returns>The <see cref="IFeatureManagementBuilder"/> so that additional calls can be chained.</returns>
    /// <example>
    /// <code>
    /// builder.Services
    ///     .AddFeatureManagement()
    ///     .AddFeaturesDashboard(options => options.UseNpgsql(connectionString))
    ///     .OnFeatureChanging((feature, changeType) =>
    ///     {
    ///         if (feature.TypeName == "PROD_X")
    ///             return new FeatureChangeValidationResult(Cancel: true, "PROD_X feature is read-only.");
    ///         return new FeatureChangeValidationResult(Cancel: false, null);
    ///     });
    /// </code>
    /// </example>
    public static IFeatureManagementBuilder OnFeatureChanging(this IFeatureManagementBuilder builder,
        Func<FeatureFlagDto, FeatureChangeType, FeatureChangeValidationResult> featureChangeValidator)
    {
        var descriptors = builder.Services
            .Where(d => d.ServiceType == typeof(IFeatureChangeValidation))
            .ToList();

        foreach (var descriptor in descriptors) builder.Services.Remove(descriptor);

        builder.Services.AddSingleton<IFeatureChangeValidation>(sp =>
            new FeatureChangeValidation(
                featureChangeValidator,
                sp.GetRequiredService<IManagedFeatureRegistration>()));
        return builder;
    }
}

public enum FeatureChangeType
{
    Create,
    Update,
    Delete
}

public record FeatureChangeValidationResult(bool Cancel, string? CancellationMessage);