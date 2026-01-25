using Microsoft.FeatureManagement;

namespace Stella.FeatureManagement.Dashboard.Services;

/// <summary>
/// Represents a registered feature filter with its metadata.
/// </summary>
/// <param name="Type">The type of the feature filter.</param>
/// <param name="Name">The display name of the filter, usually from <see cref="FilterAliasAttribute"/>.</param>
/// <param name="DefaultSettingsJson">The JSON-serialized default settings for this filter.</param>
public record FilterRegistration(Type Type, string Name, string? DefaultSettingsJson);

/// <summary>
/// Repository for keeping track of available feature filters and their default configurations.
/// </summary>
public interface IFeatureFilterRepository
{
    /// <summary>
    /// Adds a feature filter type and its default settings to the repository.
    /// </summary>
    /// <typeparam name="T">The type of the feature filter. Must implement <see cref="IFeatureFilterMetadata"/>.</typeparam>
    /// <param name="defaultSettings">The default settings object for this filter. Will be serialized to JSON.</param>
    void AddFilter<T>(object defaultSettings) where T : IFeatureFilterMetadata;

    /// <summary>
    /// Adds a feature filter type and its default settings to the repository.
    /// </summary>
    /// <param name="filterType">The type of the feature filter. Must implement <see cref="IFeatureFilterMetadata"/>.</param>
    /// <param name="defaultSettings">The default settings object for this filter. Will be serialized to JSON.</param>
    void AddFilter(Type filterType, object defaultSettings);

    /// <summary>
    /// Gets all registered feature filters.
    /// </summary>
    /// <returns>A collection of <see cref="FilterRegistration"/> containing filter metadata.</returns>
    IEnumerable<FilterRegistration> GetFilters();
}
