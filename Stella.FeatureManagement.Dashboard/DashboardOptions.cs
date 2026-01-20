namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Represents a feature configuration with enabled state and description.
/// </summary>
/// <param name="IsEnabled">Whether the feature is enabled.</param>
/// <param name="Description">Optional description of the feature.</param>
public record FeatureConfig(bool IsEnabled, string? Description = null, FeatureFilterConfig? Filter = null);

/// <summary>
/// Feature filters configuration.
/// </summary>
/// <param name="FilterType">filter type (e.g., "Microsoft.Percentage", "Microsoft.TimeWindow", "Microsoft.Targeting", "Custom").</param>
/// <param name="Parameters">
/// sets the filter parameters as JSON.
/// Examples:
/// - Percentage: {"Value": "50"}
/// - TimeWindow: {"Start": "2025-01-01", "End": "2025-12-31"}
/// - Targeting: {"Audience": {"Users": ["user1"], "Groups": [{"Name": "Beta", "RolloutPercentage": "100"}]}}
/// </param>
public record FeatureFilterConfig(string FilterType, string? Parameters);

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
    /// Key: feature name, Value: feature definition with enabled state, description, and filter.
    /// </summary>
    public Dictionary<string, FeatureConfig> AddOrUpdate { get; set; } = [];

    /// <summary>
    /// Gets or sets the feature names to delete.
    /// </summary>
    public List<string> Delete { get; set; } = [];
}