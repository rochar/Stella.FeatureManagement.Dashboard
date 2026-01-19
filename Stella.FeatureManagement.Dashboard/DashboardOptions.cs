namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Represents a feature configuration with enabled state and description.
/// </summary>
/// <param name="IsEnabled">Whether the feature is enabled.</param>
/// <param name="Description">Optional description of the feature.</param>
public record FeatureConfig(bool IsEnabled, string? Description = null);

/// <summary>
/// Configuration options for the Feature Management Dashboard.
/// </summary>
public sealed class DashboardOptions
{
    /// <summary>
    /// Gets or sets the features to add if they don't exist.
    /// Key: feature name, Value: feature definition with enabled state and description.
    /// </summary>
    public Dictionary<string, FeatureConfig> AddIfNotExists { get; set; } = [];

    /// <summary>
    /// Gets or sets the features to add or update.
    /// Key: feature name, Value: enabled state.
    /// </summary>
    public Dictionary<string, bool> AddOrUpdate { get; set; } = [];

    /// <summary>
    /// Gets or sets the feature names to delete.
    /// </summary>
    public List<string> Delete { get; set; } = [];
}
