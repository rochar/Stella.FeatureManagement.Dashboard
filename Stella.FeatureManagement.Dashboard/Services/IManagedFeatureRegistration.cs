namespace Stella.FeatureManagement.Dashboard.Services;

internal interface IManagedFeatureRegistration
{
    bool IsFeatureRegistered(string featureName);
    FilterOptions? GetFeatureRegistration(string featureName);
    bool TryAdd(string name, FilterOptions? filterOptions);
}