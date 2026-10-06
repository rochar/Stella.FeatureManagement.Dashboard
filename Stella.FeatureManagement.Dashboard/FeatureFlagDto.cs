using System.Linq.Expressions;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Represents a feature flag with its configuration.
/// </summary>
/// <param name="Name">The feature flag name.</param>
/// <param name="IsEnabled">Whether the feature is enabled.</param>
/// <param name="Description">Optional description of the feature.</param>
/// <param name="Filters">The filters applied to this feature flag.</param>
/// <param name="Application">The application this feature belongs to.</param>
public record FeatureFlagDto(string Name, bool IsEnabled, string? Description, List<FeatureFilterDto>? Filters, string Application = "Default")
{
    /// <summary>
    /// When the feature was created (UTC). Null when the feature has not been persisted.
    /// </summary>
    public DateTime? CreatedAt { get; init; }

    /// <summary>
    /// When the feature was last updated (UTC). Null when the feature has not been persisted.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Represents a filter configuration for a feature flag.
/// </summary>
/// <param name="FilterType">The filter type (e.g., "Microsoft.Percentage", "Microsoft.TimeWindow").</param>
/// <param name="Parameters">The filter parameters as JSON.</param>
public record FeatureFilterDto(string FilterType, string? Parameters);

/// <summary>
/// The single <see cref="FeatureFlag"/> to <see cref="FeatureFlagDto"/> mapping, usable both inside
/// EF Core queries (<see cref="AsDto"/>) and on loaded entities (<see cref="ToDto"/>).
/// </summary>
internal static class FeatureFlagMapping
{
    public static readonly Expression<Func<FeatureFlag, FeatureFlagDto>> AsDto = f => new FeatureFlagDto(
        f.Name,
        f.IsEnabled,
        f.Description,
        f.Filters.Select(filter => new FeatureFilterDto(filter.FilterType, filter.Parameters)).ToList(),
        f.Application)
    {
        CreatedAt = f.CreatedAt,
        UpdatedAt = f.UpdatedAt
    };

    private static readonly Func<FeatureFlag, FeatureFlagDto> CompiledAsDto = AsDto.Compile();

    public static FeatureFlagDto ToDto(this FeatureFlag feature) => CompiledAsDto(feature);
}