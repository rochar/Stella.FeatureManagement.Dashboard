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

    [Theory]
    [InlineData("MyFlag")]
    [InlineData("AnotherFlag")]
    public async Task WhenPutFeatureUpdatesState(string featureName)
    {
        // Arrange - Get current state first
        var getInitialResponse = await _client.GetAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", TestContext.Current.CancellationToken);
        var initialResult = await getInitialResponse.Content.ReadFromJsonAsync<FeatureStateResponse>(TestContext.Current.CancellationToken);
        var initialState = initialResult!.IsEnabled;
        var newState = !initialState;

        var request = new { IsEnabled = newState };

        // Act
        var response = await _client.PutAsJsonAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FeatureStateResponse>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(featureName);
        result.IsEnabled.ShouldBe(newState);

        // Verify the change persisted
        var getResponse = await _client.GetAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", TestContext.Current.CancellationToken);
        var verifyResult = await getResponse.Content.ReadFromJsonAsync<FeatureStateResponse>(TestContext.Current.CancellationToken);
        verifyResult.ShouldNotBeNull();
        verifyResult.IsEnabled.ShouldBe(newState);
    }
    [Fact]
    public async Task WhenPutFeatureUpdatesStateWithFilter()
    {
        // Arrange - Create a feature without a filter
        var featureName = $"FeatureToUpdate_{Guid.NewGuid():N}";
        var createRequest = new { Name = featureName, IsEnabled = false };
        var createResponse = await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features", createRequest, TestContext.Current.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Update with a filter
        var updateRequest = new
        {
            IsEnabled = true,
            Description = "Feature with percentage filter",
            Filter = new { FilterType = "Microsoft.Percentage", Parameters = "{\"Value\": 50}" }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FeatureStateResponse>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(featureName);
        result.IsEnabled.ShouldBeTrue();
        result.Description.ShouldBe("Feature with percentage filter");
        result.Filters.ShouldNotBeNull();
        result.Filters.Count.ShouldBe(1);
        result.Filters[0].FilterType.ShouldBe("Microsoft.Percentage");
        result.Filters[0].Parameters.ShouldBe("{\"Value\": 50}");
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
    public async Task WhenDeleteExistingFeatureShouldDelete()
    {
        // Arrange - Create a feature to delete
        var featureName = $"FeatureToDelete_{Guid.NewGuid():N}";
        var createRequest = new { Name = featureName, IsEnabled = true };
        var createResponse = await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features", createRequest, TestContext.Current.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act
        var response = await _client.DeleteAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify the feature is gone (API returns 404 for non-existent features)
        var getResponse = await _client.GetAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenDeleteFeatureNotFoundReturns404()
    {
        // Act
        var response = await _client.DeleteAsync($"{WebApp.ApiBaseUrl}/features/NonExistentFeature", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private record FeatureStateResponse(string Name, bool IsEnabled, string? Description, List<FeatureFilterResponse> Filters);
    private record FeatureFilterResponse(string FilterType, string? Parameters);
}
