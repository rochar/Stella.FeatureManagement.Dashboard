namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Reads and writes stored feature flags from application code, with the same validation as the
/// dashboard and optimistic concurrency on <see cref="FeatureFlagDto.UpdatedAt"/>.
/// </summary>
/// <remarks>
/// Every feature returned has its <see cref="FeatureFlagDto.Filters"/> in a stable order (the order the
/// filters were written in) and its timestamps as <see cref="DateTimeKind.Utc"/>, so a returned
/// <see cref="FeatureFlagDto.UpdatedAt"/> can be passed back unchanged as the concurrency token.
/// Filters are combined with OR: any filter that passes enables an enabled feature.
/// </remarks>
public interface IFeatureFlagStore
{
    /// <summary>
    /// Gets one feature by name.
    /// </summary>
    /// <param name="featureName">The feature name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The feature, or null when no feature has that name.</returns>
    Task<FeatureFlagDto?> FindFeatureAsync(string featureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets every stored feature.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>All features, ordered by name.</returns>
    Task<IReadOnlyList<FeatureFlagDto>> ListFeaturesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces <see cref="FeatureFlagDto.IsEnabled"/>, <see cref="FeatureFlagDto.Description"/>,
    /// <see cref="FeatureFlagDto.Application"/> and <see cref="FeatureFlagDto.Filters"/> of the feature named
    /// <see cref="FeatureFlagDto.Name"/>, as a compare-and-set on <see cref="FeatureFlagDto.UpdatedAt"/>.
    /// </summary>
    /// <remarks>
    /// <para>The steps, in order:</para>
    /// <list type="number">
    /// <item><description>The change is validated exactly as <see cref="ValidateFeature"/> does; a refusal is
    /// <see cref="FeatureFlagUpdateStatus.Rejected"/>.</description></item>
    /// <item><description>The requested state is compared field by field with the stored state, filters as an
    /// ordered (filter type, parameters) sequence with ordinal string comparison. When they are equal the result
    /// is <see cref="FeatureFlagUpdateStatus.Unchanged"/>: nothing is written and <c>UpdatedAt</c> does not
    /// move. <b>This check precedes the token</b>, so a stale <c>UpdatedAt</c> whose requested state equals the
    /// stored state is <see cref="FeatureFlagUpdateStatus.Unchanged"/>, not
    /// <see cref="FeatureFlagUpdateStatus.Conflict"/>: the caller's state is already stored.</description></item>
    /// <item><description>The write applies only if the stored <c>UpdatedAt</c> still equals
    /// <see cref="FeatureFlagDto.UpdatedAt"/>; otherwise nothing is written and the result is
    /// <see cref="FeatureFlagUpdateStatus.Conflict"/> carrying the current feature. A successful write sets
    /// <c>UpdatedAt</c> to the later of now and the first microsecond after the expected value, so it always
    /// moves strictly forward, even when clocks differ between writers.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="feature">The complete requested state; <see cref="FeatureFlagDto.UpdatedAt"/> is the value
    /// the caller read (the concurrency token).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The outcome; see <see cref="FeatureFlagUpdateStatus"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="feature"/> is null.</exception>
    /// <exception cref="ArgumentException"><see cref="FeatureFlagDto.UpdatedAt"/> is null or not
    /// <see cref="DateTimeKind.Utc"/>, or <see cref="FeatureFlagDto.Filters"/> is null (null would delete every
    /// filter and could widen the feature to everyone; pass an empty list to remove them on purpose).</exception>
    Task<FeatureFlagUpdateResult> UpdateFeatureAsync(FeatureFlagDto feature, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the validation <see cref="UpdateFeatureAsync"/> runs (filter parameters against their registered
    /// settings types, and any validator registered through <c>OnFeatureChanging</c>, as an update) without
    /// writing anything. Use it to preview whether a write would be accepted.
    /// </summary>
    /// <param name="feature">The requested state.</param>
    /// <returns>The validation result; <see cref="FeatureChangeValidationResult.Cancel"/> is true when
    /// <see cref="UpdateFeatureAsync"/> would return <see cref="FeatureFlagUpdateStatus.Rejected"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="feature"/> is null.</exception>
    FeatureChangeValidationResult ValidateFeature(FeatureFlagDto feature);
}
