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

        _filters[filterType] = new FilterRegistration(filterType, NameOf(filterType), JsonSerializer.Serialize(defaultSettings), defaultSettings.GetType());
    }

    /// <summary>
    ///     Adds a feature filter type with a settings type and no default settings. The filter is listed with
    ///     <see cref="FilterRegistration.DefaultSettingsJson" /> <see langword="null" />, and its parameters are
    ///     still validated against <typeparamref name="TSettings" />. If the filter type is already registered,
    ///     the existing registration is kept.
    /// </summary>
    /// <typeparam name="TFilter">The type of the feature filter. Must implement <see cref="IFeatureFilterMetadata" />.</typeparam>
    /// <typeparam name="TSettings">
    ///     The concrete type the filter's parameters are validated against. Must not be abstract or an interface.
    /// </typeparam>
    /// <exception cref="ArgumentException"><typeparamref name="TSettings" /> is abstract or an interface.</exception>
    public void AddFilter<TFilter, TSettings>()
        where TFilter : IFeatureFilterMetadata
        where TSettings : class
    {
        // Not on IFeatureFilterRepository: a new member there would break its third-party implementers.
        var settingsType = typeof(TSettings);

        // System.Text.Json cannot deserialize into these, and validation only turns JsonException into a
        // message: every save using the filter would otherwise fail with a 500. Fail at startup instead.
        if (settingsType.IsAbstract || settingsType.IsInterface)
            throw new ArgumentException(
                $"The settings type {settingsType.FullName} of filter {typeof(TFilter).FullName} must be a concrete " +
                "type: filter parameters cannot be validated against an abstract type or an interface.",
                nameof(TSettings));

        var filterType = typeof(TFilter);
        if (_filters.ContainsKey(filterType)) return;

        _filters[filterType] = new FilterRegistration(filterType, NameOf(filterType), null, settingsType);
    }

    private static string NameOf(Type filterType) =>
        filterType.GetCustomAttribute<FilterAliasAttribute>()?.Alias ?? filterType.Name;

    /// <inheritdoc />
    public IEnumerable<FilterRegistration> GetFilters()
    {
        return _filters.Values;
    }

    /// <inheritdoc />
    public FilterRegistration? GetFilterByName(string name)
    {
        return _filters.Values.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}