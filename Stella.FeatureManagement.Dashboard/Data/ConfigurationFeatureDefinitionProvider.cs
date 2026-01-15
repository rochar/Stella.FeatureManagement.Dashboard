using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

namespace Stella.FeatureManagement.Dashboard.Data;

/// <summary>
/// Provides feature definitions from configuration (appsettings.json).
/// Used as fallback when no database is configured.
/// </summary>
internal class ConfigurationFeatureDefinitionProvider : IFeatureDefinitionProvider
{
    private readonly IConfiguration _configuration;

    public ConfigurationFeatureDefinitionProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
    {
        var featureManagementSection = _configuration.GetSection("FeatureManagement");

        foreach (var feature in featureManagementSection.GetChildren())
        {
            yield return new FeatureDefinition
            {
                Name = feature.Key,
                EnabledFor = feature.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
                    ? [new FeatureFilterConfiguration { Name = "AlwaysOn" }]
                    : []
            };
        }

        await Task.CompletedTask;
    }

    public Task<FeatureDefinition?> GetFeatureDefinitionAsync(string featureName)
    {
        var featureValue = _configuration[$"FeatureManagement:{featureName}"];

        if (featureValue is null)
        {
            return Task.FromResult<FeatureDefinition?>(null);
        }

        return Task.FromResult<FeatureDefinition?>(new FeatureDefinition
        {
            Name = featureName,
            EnabledFor = featureValue.Equals("true", StringComparison.OrdinalIgnoreCase)
                ? [new FeatureFilterConfiguration { Name = "AlwaysOn" }]
                : []
        });
    }
}
