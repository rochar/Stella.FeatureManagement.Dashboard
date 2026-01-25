using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class GetFeaturesEndPointTests(WebApp webApp) : IClassFixture<WebApp>
{
    private readonly HttpClient _client = webApp.CreateClient();

    [Fact]
    public async Task WhenGetFeatureWithPercentageFilterFromFeatureManager()
    {
        int numberOfRequests = 100;
        var results = new List<bool>(numberOfRequests);
        // Act
        for (int i = 0; i < numberOfRequests; i++)
        {
            var response = await _client.GetAsync($"features/FilteredFlag", TestContext.Current.CancellationToken);
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            results.Add(bool.Parse(content));
        }

        // Assert
        results.Count(r => r).ShouldBeInRange(35, 65);
    }

    [Theory]
    [InlineData("MyFlag", true)]
    [InlineData("AnotherFlag", false)]
    public async Task WhenGetFeatureByNameIsEnabled(string featureName, bool isEnabled)
    {
        // Act
        var response = await _client.GetAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FeatureStateResponse>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(featureName);
        result.IsEnabled.ShouldBe(isEnabled);
    }

    [Fact]
    public async Task WhenGetDashboardReturnsHtml()
    {
        // Act
        var response = await _client.GetAsync("/features/dashboard/", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
    }

    [Fact]
    public async Task WhenGetDashboardAssetsReturnsCss()
    {
        // Act
        var response = await _client.GetAsync("/features/dashboard/assets/index.css", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/css");
    }
}