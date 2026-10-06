namespace Stella.FeatureManagement.Dashboard.Services;

/// <summary>
///     Records feature evaluations in memory so they can be persisted as daily counters.
/// </summary>
internal interface IFeatureUsageRecorder
{
    /// <summary>
    ///     Counts one evaluation of <paramref name="featureName" /> for the current UTC day.
    ///     Never performs I/O, so it is safe to call on the request path.
    /// </summary>
    void Record(string featureName, bool isEnabled);

    /// <summary>
    ///     Writes the counters accumulated since the previous flush to the database.
    /// </summary>
    Task FlushAsync(CancellationToken cancellationToken = default);
}
