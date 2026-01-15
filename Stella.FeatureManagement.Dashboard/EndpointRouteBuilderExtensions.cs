using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.FeatureManagement;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/> to configure the Feature Management Dashboard endpoints.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Creates a route group and maps the Feature Management Dashboard endpoints to it.
    /// </summary>
    /// <param name="routeBuilder">The <see cref="IEndpointRouteBuilder"/> to add dashboard endpoints to.</param>
    /// <param name="group">The route prefix for the dashboard endpoints. Defaults to "/features".</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> so that additional calls can be chained.</returns>
    public static IEndpointRouteBuilder UseDashboard(this IEndpointRouteBuilder routeBuilder, string group = "/features")
    {
        var routeGroup = routeBuilder.MapGroup(group);

        // Serve React SPA from embedded files
        var embeddedProvider = new ManifestEmbeddedFileProvider(
            typeof(EndpointRouteBuilderExtensions).Assembly, "wwwroot");

        routeGroup.MapGet("dashboard/{**path}", (HttpContext context, string? path) =>
        {
            var encodedPath = context.Request.GetEncodedPathAndQuery(); // Path + query only

            if (string.IsNullOrEmpty(path))
            {
                return Results.Redirect(encodedPath.EndsWith("/", StringComparison.CurrentCultureIgnoreCase) ?
                    "index.html" : "dashboard/index.html");
            }

            var file = embeddedProvider.GetFileInfo(path);

            // SPA fallback: serve index.html for client-side routing
            if (!file.Exists || file.IsDirectory)
            {
                file = embeddedProvider.GetFileInfo("index.html");
            }

            if (!file.Exists)
            {
                return Results.NotFound();
            }

            var contentTypeProvider = new FileExtensionContentTypeProvider();
            if (!contentTypeProvider.TryGetContentType(file.Name, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return Results.Stream(file.CreateReadStream(), contentType);

        }).ExcludeFromDescription();


        routeGroup.MapGet("", async (IFeatureManager featureManager) =>
        {
            var features = new List<FeatureState>();
            await foreach (var featureName in featureManager.GetFeatureNamesAsync())
            {
                var isEnabled = await featureManager.IsEnabledAsync(featureName);
                features.Add(new FeatureState(featureName, isEnabled));
            }

            return features;
        }).Produces<List<FeatureState>>(200);

        routeGroup.MapGet("{featureName}", async (string featureName, IFeatureManager featureManager) =>
        {
            var isEnabled = await featureManager.IsEnabledAsync(featureName);
            return isEnabled;
        }).Produces<bool>(200);

        return routeBuilder;
    }

    private record FeatureState(string FeatureName, bool IsEnabled);
}