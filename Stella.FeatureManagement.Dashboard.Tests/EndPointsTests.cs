using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using System.Net;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class EndPointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("MyFlag", true)]
    [InlineData("AnotherFlag", false)]
    public async Task WhenGetFeatureByNameIsEnabled(string featureName, bool isEnabled)
    {
        // Act
        var response = await _client.GetAsync($"/features/{featureName}", TestContext.Current.CancellationToken);

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
}
