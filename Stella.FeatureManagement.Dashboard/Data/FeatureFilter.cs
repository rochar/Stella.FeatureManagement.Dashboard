namespace Stella.FeatureManagement.Dashboard.Data;

/// <summary>
/// Represents a filter configuration for a feature flag.
/// </summary>
public class FeatureFilter
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the feature flag ID.
    /// </summary>
    public int FeatureFlagId { get; set; }

    /// <summary>
    /// Gets or sets the associated feature flag.
    /// </summary>
    public FeatureFlag FeatureFlag { get; set; } = null!;

    /// <summary>
    /// Gets or sets the filter type (e.g., "Microsoft.Percentage", "Microsoft.TimeWindow", "Microsoft.Targeting").
    /// </summary>
    public required string FilterType { get; set; }

    /// <summary>
    /// Gets or sets the filter parameters as JSON.
    /// Examples:
    /// - Percentage: {"Value": "50"}
    /// - TimeWindow: {"Start": "2025-01-01", "End": "2025-12-31"}
    /// - Targeting: {"Audience": {"Users": ["user1"], "Groups": [{"Name": "Beta", "RolloutPercentage": "100"}]}}
    /// </summary>
    public string? Parameters { get; set; }
}
