using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class GetApplicationsExtension
{
    public static RouteGroupBuilder MapGetApplications(this RouteGroupBuilder routeGroup)
    {
        routeGroup.MapGet("", async (IDbContextFactory<FeatureFlagDbContext> contextFactory) =>
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var applications = await context.FeatureFlags
                .Select(f => f.Application)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            return applications;
        });

        return routeGroup;
    }
}
