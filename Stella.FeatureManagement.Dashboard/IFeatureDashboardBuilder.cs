namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Provides methods for building and managing the feature dashboard.
/// </summary>
public interface IFeatureDashboardBuilder
{
    /// <summary>
    /// Applies any pending migrations to the features database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous migration operation.</returns>
    Task MigrateFeaturesDatabaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new managed feature flag in the dashboard.
    /// Ensure its created, allows to edit and validates associated filter settings
    /// Note: It's limited to one filter per feature flag, since the UI does not support multiple filters yet.
    /// The defined filter will be always mandatory
    /// and the user won't be able to disable it from the dashboard or add other ones
    /// since the UI does not support it yet.
    /// </summary>
    /// <param name="name">The unique name of the feature flag.</param>
    /// <param name="description">A description of the feature flag's purpose.</param>
    /// <param name="isEnabled">Indicates whether the feature flag is enabled by default.</param>
    /// <param name="filterOptions">Optional filter options to apply to the feature flag.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous registration operation.</returns>
    Task RegisterManagedFeatureAsync(string name, string description, bool isEnabled,
        FilterOptions? filterOptions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers multiple managed feature flags in the dashboard.
    /// Iterates through the provided features and registers each one.
    /// </summary>
    /// <param name="features">A collection of managed features to register.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous registration operation.</returns>
    Task RegisterManagedFeaturesAsync(IEnumerable<ManagedFeature> features,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a managed feature flag to be registered in the dashboard.
/// </summary>
/// <param name="Name">The unique name of the feature flag.</param>
/// <param name="Description">A description of the feature flag's purpose.</param>
/// <param name="IsEnabled">Indicates whether the feature flag is enabled by default.</param>
/// <param name="FilterOptions">Optional filter options to apply to the feature flag.</param>
public sealed record ManagedFeature(
    string Name,
    string Description,
    bool IsEnabled,
    FilterOptions? FilterOptions = null);