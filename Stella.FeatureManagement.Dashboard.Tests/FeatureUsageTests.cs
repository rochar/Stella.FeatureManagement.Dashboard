using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Stella.FeatureManagement.Dashboard.Data;
using Stella.FeatureManagement.Dashboard.Services;
using System.Net;
using System.Net.Http.Json;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class FeatureUsageTests(WebApp webApp) : IClassFixture<WebApp>
{
    private readonly HttpClient _client = webApp.CreateClient();
    private readonly IFeatureUsageRecorder _recorder = webApp.Services.GetRequiredService<IFeatureUsageRecorder>();

    private readonly IDbContextFactory<FeatureFlagDbContext> _contextFactory =
        webApp.Services.GetRequiredService<IDbContextFactory<FeatureFlagDbContext>>();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WhenFeatureRequestedConcurrentlyCountsEveryCall(bool isEnabled)
    {
        // Arrange
        var featureName = await CreateFeatureAsync(isEnabled);
        const int numberOfRequests = 50;

        // Act
        await Task.WhenAll(Enumerable.Range(0, numberOfRequests)
            .Select(_ => _client.GetAsync($"features/{featureName}", TestContext.Current.CancellationToken)));
        await _recorder.FlushAsync(TestContext.Current.CancellationToken);

        // Assert
        var (enabled, disabled) = await GetUsageAsync(featureName);
        enabled.ShouldBe(isEnabled ? numberOfRequests : 0);
        disabled.ShouldBe(isEnabled ? 0 : numberOfRequests);
    }

    [Fact]
    public async Task WhenFlushedRepeatedlyCountsAccumulate()
    {
        // Arrange
        var featureName = await CreateFeatureAsync(true);

        // Act
        for (var i = 0; i < 3; i++)
        {
            await _client.GetAsync($"features/{featureName}", TestContext.Current.CancellationToken);
            await _recorder.FlushAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        var (enabled, _) = await GetUsageAsync(featureName);
        enabled.ShouldBe(3);
    }

    [Fact]
    public async Task WhenMultipleInstancesFlushConcurrentlyCountsAreSummed()
    {
        // Arrange - two recorders simulate two application instances sharing one database
        var featureName = await CreateFeatureAsync(true);
        using var instanceA = CreateRecorder();
        using var instanceB = CreateRecorder();
        const int rounds = 5;
        const int callsPerRound = 20;

        // Act
        for (var round = 0; round < rounds; round++)
        {
            for (var i = 0; i < callsPerRound; i++)
            {
                instanceA.Record(featureName, true);
                instanceB.Record(featureName, false);
            }

            await Task.WhenAll(
                instanceA.FlushAsync(TestContext.Current.CancellationToken),
                instanceB.FlushAsync(TestContext.Current.CancellationToken));
        }

        // Assert
        var (enabled, disabled) = await GetUsageAsync(featureName);
        enabled.ShouldBe(rounds * callsPerRound);
        disabled.ShouldBe(rounds * callsPerRound);
    }

    [Fact]
    public async Task WhenUnknownFeatureRequestedKnownFeaturesInSameFlushAreStillStored()
    {
        // Arrange
        var featureName = await CreateFeatureAsync(true);
        var unknownFeatureName = $"Unknown_{Guid.NewGuid():N}";

        // Act - both names land in the same batch; the unknown one must not fail the flush
        await _client.GetAsync($"features/{unknownFeatureName}", TestContext.Current.CancellationToken);
        await _client.GetAsync($"features/{featureName}", TestContext.Current.CancellationToken);
        await _recorder.FlushAsync(TestContext.Current.CancellationToken);

        // Assert
        (await GetUsageAsync(featureName)).Enabled.ShouldBe(1);
    }

    [Fact]
    public async Task WhenFeatureDeletedUsageIsDeleted()
    {
        // Arrange
        var featureName = await CreateFeatureAsync(true);
        await _client.GetAsync($"features/{featureName}", TestContext.Current.CancellationToken);
        await _recorder.FlushAsync(TestContext.Current.CancellationToken);
        var featureId = await GetFeatureIdAsync(featureName);
        (await GetUsageAsync(featureName)).Enabled.ShouldBe(1);

        // Act
        var response = await _client.DeleteAsync($"{WebApp.ApiBaseUrl}/features/{featureName}",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await using var context = await _contextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var exists = await context.FeatureFlagUsages
            .AnyAsync(u => u.FeatureFlagId == featureId, TestContext.Current.CancellationToken);
        exists.ShouldBeFalse();
    }

    private FeatureUsageRecorder CreateRecorder() =>
        new(_contextFactory, TimeProvider.System, NullLogger<FeatureUsageRecorder>.Instance);

    private async Task<string> CreateFeatureAsync(bool isEnabled)
    {
        var featureName = $"UsageFeature_{Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync($"{WebApp.ApiBaseUrl}/features",
            new { Name = featureName, IsEnabled = isEnabled }, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return featureName;
    }

    private async Task<int> GetFeatureIdAsync(string featureName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        return await context.FeatureFlags
            .Where(f => f.Name == featureName)
            .Select(f => f.Id)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    // Summed over all days so a test crossing UTC midnight still passes.
    private async Task<(long Enabled, long Disabled)> GetUsageAsync(string featureName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var usages = await context.FeatureFlagUsages
            .Where(u => u.FeatureFlag.Name == featureName)
            .ToListAsync(TestContext.Current.CancellationToken);
        return (usages.Sum(u => u.EnabledCount), usages.Sum(u => u.DisabledCount));
    }
}
