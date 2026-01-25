using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.FeatureManagement;
using Stella.FeatureManagement.Dashboard.Services;

namespace Stella.FeatureManagement.Dashboard.EndPoints;

internal static class GetFiltersExtension
{
    public static RouteGroupBuilder MapGetFilters(this RouteGroupBuilder routeGroup)
    {
        routeGroup.MapGet("", (IFeatureFilterRepository repository) =>
        {
            var filters = repository.GetFilters()
                .Select(registration => new FilterDto(registration.Name, registration.DefaultSettingsJson))
                .ToList();

            return Results.Ok(filters);
        }).Produces<List<FilterDto>>(200);

        return routeGroup;
    }
}
