namespace Stella.FeatureManagement.Dashboard;

/// <summary>
///     Options for recording feature usage served by the feature state endpoint.
/// </summary>
public class FeatureUsageOptions
{
    /// <summary>
    ///     Gets or sets how often buffered usage counters are written to the database.
    ///     Counters not yet written are lost if the process stops abruptly. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(30);
}
