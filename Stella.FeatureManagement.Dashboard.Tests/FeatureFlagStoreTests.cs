using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Example.Api.FeatureManager;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Stella.FeatureManagement.Dashboard.Data;
using Stella.FeatureManagement.Dashboard.Services;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class FeatureFlagStoreTests(WebApp webApp) : IClassFixture<WebApp>
{
    private readonly HttpClient _client = webApp.CreateClient();
    private readonly IServiceProvider _services = webApp.Services;
    private readonly IFeatureFlagStore _store = webApp.Services.GetRequiredService<IFeatureFlagStore>();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task WhenStoreUpdatesWithCurrentUpdatedAtItWritesAndAdvancesUpdatedAt()
    {
        var original = await CreateAsync("StoreUpdate", TestFilterIds(1));

        var result = await _store.UpdateFeatureAsync(original with
        {
            IsEnabled = true,
            Description = "updated",
            Application = "Billing",
            Filters = [TestFilterIds(7), TestFilterIds(3)]
        }, Ct);

        result.Status.ShouldBe(FeatureFlagUpdateStatus.Updated);
        result.Feature.ShouldNotBeNull();
        result.Feature.UpdatedAt!.Value.ShouldBeGreaterThan(original.UpdatedAt!.Value);

        var stored = await _store.FindFeatureAsync(original.Name, Ct);
        stored.ShouldNotBeNull();
        stored.IsEnabled.ShouldBeTrue();
        stored.Description.ShouldBe("updated");
        stored.Application.ShouldBe("Billing");
        stored.Filters.ShouldBe([TestFilterIds(7), TestFilterIds(3)]);
        stored.UpdatedAt.ShouldBe(result.Feature.UpdatedAt);
    }

    [Fact]
    public async Task WhenStoreUpdatesWithStaleUpdatedAtItReturnsConflictWithCurrentFeature()
    {
        var original = await CreateAsync("StoreStale");
        var first = await _store.UpdateFeatureAsync(original with { IsEnabled = true }, Ct);
        first.Status.ShouldBe(FeatureFlagUpdateStatus.Updated);

        var result = await _store.UpdateFeatureAsync(original with { Description = "from a stale read" }, Ct);

        result.Status.ShouldBe(FeatureFlagUpdateStatus.Conflict);
        result.Message.ShouldNotBeNullOrWhiteSpace();
        ShouldHaveSameState(result.Feature, first.Feature);
        ShouldHaveSameState(await _store.FindFeatureAsync(original.Name, Ct), first.Feature);
    }

    [Fact]
    public async Task WhenTwoStoreUpdatesRaceOnTheSameUpdatedAtExactlyOneWins()
    {
        // No initial filters: deleting a shared filter row would also detect the race, and this test is
        // about the UpdatedAt predicate alone.
        var original = await CreateAsync("StoreRace");
        var updater = _services.GetRequiredService<FeatureFlagUpdater>();
        var contextFactory = _services.GetRequiredService<IDbContextFactory<FeatureFlagDbContext>>();
        await using var contextA = await contextFactory.CreateDbContextAsync(Ct);
        await using var contextB = await contextFactory.CreateDbContextAsync(Ct);

        // Both writers load before either saves, so both pass the state check and only the database's
        // UpdatedAt predicate can stop the second one.
        var loadedA = await FeatureFlagUpdater.LoadAsync(contextA, original.Name, Ct);
        var loadedB = await FeatureFlagUpdater.LoadAsync(contextB, original.Name, Ct);
        var requestA = original with { Filters = [TestFilterIds(11)] };
        var requestB = original with { Filters = [TestFilterIds(22)] };

        var results = await Task.WhenAll(
            updater.UpdateAsync(contextA, loadedA!, requestA, original.UpdatedAt, Ct),
            updater.UpdateAsync(contextB, loadedB!, requestB, original.UpdatedAt, Ct));

        results.Select(r => r.Status).Order().ToArray().ShouldBe([FeatureFlagUpdateStatus.Updated, FeatureFlagUpdateStatus.Conflict]);
        var winner = results[0].Status == FeatureFlagUpdateStatus.Updated ? requestA : requestB;
        var stored = await _store.FindFeatureAsync(original.Name, Ct);
        stored.ShouldNotBeNull();
        stored.Filters.ShouldBe(winner.Filters);
    }

    [Fact]
    public async Task WhenTheClockHasNotMovedStoreUpdateStillAdvancesUpdatedAt()
    {
        var original = await CreateAsync("StoreClock");
        var ahead = await MoveUpdatedAtAheadOfNowAsync(_services, original.Name);

        var result = await _store.UpdateFeatureAsync(original with { UpdatedAt = ahead, IsEnabled = true }, Ct);

        result.Status.ShouldBe(FeatureFlagUpdateStatus.Updated);
        result.Feature!.UpdatedAt.ShouldBe(ahead.AddTicks(TimeSpan.TicksPerMicrosecond));
        (await _store.FindFeatureAsync(original.Name, Ct))!.UpdatedAt.ShouldBe(result.Feature.UpdatedAt);
    }

    [Theory]
    [InlineData("CurrentToken")]
    [InlineData("StaleTokenWithStoredState")]
    public async Task WhenStoreUpdateEqualsStoredStateItReturnsUnchangedAndKeepsUpdatedAt(string tokenCase)
    {
        var original = await CreateAsync("StoreUnchanged", TestFilterIds(1), TestFilterIds(2));
        var current = (await _store.UpdateFeatureAsync(original with { Description = "stored" }, Ct)).Feature!;
        var token = tokenCase == "CurrentToken" ? current.UpdatedAt : original.UpdatedAt;

        // A fresh list with fresh filter instances: equality must be by value, field by field.
        var request = current with
        {
            UpdatedAt = token,
            Filters = [TestFilterIds(1), TestFilterIds(2)]
        };
        var result = await _store.UpdateFeatureAsync(request, Ct);

        result.Status.ShouldBe(FeatureFlagUpdateStatus.Unchanged);
        ShouldHaveSameState(result.Feature, current);
        (await _store.FindFeatureAsync(original.Name, Ct))!.UpdatedAt.ShouldBe(current.UpdatedAt);
    }

    [Fact]
    public async Task WhenStoreUpdatesUnknownFeatureItReturnsNotFound()
    {
        var result = await _store.UpdateFeatureAsync(
            new FeatureFlagDto($"Missing_{Guid.NewGuid():N}", true, null, []) { UpdatedAt = DateTime.UtcNow }, Ct);

        result.Status.ShouldBe(FeatureFlagUpdateStatus.NotFound);
        result.Feature.ShouldBeNull();
    }

    [Fact]
    public async Task WhenStoreUpdateHasNullFiltersItThrowsArgumentException()
    {
        var original = await CreateAsync("StoreNullFilters", TestFilterIds(1));

        await Should.ThrowAsync<ArgumentException>(() =>
            _store.UpdateFeatureAsync(original with { Filters = null, IsEnabled = true }, Ct));

        ShouldHaveSameState(await _store.FindFeatureAsync(original.Name, Ct), original);
    }

    [Theory]
    [InlineData("Null")]
    [InlineData("Local")]
    [InlineData("Unspecified")]
    public async Task WhenStoreUpdateHasNullOrNonUtcUpdatedAtItThrowsArgumentException(string kindCase)
    {
        var original = await CreateAsync("StoreBadToken");
        var utc = original.UpdatedAt!.Value;
        DateTime? token = kindCase switch
        {
            "Null" => null,
            "Local" => utc.ToLocalTime(),
            _ => DateTime.SpecifyKind(utc, DateTimeKind.Unspecified)
        };

        await Should.ThrowAsync<ArgumentException>(() =>
            _store.UpdateFeatureAsync(original with { UpdatedAt = token, IsEnabled = true }, Ct));

        ShouldHaveSameState(await _store.FindFeatureAsync(original.Name, Ct), original);
    }

    [Fact]
    public async Task WhenStoreUpdateHasCamelCaseParametersItReturnsRejected()
    {
        var original = await CreateAsync("StoreCamelCase");

        var result = await _store.UpdateFeatureAsync(
            original with { IsEnabled = true, Filters = [new FeatureFilterDto("TestFilter", "{\"ids\":[1]}")] }, Ct);

        result.Status.ShouldBe(FeatureFlagUpdateStatus.Rejected);
        result.Message.ShouldNotBeNullOrWhiteSpace();
        ShouldHaveSameState(await _store.FindFeatureAsync(original.Name, Ct), original);
    }

    [Fact]
    public async Task WhenCustomValidatorCancelsStoreUpdateItReturnsRejected()
    {
        // Example.Api registers OnFeatureChanging to refuse every change to MyFlag.
        var myFlag = await _store.FindFeatureAsync("MyFlag", Ct);
        myFlag.ShouldNotBeNull();

        var result = await _store.UpdateFeatureAsync(myFlag with { Description = "changed" }, Ct);

        result.Status.ShouldBe(FeatureFlagUpdateStatus.Rejected);
        result.Message.ShouldBe("MyFlag can not be updated!");
        ShouldHaveSameState(await _store.FindFeatureAsync("MyFlag", Ct), myFlag);
    }

    [Fact]
    public async Task WhenStoreValidatesFeatureItReportsTheSameRejectionAsUpdate()
    {
        var original = await CreateAsync("StoreValidate");
        var invalid = original with { IsEnabled = true, Filters = [new FeatureFilterDto("TestFilter", "{\"ids\":[1]}")] };
        var valid = original with { IsEnabled = true, Filters = [TestFilterIds(1)] };

        var invalidValidation = _store.ValidateFeature(invalid);
        var validValidation = _store.ValidateFeature(valid);

        invalidValidation.Cancel.ShouldBeTrue();
        validValidation.Cancel.ShouldBeFalse();
        ShouldHaveSameState(await _store.FindFeatureAsync(original.Name, Ct), original);

        var update = await _store.UpdateFeatureAsync(invalid, Ct);
        update.Status.ShouldBe(FeatureFlagUpdateStatus.Rejected);
        update.Message.ShouldBe(invalidValidation.CancellationMessage);
    }

    [Fact]
    public async Task WhenStoreListsFeaturesItReturnsEveryFeatureWithUtcUpdatedAt()
    {
        var created = await CreateAsync("StoreList");

        var features = await _store.ListFeaturesAsync(Ct);
        var found = await _store.FindFeatureAsync(created.Name, Ct);

        features.ShouldContain(f => f.Name == created.Name);
        features.ShouldContain(f => f.Name == "MyFlag");
        features.ShouldAllBe(f => f.CreatedAt!.Value.Kind == DateTimeKind.Utc && f.UpdatedAt!.Value.Kind == DateTimeKind.Utc);
        found.ShouldNotBeNull();
        found.CreatedAt!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        found.UpdatedAt!.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    private async Task<FeatureFlagDto> CreateAsync(string prefix, params FeatureFilterDto[] filters)
    {
        var name = $"{prefix}_{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features",
            new { Name = name, IsEnabled = false, Filters = filters }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await _store.FindFeatureAsync(name, Ct);
        created.ShouldNotBeNull();
        return created;
    }

    /// <summary>
    /// Simulates a writer whose clock runs ahead of this one: the stored UpdatedAt is later than "now".
    /// </summary>
    internal static async Task<DateTime> MoveUpdatedAtAheadOfNowAsync(IServiceProvider services, string featureName)
    {
        var ahead = FeatureFlag.UtcNowForStorage().AddHours(1);
        var contextFactory = services.GetRequiredService<IDbContextFactory<FeatureFlagDbContext>>();
        await using var context = await contextFactory.CreateDbContextAsync(Ct);
        await context.FeatureFlags
            .Where(f => f.Name == featureName)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.UpdatedAt, ahead), Ct);
        return ahead;
    }

    /// <summary>
    /// Record equality would compare the <see cref="FeatureFlagDto.Filters"/> lists by reference.
    /// </summary>
    internal static void ShouldHaveSameState(FeatureFlagDto? actual, FeatureFlagDto? expected)
    {
        actual.ShouldNotBeNull();
        expected.ShouldNotBeNull();
        (actual with { Filters = null }).ShouldBe(expected with { Filters = null });
        (actual.Filters ?? []).ShouldBe(expected.Filters ?? []);
    }

    internal static FeatureFilterDto TestFilterIds(params int[] ids) =>
        new("TestFilter", JsonSerializer.Serialize(new TestFilterSettings { Ids = ids }));
}
