using Microsoft.EntityFrameworkCore;

namespace Stella.FeatureManagement.Dashboard.Data;

/// <summary>
/// Represents a feature flag stored in the database.
/// </summary>
[Index(nameof(Name), IsUnique = true)]
public class FeatureFlag
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the feature name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the feature description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// When true and no filters exist, feature is always on.
    /// When true with filters, all filters must pass.
    /// When false, feature is always off regardless of filters.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the filters for this feature.
    /// Multiple filters use AND logic - all must pass for the feature to be enabled.
    /// </summary>
    public ICollection<FeatureFilter> Filters { get; set; } = [];

    /// <summary>
    /// Gets or sets when the feature was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the feature was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
