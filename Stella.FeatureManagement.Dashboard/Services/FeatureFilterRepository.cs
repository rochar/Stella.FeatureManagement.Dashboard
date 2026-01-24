using Microsoft.FeatureManagement;

namespace Stella.FeatureManagement.Dashboard.Services;

/// <summary>
/// Default implementation of <see cref="IFeatureFilterRepository" />.
/// </summary>
public class FeatureFilterRepository : IFeatureFilterRepository
{
    private readonly HashSet<Type> _filters = [];

    /// <inheritdoc />
    public void AddFilter<T>() where T : IFeatureFilterMetadata
    {
        _filters.Add(typeof(T));
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetFilters()
    {
        return _filters;
    }
}
