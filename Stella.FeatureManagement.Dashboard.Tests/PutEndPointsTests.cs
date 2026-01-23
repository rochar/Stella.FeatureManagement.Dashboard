using Example.Api.FeatureManager;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class PutEndPointsTests(WebApp webApp) : IClassFixture<WebApp>
{
    private readonly HttpClient _client = webApp.CreateClient();

    [Theory]
    [InlineData("AnotherFlag")]
    public async Task WhenPutFeatureUpdatesState(string featureName)
    {
        // Arrange - Get current state first
        var getInitialResponse = await _client.GetAsync($"{WebApp.ApiBaseUrl}/features/{featureName}",
            TestContext.Current.CancellationToken);
        var initialResult =
            await getInitialResponse.Content.ReadFromJsonAsync<FeatureStateResponse>(TestContext.Current
                .CancellationToken);
        var initialState = initialResult!.IsEnabled;
        var newState = !initialState;

        var request = new { IsEnabled = newState };

        // Act
        var response = await _client.PutAsJsonAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result =
            await response.Content.ReadFromJsonAsync<FeatureStateResponse>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(featureName);
        result.IsEnabled.ShouldBe(newState);

        // Verify the change persisted
        var getResponse = await _client.GetAsync($"{WebApp.ApiBaseUrl}/features/{featureName}",
            TestContext.Current.CancellationToken);
        var verifyResult =
            await getResponse.Content.ReadFromJsonAsync<FeatureStateResponse>(TestContext.Current.CancellationToken);
        verifyResult.ShouldNotBeNull();
        verifyResult.IsEnabled.ShouldBe(newState);
    }

    [Fact]
    public async Task WhenPutFeatureUpdatesStateWithFilter()
    {
        // Arrange - Create a feature without a filter
        var featureName = $"FeatureToUpdate_{Guid.NewGuid():N}";
        var createRequest = new { Name = featureName, IsEnabled = false };
        var createResponse = await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features", createRequest,
            TestContext.Current.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Update with a filter
        var updateRequest = new
        {
            IsEnabled = true,
            Description = "Feature with percentage filter",
            Filter = new { FilterType = "Microsoft.Percentage", Parameters = "{\"Value\": 50}" }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", updateRequest,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result =
            await response.Content.ReadFromJsonAsync<FeatureStateResponse>(TestContext.Current.CancellationToken);
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
    public async Task WhenPutFeatureUpdatesStateWithInvalidFilterReturn400()
    {
        // Arrange 
        var updateRequest = new
        {
            IsEnabled = true,
            Filter = new { FilterType = "Microsoft.Percentage", Parameters = "{\"Dummy\": 50}" }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{WebApp.ApiBaseUrl}/features/FilteredFlag", updateRequest,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenPutFeatureNotFoundReturns404()
    {
        // Arrange
        var request = new { IsEnabled = true };

        // Act
        var response = await _client.PutAsJsonAsync($"{WebApp.ApiBaseUrl}/features/NonExistentFeature", request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenPutReadonlyFeatureShouldReturn400()
    {
        // Arrange
        var updateRequest = new
        {
            IsEnabled = true
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{WebApp.ApiBaseUrl}/features/MyFlag", updateRequest,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public async Task WhenUpdateFilterShouldReturnEnabled(int id, bool expectedResult)
    {
        // Arrange
        // Arrange 
        var updateRequest = new
        {
            IsEnabled = true,
            Filter = new { FilterType = "TestFilter", Parameters = System.Text.Json.JsonSerializer.Serialize(new TestFilterSettings { Ids = [id] }) }
        };

        var updateResponse = await _client.PutAsJsonAsync($"{WebApp.ApiBaseUrl}/features/CustomFilterFlag", updateRequest,
             TestContext.Current.CancellationToken);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act - Get from FeatureManager the current state
        var response = await _client.GetAsync($"features/CustomFilterFlag", TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var result = bool.Parse(content);

        // Assert
        result.ShouldBe(expectedResult);
    }

}