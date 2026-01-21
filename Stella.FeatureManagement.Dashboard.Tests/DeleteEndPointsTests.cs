using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class DeleteEndPointsTests(WebApp webApp) : IClassFixture<WebApp>
{
    private readonly HttpClient _client = webApp.CreateClient();
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
    [Fact]
    public async Task WhenDeleteReadonlyFeatureShouldReturn400()
    {
        // Act
        var response = await _client.DeleteAsync($"{WebApp.ApiBaseUrl}/features/MyFlag", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
