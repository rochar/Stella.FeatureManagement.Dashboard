using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Stella.FeatureManagement.Dashboard.Data;
using Stella.FeatureManagement.Dashboard.Services;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
///     Default implementation of <see cref="IFeatureManagerDashboardBuilder" />.
/// </summary>
public class FeatureManagerDashboardBuilder : IFeatureManagerDashboardBuilder
{
    private readonly IFeatureManagementBuilder _builder;
    private readonly FeatureFilterRepository _filterRepository;
    /// <summary>
    ///     Initializes a new instance of the <see cref="FeatureManagerDashboardBuilder" /> class.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configureDbContext">Action to configure the database context.</param>
    public FeatureManagerDashboardBuilder(IServiceCollection serviceCollection,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        var logger = serviceCollection.BuildServiceProvider().GetService<ILogger<FeatureFilterRepository>>();
        _filterRepository = new FeatureFilterRepository(logger);
        
        _builder = serviceCollection.AddFeatureManagement();
        _builder.Services.AddDbContextFactory<FeatureFlagDbContext>(configureDbContext);
        _builder.Services.AddSingleton<IFeatureDefinitionProvider, DatabaseFeatureDefinitionProvider>();
        _builder.Services.AddSingleton<IDashboardInitializer, DashboardInitializer>();
        _builder.Services.AddSingleton<IManagedFeatureRegistration, ManagedFeatureRegistration>();
        _builder.Services.AddSingleton<IFeatureChangeValidation, FeatureChangeValidation>();
        _builder.Services.AddSingleton<IFeatureFilterRepository>(_filterRepository);
        
        _filterRepository.AddFilter<PercentageFilter>(new PercentageFilterSettings(){Value = 50});
        _filterRepository.AddFilter<TimeWindowFilter>(new TimeWindowFilterSettings(){Start = DateTime.UtcNow,End = DateTime.UtcNow.AddDays(1)});
    }
    
    /// <inheritdoc />
    public IFeatureManagerDashboardBuilder AddFeatureFilter<T>(object defaultSettings) where T : IFeatureFilterMetadata
    {
        _builder.AddFeatureFilter<T>();
        _filterRepository.AddFilter<T>(defaultSettings);
        return this;
    }
}