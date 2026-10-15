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

    [Fact]
    public void WhenFilterIsAddedWithoutDefaultsRepositoryRecordsSettingsTypeAndNoDefaultJson()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddFeaturesDashboard(options => { });

        // Act: each filter is registered twice, through both overloads in opposite orders.
        builder.AddFeatureFilter<TestFilter, TestFilterSettings>();
        builder.AddFeatureFilter<TestFilter>(new TestFilterSettings { Ids = [1] });
        builder.AddFeatureFilter<OtherTestFilter>(new TestFilterSettings { Ids = [2] });
        builder.AddFeatureFilter<OtherTestFilter, TestFilterSettings>();

        // Assert
        var repository = services.BuildServiceProvider().GetRequiredService<IFeatureFilterRepository>();

        var withoutDefaults = repository.GetFilters().Single(f => f.Type == typeof(TestFilter));
        withoutDefaults.Name.ShouldBe(nameof(TestFilter));
        withoutDefaults.DefaultSettingsJson.ShouldBeNull();
        withoutDefaults.SettingsType.ShouldBe(typeof(TestFilterSettings));

        var withDefaults = repository.GetFilters().Single(f => f.Type == typeof(OtherTestFilter));
        withDefaults.DefaultSettingsJson.ShouldBe("{\"Ids\":[2]}");
        withDefaults.SettingsType.ShouldBe(typeof(TestFilterSettings));

        services.ShouldContain(d =>
            d.ServiceType == typeof(IFeatureFilterMetadata) && d.ImplementationType == typeof(TestFilter));
    }

    [Fact]
    public void WhenFilterIsAddedWithoutDefaultsWithAbstractOrInterfaceSettingsTypeRegistrationIsRefused()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddFeaturesDashboard(options => { });

        // Act / Assert: System.Text.Json cannot deserialize either, so validation could never run.
        Should.Throw<ArgumentException>(() => builder.AddFeatureFilter<TestFilter, ITestFilterSettings>())
            .ParamName.ShouldBe("TSettings");
        Should.Throw<ArgumentException>(() => builder.AddFeatureFilter<OtherTestFilter, AbstractTestFilterSettings>())
            .ParamName.ShouldBe("TSettings");

        // Nothing is half-registered: neither the dashboard nor Microsoft.FeatureManagement knows the filters.
        var repository = services.BuildServiceProvider().GetRequiredService<IFeatureFilterRepository>();
        repository.GetFilters().ShouldNotContain(f => f.Type == typeof(TestFilter) || f.Type == typeof(OtherTestFilter));
        services.ShouldNotContain(d =>
            d.ServiceType == typeof(IFeatureFilterMetadata) &&
            (d.ImplementationType == typeof(TestFilter) || d.ImplementationType == typeof(OtherTestFilter)));
    }

    [Fact]
    public void WhenBuilderInterfaceDefaultIsUsedItThrowsNotSupported()
    {
        IFeatureManagerDashboardBuilder builder = new BuilderWithoutFiltersWithoutDefaults();

        Should.Throw<NotSupportedException>(() => builder.AddFeatureFilter<TestFilter, TestFilterSettings>());
    }

    private sealed class TestFilter : IFeatureFilterMetadata
    {
    }

    private sealed class OtherTestFilter : IFeatureFilterMetadata
    {
    }

    private sealed class TestFilterSettings
    {
        public int[] Ids { get; set; } = [];
    }

    private interface ITestFilterSettings
    {
        int[] Ids { get; set; }
    }

    private abstract class AbstractTestFilterSettings
    {
        public int[] Ids { get; set; } = [];
    }

    // A third-party implementer written before AddFeatureFilter<TFilter, TSettings>() existed.
    private sealed class BuilderWithoutFiltersWithoutDefaults : IFeatureManagerDashboardBuilder
    {
        public IFeatureManagerDashboardBuilder AddFeatureFilter<T>(object defaultSettings)
            where T : IFeatureFilterMetadata => this;
    }
}
