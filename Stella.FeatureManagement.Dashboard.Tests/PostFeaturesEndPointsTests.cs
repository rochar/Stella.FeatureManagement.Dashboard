using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class PostFeaturesEndPointsTests(WebApp webApp) : IClassFixture<WebApp>
{
    private readonly HttpClient _client = webApp.CreateClient();

    [Fact]
    public async Task WhenPostFeatureWithInvalidFilterReturns400AndDoesNotCreate()
    {
        // Arrange
        var featureName = $"FeatureInvalidFilter_{Guid.NewGuid():N}";
        var createRequest = new
        {
            Name = featureName,
            IsEnabled = true,
            Filters = new[] { new { FilterType = "Microsoft.Percentage", Parameters = "{\"Dummy\": 50}" } }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features", createRequest,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var getResponse = await _client.GetAsync($"{WebApp.ApiBaseUrl}/features/{featureName}",
            TestContext.Current.CancellationToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenPostFeatureWithValidFilterCreates()
    {
        // Arrange
        var featureName = $"FeatureValidFilter_{Guid.NewGuid():N}";
        var createRequest = new
        {
            Name = featureName,
            IsEnabled = true,
            Filters = new[] { new { FilterType = "Microsoft.Percentage", Parameters = "{\"Value\": 50}" } }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features", createRequest,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result =
            await response.Content.ReadFromJsonAsync<FeatureStateResponse>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Filters.Count.ShouldBe(1);
        result.CreatedAt.ShouldNotBeNull();
        result.UpdatedAt.ShouldBe(result.CreatedAt);
    }
}
