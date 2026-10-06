namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Daily evaluation counters of one feature over a window of days, oldest first, with days
/// without usage filled with zeros.
/// </summary>
/// <param name="Name">The feature flag name.</param>
/// <param name="Days">One entry per UTC day in the window.</param>
internal sealed record FeatureUsageDto(string Name, List<DailyUsageDto> Days);

/// <summary>
/// Evaluation counters of a feature for one UTC day.
/// </summary>
/// <param name="Date">The UTC day.</param>
/// <param name="EnabledCount">How many evaluations returned enabled.</param>
/// <param name="DisabledCount">How many evaluations returned disabled.</param>
internal sealed record DailyUsageDto(DateOnly Date, long EnabledCount, long DisabledCount);
