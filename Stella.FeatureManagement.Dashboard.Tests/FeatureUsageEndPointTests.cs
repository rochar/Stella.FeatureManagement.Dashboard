using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Stella.FeatureManagement.Dashboard.Data;
using System.Net;
using System.Net.Http.Json;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class FeatureUsageEndPointTests(WebApp webApp) : IClassFixture<WebApp>
{
    private const string UsageUrl = $"{WebApp.ApiBaseUrl}/usage";

    private readonly HttpClient _client = webApp.CreateClient();

    private readonly IDbContextFactory<FeatureFlagDbContext> _contextFactory =
        webApp.Services.GetRequiredService<IDbContextFactory<FeatureFlagDbContext>>();

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task WhenGetAllUsageReturnsLastSevenDaysZeroFilled()
    {
        // Arrange
        var featureName = await CreateFeatureAsync();
        await SeedUsageAsync(featureName,
            (Today, 5, 1),
            (Today.AddDays(-3), 2, 0),
            (Today.AddDays(-7), 100, 100)); // just outside the 7-day window

        // Act
        var usage = await _client.GetFromJsonAsync<List<FeatureUsageResponse>>(UsageUrl,
            TestContext.Current.CancellationToken);

        // Assert
        var feature = usage.ShouldNotBeNull().Single(u => u.Name == featureName);
        feature.Days.Select(d => d.Date).ShouldBe(Enumerable.Range(0, 7).Select(i => Today.AddDays(i - 6)));
        feature.Days[^1].ShouldBe(new DailyUsageResponse(Today, 5, 1));
        feature.Days[3].ShouldBe(new DailyUsageResponse(Today.AddDays(-3), 2, 0));
        feature.Days.Sum(d => d.EnabledCount + d.DisabledCount).ShouldBe(8);
    }

    [Fact]
    public async Task WhenGetAllUsageFeaturesWithoutUsageInWindowAreOmitted()
    {
        // Arrange
        var unusedFeature = await CreateFeatureAsync();
        var staleFeature = await CreateFeatureAsync();
        await SeedUsageAsync(staleFeature, (Today.AddDays(-10), 1, 1));

        // Act
        var usage = await _client.GetFromJsonAsync<List<FeatureUsageResponse>>(UsageUrl,
            TestContext.Current.CancellationToken);

        // Assert
        usage.ShouldNotBeNull().Select(u => u.Name).ShouldNotContain(unusedFeature);
        usage.Select(u => u.Name).ShouldNotContain(staleFeature);
    }

    [Fact]
    public async Task WhenGetFeatureUsageReturnsRequestedDays()
    {
        // Arrange
        var featureName = await CreateFeatureAsync();
        await SeedUsageAsync(featureName, (Today.AddDays(-29), 3, 4), (Today.AddDays(-30), 9, 9));

        // Act
        var usage = await _client.GetFromJsonAsync<FeatureUsageResponse>($"{UsageUrl}/{featureName}?days=30",
            TestContext.Current.CancellationToken);

        // Assert
        usage.ShouldNotBeNull().Name.ShouldBe(featureName);
        usage.Days.Count.ShouldBe(30);
        usage.Days[0].ShouldBe(new DailyUsageResponse(Today.AddDays(-29), 3, 4));
        usage.Days.Skip(1).ShouldAllBe(d => d.EnabledCount == 0 && d.DisabledCount == 0);
    }

    [Fact]
    public async Task WhenGetFeatureUsageWithoutUsageReturnsZeroFilledDays()
    {
        // Arrange
        var featureName = await CreateFeatureAsync();

        // Act
        var usage = await _client.GetFromJsonAsync<FeatureUsageResponse>($"{UsageUrl}/{featureName}",
            TestContext.Current.CancellationToken);

        // Assert
        usage.ShouldNotBeNull().Days.Count.ShouldBe(7);
        usage.Days.ShouldAllBe(d => d.EnabledCount == 0 && d.DisabledCount == 0);
    }

    [Fact]
    public async Task WhenGetUsageOfUnknownFeatureReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"{UsageUrl}/Unknown_{Guid.NewGuid():N}",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1000, 90)]
    public async Task WhenGetUsageWithOutOfRangeDaysClampsWindow(int days, int expectedDays)
    {
        // Arrange
        var featureName = await CreateFeatureAsync();

        // Act
        var usage = await _client.GetFromJsonAsync<FeatureUsageResponse>($"{UsageUrl}/{featureName}?days={days}",
            TestContext.Current.CancellationToken);

        // Assert
        usage.ShouldNotBeNull().Days.Count.ShouldBe(expectedDays);
        usage.Days[^1].Date.ShouldBe(Today);
    }

    private async Task<string> CreateFeatureAsync()
    {
        var featureName = $"UsageEndPoint_{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features",
            new { Name = featureName, IsEnabled = true }, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return featureName;
    }

    private async Task SeedUsageAsync(string featureName, params (DateOnly Date, long Enabled, long Disabled)[] days)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var featureId = await context.FeatureFlags
            .Where(f => f.Name == featureName)
            .Select(f => f.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

        context.FeatureFlagUsages.AddRange(days.Select(d => new FeatureFlagUsage
        {
            FeatureFlagId = featureId,
            Date = d.Date,
            EnabledCount = d.Enabled,
            DisabledCount = d.Disabled
        }));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed record FeatureUsageResponse(string Name, List<DailyUsageResponse> Days);

    private sealed record DailyUsageResponse(DateOnly Date, long EnabledCount, long DisabledCount);
}
