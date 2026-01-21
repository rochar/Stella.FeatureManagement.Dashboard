using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using System.Text.Json;

namespace Stella.FeatureManagement.Dashboard.Data;

/// <summary>
/// Provides feature definitions from the database.
/// </summary>
internal class DatabaseFeatureDefinitionProvider : IFeatureDefinitionProvider
{
    private readonly IDbContextFactory<FeatureFlagDbContext> _contextFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="DatabaseFeatureDefinitionProvider"/>.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    public DatabaseFeatureDefinitionProvider(IDbContextFactory<FeatureFlagDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var features = await context.FeatureFlags
            .Include(f => f.Filters)
            .ToListAsync();

        foreach (var feature in features) yield return CreateFeatureDefinition(feature);
    }

    /// <inheritdoc/>
    public async Task<FeatureDefinition?> GetFeatureDefinitionAsync(string featureName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var feature = await context.FeatureFlags
            .Include(f => f.Filters)
            .FirstOrDefaultAsync(f => f.Name == featureName);

        return feature is null ? null : CreateFeatureDefinition(feature);
    }

    private static FeatureDefinition CreateFeatureDefinition(FeatureFlag feature)
    {
        var definition = new FeatureDefinition
        {
            Name = feature.Name
        };

        // Feature is disabled - return empty filters (feature always off)
        if (!feature.IsEnabled)
        {
            definition.EnabledFor = [];
            return definition;
        }

        // No filters - feature is always on
        if (feature.Filters.Count == 0)
        {
            definition.EnabledFor = [new FeatureFilterConfiguration { Name = "AlwaysOn" }];
            return definition;
        }

        // Has filters - all must pass (AND logic is handled by FeatureManager)
        var filterConfigs = feature.Filters.Select(filter =>
        {
            var parameters = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(filter.Parameters))
            {
                var jsonDoc = JsonDocument.Parse(filter.Parameters);
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    parameters[property.Name] = property.Value.ToString();
                }
            }

            return new FeatureFilterConfiguration
            {
                Name = filter.FilterType,
                Parameters = new ConfigurationBuilder()
                    .AddInMemoryCollection(parameters!)
                    .Build()
            };
        }).ToList();

        definition.EnabledFor = filterConfigs;
        return definition;
    }
}