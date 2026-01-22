using System.Collections.Concurrent;

namespace Stella.FeatureManagement.Dashboard.Services;

internal sealed class ManagedFeatureRegistration : IManagedFeatureRegistration
{
    private readonly ConcurrentDictionary<string, FilterOptions?> _managedFeatures = new();
    public bool IsFeatureRegistered(string featureName)
    {
        return _managedFeatures.ContainsKey(featureName);
    }

    public FilterOptions? GetFeatureRegistration(string featureName)
    {
        return _managedFeatures[featureName];
    }

    public bool TryAdd(string name, FilterOptions? filterOptions)
    {
        return _managedFeatures.TryAdd(name, filterOptions);
    }
}