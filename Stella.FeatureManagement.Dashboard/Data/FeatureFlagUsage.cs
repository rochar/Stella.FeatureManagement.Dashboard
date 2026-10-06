namespace Stella.FeatureManagement.Dashboard.Data;

/// <summary>
/// Daily evaluation counters for a feature flag, as served by the feature state endpoint.
/// One row per feature per UTC day.
/// </summary>
public class FeatureFlagUsage
{
    /// <summary>
    /// Gets or sets the feature flag ID.
    /// </summary>
    public int FeatureFlagId { get; set; }

    /// <summary>
    /// Gets or sets the associated feature flag.
    /// </summary>
    public FeatureFlag FeatureFlag { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UTC day the counters belong to.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets how many evaluations returned enabled on this day.
    /// </summary>
    public long EnabledCount { get; set; }

    /// <summary>
    /// Gets or sets how many evaluations returned disabled on this day.
    /// </summary>
    public long DisabledCount { get; set; }
}
