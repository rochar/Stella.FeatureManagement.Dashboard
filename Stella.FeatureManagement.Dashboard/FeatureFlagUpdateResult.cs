namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// The outcome of <see cref="IFeatureFlagStore.UpdateFeatureAsync"/>.
/// </summary>
public sealed class FeatureFlagUpdateResult
{
    /// <summary>
    /// What the update did.
    /// </summary>
    public FeatureFlagUpdateStatus Status { get; init; }

    /// <summary>
    /// The feature as stored after the call: the written state for <see cref="FeatureFlagUpdateStatus.Updated"/>,
    /// the unchanged stored state for <see cref="FeatureFlagUpdateStatus.Unchanged"/> and
    /// <see cref="FeatureFlagUpdateStatus.Rejected"/>, and the freshly read current state for
    /// <see cref="FeatureFlagUpdateStatus.Conflict"/>. Null for <see cref="FeatureFlagUpdateStatus.NotFound"/>.
    /// </summary>
    public FeatureFlagDto? Feature { get; init; }

    /// <summary>
    /// A human-readable explanation: the validation message for <see cref="FeatureFlagUpdateStatus.Rejected"/>,
    /// and a description of the failure for <see cref="FeatureFlagUpdateStatus.NotFound"/> and
    /// <see cref="FeatureFlagUpdateStatus.Conflict"/>. Null on success.
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// The status of a <see cref="IFeatureFlagStore.UpdateFeatureAsync"/> call.
/// </summary>
public enum FeatureFlagUpdateStatus
{
    /// <summary>
    /// The requested state was written and <see cref="FeatureFlagDto.UpdatedAt"/> moved strictly forward.
    /// </summary>
    Updated = 1,

    /// <summary>
    /// The requested state already equals the stored state. Nothing was written and
    /// <see cref="FeatureFlagDto.UpdatedAt"/> did not move. Takes precedence over the token: a stale
    /// <see cref="FeatureFlagDto.UpdatedAt"/> whose requested state equals the stored state is
    /// <see cref="Unchanged"/>, not <see cref="Conflict"/>.
    /// </summary>
    Unchanged,

    /// <summary>
    /// No feature with the requested name exists.
    /// </summary>
    NotFound,

    /// <summary>
    /// The stored <see cref="FeatureFlagDto.UpdatedAt"/> no longer equals the expected one (another writer
    /// got there first). Nothing was written; <see cref="FeatureFlagUpdateResult.Feature"/> holds the current state.
    /// </summary>
    Conflict,

    /// <summary>
    /// Validation refused the change (filter parameters, or a validator registered through
    /// <c>OnFeatureChanging</c>). Nothing was written; <see cref="FeatureFlagUpdateResult.Message"/> holds the reason.
    /// </summary>
    Rejected
}
