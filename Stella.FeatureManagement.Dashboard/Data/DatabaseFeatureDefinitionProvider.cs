using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using System.Text;

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
        // Any is also Microsoft.FeatureManagement's default; it is set explicitly because consumers
        // (e.g. adding one id to a targeting filter) rely on OR and must not inherit a changed default.
        var definition = new FeatureDefinition
        {
            Name = feature.Name,
            RequirementType = RequirementType.Any
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

        // Has filters - any filter that passes enables the feature (OR; RequirementType.Any)
        var filterConfigs = feature.Filters.Select(filter =>
        {
            var configurationBuilder = new ConfigurationBuilder();
            if (!string.IsNullOrEmpty(filter.Parameters))
                configurationBuilder
                    .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(filter.Parameters)));

            return new FeatureFilterConfiguration
            {
                Name = filter.FilterType,
                Parameters = configurationBuilder
                    .Build()
            };
        }).ToList();

        definition.EnabledFor = filterConfigs;
        return definition;
    }
}