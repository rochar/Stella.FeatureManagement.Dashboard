using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Stella.FeatureManagement.Dashboard.Services;

/// <summary>
///     Default implementation of <see cref="IFeatureFilterRepository" /> that stores filter metadata in memory.
/// </summary>
public class FeatureFilterRepository : IFeatureFilterRepository
{
    private readonly Dictionary<Type, FilterRegistration> _filters = [];
    private readonly ILogger<FeatureFilterRepository>? _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FeatureFilterRepository" /> class.
    /// </summary>
    /// <param name="logger">The logger for capturing diagnostic information.</param>
    public FeatureFilterRepository(ILogger<FeatureFilterRepository>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void AddFilter<T>(object defaultSettings) where T : IFeatureFilterMetadata
    {
        AddFilter(typeof(T), defaultSettings);
    }

    /// <inheritdoc />
    public void AddFilter(Type filterType, object defaultSettings)
    {
        ArgumentNullException.ThrowIfNull(filterType);
        ArgumentNullException.ThrowIfNull(defaultSettings);
        
        if (_filters.ContainsKey(filterType)) return;

        var aliasAttribute = filterType.GetCustomAttribute<FilterAliasAttribute>();
        var name = aliasAttribute?.Alias ?? filterType.Name;

        _filters[filterType] = new FilterRegistration(filterType, name, JsonSerializer.Serialize(defaultSettings));
    }

    /// <inheritdoc />
    public IEnumerable<FilterRegistration> GetFilters()
    {
        return _filters.Values;
    }
}