using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Service for applying feature flag options to the database.
/// </summary>
public interface IFeatureOptionsApplier
{
    /// <summary>
    /// Applies the specified feature options to the database.
    /// </summary>
    /// <param name="options">The dashboard options containing feature operations.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task ApplyAsync(DashboardOptions options, CancellationToken cancellationToken = default);
}
