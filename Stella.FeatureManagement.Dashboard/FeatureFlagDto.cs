namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Represents a feature flag with its configuration.
/// </summary>
/// <param name="Name">The feature flag name.</param>
/// <param name="IsEnabled">Whether the feature is enabled.</param>
/// <param name="Description">Optional description of the feature.</param>
/// <param name="Filters">The filters applied to this feature flag.</param>
public record FeatureFlagDto(string Name, bool IsEnabled, string? Description, List<FeatureFilterDto>? Filters);

/// <summary>
/// Represents a filter configuration for a feature flag.
/// </summary>
/// <param name="FilterType">The filter type (e.g., "Microsoft.Percentage", "Microsoft.TimeWindow").</param>
/// <param name="Parameters">The filter parameters as JSON.</param>
public record FeatureFilterDto(string FilterType, string? Parameters);