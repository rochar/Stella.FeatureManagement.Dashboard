namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Configuration options for the Feature Management Dashboard.
/// </summary>
public sealed class DashboardOptions
{
    /// <summary>
    /// Gets or sets the features to add if they don't exist.
    /// Key: feature name, Value: initial enabled state.
    /// </summary>
    public Dictionary<string, bool> AddIfNotExists { get; set; } = [];

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
