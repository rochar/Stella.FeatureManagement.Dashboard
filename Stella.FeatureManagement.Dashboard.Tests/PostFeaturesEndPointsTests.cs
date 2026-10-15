using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class PostFeaturesEndPointsTests(WebApp webApp) : IClassFixture<WebApp>
{
    private readonly HttpClient _client = webApp.CreateClient();
    private readonly IFeatureFlagStore _store = webApp.Services.GetRequiredService<IFeatureFlagStore>();

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

    [Fact]
    public async Task WhenFilterParametersExceed4000CharactersCreateAndUpdateAreRejected()
    {
        // Valid TestFilter settings, so only the length rule can refuse them (the column is varchar(4000)).
        var tooLong = FeatureFlagStoreTests.TestFilterIds([.. Enumerable.Repeat(1, 2001)]);
        tooLong.Parameters!.Length.ShouldBeGreaterThan(4000);
        var ct = TestContext.Current.CancellationToken;

        var rejectedName = $"FeatureTooLong_{Guid.NewGuid():N}";
        var create = await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features",
            new { Name = rejectedName, IsEnabled = true, Filters = new[] { tooLong } }, ct);
        create.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await _store.FindFeatureAsync(rejectedName, ct)).ShouldBeNull();

        var existingName = $"FeatureTooLongUpdate_{Guid.NewGuid():N}";
        (await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features",
            new { Name = existingName, IsEnabled = false }, ct)).StatusCode.ShouldBe(HttpStatusCode.Created);
        var existing = await _store.FindFeatureAsync(existingName, ct);

        var put = await _client.PutAsJsonAsync($"{WebApp.ApiBaseUrl}/features/{existingName}",
            new { IsEnabled = true, Filters = new[] { tooLong } }, ct);
        put.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var stored = await _store.UpdateFeatureAsync(existing! with { IsEnabled = true, Filters = [tooLong] }, ct);
        stored.Status.ShouldBe(FeatureFlagUpdateStatus.Rejected);
        stored.Message.ShouldNotBeNull();
        stored.Message.ShouldContain("4000");
        FeatureFlagStoreTests.ShouldHaveSameState(await _store.FindFeatureAsync(existingName, ct), existing);
    }

    [Fact]
    public async Task WhenFilterParametersAreNullCreateIsStillAccepted()
    {
        var featureName = $"FeatureNullParameters_{Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features",
            new { Name = featureName, IsEnabled = true, Filters = new[] { new FeatureFilterDto("Microsoft.Percentage", null) } },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("UnmappedMember", "{\"Dummy\":1}", false)]
    [InlineData("CamelCaseMember", "{\"ids\":[1]}", false)]
    [InlineData("ValidJson", "{\"Ids\":[1]}", true)]
    [InlineData("Null", null, true)]
    // Blank strings stay refused: server validation is unchanged by filters without defaults, so a client
    // must send null for an empty editor.
    [InlineData("Blank", "", false)]
    [InlineData("WhitespaceOnly", "   ", false)]
    public async Task WhenFilterIsRegisteredWithoutDefaultsItsParametersAreValidatedAgainstItsSettingsType(
        string parametersCase, string? parameters, bool accepted)
    {
        var ct = TestContext.Current.CancellationToken;
        FeatureFilterDto[] filters = [new("BlankTestFilter", parameters)];

        var postedName = $"BlankPost{parametersCase}_{Guid.NewGuid():N}";
        var post = await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features",
            new { Name = postedName, IsEnabled = true, Filters = filters }, ct);
        post.StatusCode.ShouldBe(accepted ? HttpStatusCode.Created : HttpStatusCode.BadRequest);
        (await _store.FindFeatureAsync(postedName, ct) is not null).ShouldBe(accepted);

        var putTarget = await CreateWithoutFiltersAsync($"BlankPut{parametersCase}");
        var put = await _client.PutAsJsonAsync($"{WebApp.ApiBaseUrl}/features/{putTarget.Name}",
            new { IsEnabled = true, Filters = filters }, ct);
        put.StatusCode.ShouldBe(accepted ? HttpStatusCode.OK : HttpStatusCode.BadRequest);

        var storeTarget = await CreateWithoutFiltersAsync($"BlankStore{parametersCase}");
        var update = await _store.UpdateFeatureAsync(storeTarget with { IsEnabled = true, Filters = [.. filters] }, ct);
        update.Status.ShouldBe(accepted ? FeatureFlagUpdateStatus.Updated : FeatureFlagUpdateStatus.Rejected);
        if (!accepted)
            FeatureFlagStoreTests.ShouldHaveSameState(await _store.FindFeatureAsync(storeTarget.Name, ct), storeTarget);
    }

    private async Task<FeatureFlagDto> CreateWithoutFiltersAsync(string prefix)
    {
        var ct = TestContext.Current.CancellationToken;
        var name = $"{prefix}_{Guid.NewGuid():N}";
        (await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features", new { Name = name, IsEnabled = false }, ct))
            .StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await _store.FindFeatureAsync(name, ct))!;
    }
}
