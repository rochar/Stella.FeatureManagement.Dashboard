using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Shouldly;
using Stella.FeatureManagement.Dashboard.Services;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class FeatureFilterRepositoryTests
{
    [Fact]
    public void WhenAddFeatureFilterShouldBeStoredInRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        var builder = services.AddFeaturesDashboard(options => { });
        builder.AddFeatureFilter<TestFilter>(new { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var repository = serviceProvider.GetRequiredService<IFeatureFilterRepository>();
        
        var filters = repository.GetFilters();
        filters.ShouldContain(f => f.Type == typeof(TestFilter));
    }

    private class TestFilter : IFeatureFilterMetadata
    {
    }
}
