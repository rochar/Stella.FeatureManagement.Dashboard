namespace Stella.FeatureManagement.Dashboard.Services;

/// <summary>
/// Service for to update database schema and initialize feature flag .
/// </summary>
internal interface IDashboardInitializer
{
    Task RegisterFeatureAsync(string name, string description, bool isEnabled, FilterOptions? filterOptions,
        CancellationToken cancellationToken);

    Task RunMigrationsAsync(CancellationToken cancellationToken);
}