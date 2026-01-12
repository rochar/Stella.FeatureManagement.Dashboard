using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using System.Net;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class EndPointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("MyFeatureFlag", true)]
    [InlineData("Dummy", false)]
    public async Task WhenGetFeatureByNameIsEnabled(string featureName, bool isEnabled)
    {
        // Act
        var response = await _client.GetAsync($"/features/{featureName}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldBe(isEnabled.ToString().ToLowerInvariant());
    }
}
