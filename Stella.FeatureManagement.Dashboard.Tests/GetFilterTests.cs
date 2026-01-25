using System.Net.Http.Json;
using Example.Api.FeatureManager;
using Microsoft.FeatureManagement.FeatureFilters;
using Shouldly;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class GetFilterTests(WebApp webApp) : IClassFixture<WebApp>
{
    private readonly HttpClient _client = webApp.CreateClient();

    [Fact]
    public async Task WhenGetFiltersReturnsPercentageFilter()
    {
        // Act
        var response = await _client.GetAsync($"{WebApp.ApiBaseUrl}/filters", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var filters = await response.Content.ReadFromJsonAsync<List<FilterDto>>(TestContext.Current.CancellationToken);
        
        filters.ShouldNotBeNull();
        var percentageFilter = filters.Single(f => f.Name == "Microsoft.Percentage");
        percentageFilter.DefaultSettings.ShouldNotBeNull();
        percentageFilter.DefaultSettings.ShouldContain("\"Value\":50");
    }

    [Fact]
    public async Task WhenGetFiltersReturnsTestFilter()
    {
        // Act
        var response = await _client.GetAsync($"{WebApp.ApiBaseUrl}/filters", TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var filters = await response.Content.ReadFromJsonAsync<List<FilterDto>>(TestContext.Current.CancellationToken);
        
        filters.ShouldNotBeNull();
        var testFilter = filters.Single(f => f.Name == "TestFilter");
        testFilter.DefaultSettings.ShouldNotBeNull();
        testFilter.DefaultSettings.ShouldContain("\"Ids\":[3,4]");
    }
}
