using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class GetUsageExtension
{
    internal const int DefaultDays = 7;
    internal const int MaxDays = 90;

    public static RouteGroupBuilder MapGetUsage(this RouteGroupBuilder routeGroup)
    {
        // All features in one query, served separately from the feature list so the list never waits on usage.
        // Features without usage in the window are omitted.
        routeGroup.MapGet("", async (int? days, IDbContextFactory<FeatureFlagDbContext> contextFactory, TimeProvider timeProvider) =>
        {
            var (from, to) = GetWindow(days, timeProvider);

            await using var context = await contextFactory.CreateDbContextAsync();
            var rows = await context.FeatureFlagUsages
                .AsNoTracking()
                .Where(u => u.Date >= from && u.Date <= to)
                .Select(u => new { u.FeatureFlag.Name, u.Date, u.EnabledCount, u.DisabledCount })
                .ToListAsync();

            return rows
                .GroupBy(r => r.Name)
                .Select(g => new FeatureUsageDto(g.Key,
                    ToDenseSeries(from, to, g.ToDictionary(r => r.Date, r => (r.EnabledCount, r.DisabledCount)))))
                .ToList();
        }).Produces<List<FeatureUsageDto>>(200);

        routeGroup.MapGet("{featureName}", async (string featureName, int? days,
            IDbContextFactory<FeatureFlagDbContext> contextFactory, TimeProvider timeProvider) =>
        {
            var (from, to) = GetWindow(days, timeProvider);

            await using var context = await contextFactory.CreateDbContextAsync();
            // One round trip: the feature (for the 404) and its usage rows in the window.
            var feature = await context.FeatureFlags
                .Where(f => f.Name == featureName)
                .Select(f => new
                {
                    Usages = f.Usages
                        .Where(u => u.Date >= from && u.Date <= to)
                        .Select(u => new { u.Date, u.EnabledCount, u.DisabledCount })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (feature is null)
                return Results.NotFound();

            var usage = feature.Usages.ToDictionary(u => u.Date, u => (u.EnabledCount, u.DisabledCount));
            return Results.Ok(new FeatureUsageDto(featureName, ToDenseSeries(from, to, usage)));
        }).Produces<FeatureUsageDto>(200)
        .Produces(404);

        return routeGroup;
    }

    /// <summary>
    /// Today (UTC) and the <c>days - 1</c> days before it, with <c>days</c> clamped to 1..<see cref="MaxDays"/>.
    /// </summary>
    private static (DateOnly From, DateOnly To) GetWindow(int? days, TimeProvider timeProvider)
    {
        var count = Math.Clamp(days ?? DefaultDays, 1, MaxDays);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return (today.AddDays(1 - count), today);
    }

    private static List<DailyUsageDto> ToDenseSeries(DateOnly from, DateOnly to,
        Dictionary<DateOnly, (long Enabled, long Disabled)> usage)
    {
        var series = new List<DailyUsageDto>(to.DayNumber - from.DayNumber + 1);
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var (enabled, disabled) = usage.GetValueOrDefault(date);
            series.Add(new DailyUsageDto(date, enabled, disabled));
        }

        return series;
    }
}
