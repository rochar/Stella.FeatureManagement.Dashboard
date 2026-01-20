using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class EndPointTests(WebApp webApp) : IClassFixture<WebApp>
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
        results.Count(r => r).ShouldBeInRange(40, 60);
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
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldBe(isEnabled.ToString().ToLowerInvariant());
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

    [Theory]
    [InlineData("MyFlag")]
    [InlineData("AnotherFlag")]
    public async Task WhenPutFeatureUpdatesState(string featureName)
    {
        // Arrange - Get current state first
        var getInitialResponse = await _client.GetAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", TestContext.Current.CancellationToken);

        var initialContent = await getInitialResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var initialState = bool.Parse(initialContent);
        var newState = !initialState;

        var request = new { IsEnabled = newState };

        // Act
        var response = await _client.PutAsJsonAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FeatureStateResponse>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.FeatureName.ShouldBe(featureName);
        result.IsEnabled.ShouldBe(newState);

        // Verify the change persisted
        var getResponse = await _client.GetAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", TestContext.Current.CancellationToken);
        var content = await getResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldBe(newState.ToString().ToLowerInvariant());
    }

    [Fact]
    public async Task WhenPutFeatureNotFoundReturns404()
    {
        // Arrange
        var request = new { IsEnabled = true };

        // Act
        var response = await _client.PutAsJsonAsync($"{WebApp.ApiBaseUrl}/features/NonExistentFeature", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenDeleteExistingFeatureReturnsNoContent()
    {
        // Arrange - Create a feature to delete
        var featureName = $"FeatureToDelete_{Guid.NewGuid():N}";
        var createRequest = new { FeatureName = featureName, IsEnabled = true };
        var createResponse = await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features", createRequest, TestContext.Current.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act
        var response = await _client.DeleteAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify the feature is gone (FeatureManager returns false for non-existent features)
        var getResponse = await _client.GetAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", TestContext.Current.CancellationToken);
        var content = await getResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldBe("false");
    }

    [Fact]
    public async Task WhenDeleteFeatureNotFoundReturns404()
    {
        // Act
        var response = await _client.DeleteAsync($"{WebApp.ApiBaseUrl}/features/NonExistentFeature", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private record FeatureStateResponse(string FeatureName, bool IsEnabled);
}
