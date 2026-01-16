namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Service for to update database schema and initialize feature flag .
/// </summary>
internal interface IDashboardInitializer
{
    /// <summary>
    /// Applies the specified feature options to the database.
    /// </summary>
    /// <param name="options">The dashboard options containing feature operations.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task ApplyDashboardOptionsAsync(DashboardOptions options, CancellationToken cancellationToken = default);

    Task RunMigrationsAsync(CancellationToken cancellationToken);
}