using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Stella.FeatureManagement.Dashboard.Data;
using Stella.FeatureManagement.Dashboard.Services;
using System.Net;
using System.Net.Http.Json;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class DeleteFeaturesEndPointsTests(WebApp webApp) : IClassFixture<WebApp>
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

/// <summary>
/// A class of its own, so it gets its own <see cref="WebApp"/>: the custom-validation slot holds one validator,
/// and registering one here replaces Example.Api's MyFlag rule that the read-only tests above rely on.
/// </summary>
public class DeleteFeaturesConcurrencyEndPointsTests(WebApp webApp) : IClassFixture<WebApp>
{
    private readonly HttpClient _client = webApp.CreateClient();
    private readonly IServiceProvider _services = webApp.Services;

    [Fact]
    public async Task WhenDeleteRacesUpdateItReturnsConflictNotServerError()
    {
        var ct = TestContext.Current.CancellationToken;
        var featureName = $"FeatureDeleteRace_{Guid.NewGuid():N}";
        (await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features",
            new { Name = featureName, IsEnabled = true }, ct)).StatusCode.ShouldBe(HttpStatusCode.Created);

        // The DELETE calls the validation between loading the feature and saving, so a concurrent update
        // committed from here lands exactly in that window: no timing involved.
        var contextFactory = _services.GetRequiredService<IDbContextFactory<FeatureFlagDbContext>>();
        _services.GetRequiredService<IFeatureChangeValidation>().RegisterCustomValidation((feature, changeType) =>
        {
            if (changeType == FeatureChangeType.Delete && feature.Name == featureName)
            {
                using var context = contextFactory.CreateDbContext();
                context.FeatureFlags
                    .Where(f => f.Name == featureName)
                    .ExecuteUpdate(s => s
                        .SetProperty(f => f.Description, "changed concurrently")
                        .SetProperty(f => f.UpdatedAt, f => f.UpdatedAt.AddSeconds(1)));
            }

            return new FeatureChangeValidationResult(false, string.Empty);
        });

        var response = await _client.DeleteAsync($"{WebApp.ApiBaseUrl}/features/{featureName}", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var stored = await _client.GetFromJsonAsync<FeatureStateResponse>($"{WebApp.ApiBaseUrl}/features/{featureName}", ct);
        stored.ShouldNotBeNull();
        stored.Description.ShouldBe("changed concurrently");
    }
}
