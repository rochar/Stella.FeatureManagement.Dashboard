using Microsoft.FeatureManagement;

namespace Stella.FeatureManagement.Dashboard.Services;

/// <summary>
/// Repository for keeping track of available feature filters.
/// </summary>
public interface IFeatureFilterRepository
{
    /// <summary>
    /// Adds a feature filter type to the repository.
    /// </summary>
    /// <typeparam name="T">The type of the feature filter.</typeparam>
    void AddFilter<T>() where T : IFeatureFilterMetadata;

    /// <summary>
    /// Gets all registered feature filter types.
    /// </summary>
    /// <returns>A collection of registered feature filter types.</returns>
    IEnumerable<Type> GetFilters();
}
