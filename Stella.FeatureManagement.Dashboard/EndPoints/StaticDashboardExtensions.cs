using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Stella.FeatureManagement.Dashboard.EndPoints
{
    internal static class StaticDashboardExtensions
    {
        public static RouteGroupBuilder MapStaticDashboard(this RouteGroupBuilder routeGroup)
        {
            // Serve React SPA from embedded files
            var embeddedProvider = new ManifestEmbeddedFileProvider(
                typeof(EndpointRouteBuilderExtensions).Assembly, "wwwroot");

            routeGroup.MapGet("{**path}", (HttpContext context, string? path) =>
            {
                var encodedPath = context.Request.GetEncodedPathAndQuery(); // Path + query only

                if (string.IsNullOrEmpty(path))
                    return Results.Redirect(encodedPath.EndsWith("/", StringComparison.CurrentCultureIgnoreCase)
                        ? "index.html"
                        : "dashboard/index.html");

                var file = embeddedProvider.GetFileInfo(path);

                // SPA fallback: serve index.html for client-side routing
                if (!file.Exists || file.IsDirectory) file = embeddedProvider.GetFileInfo("index.html");

                if (!file.Exists) return Results.NotFound();

                var contentTypeProvider = new FileExtensionContentTypeProvider();
                if (!contentTypeProvider.TryGetContentType(file.Name, out var contentType))
                    contentType = "application/octet-stream";

                return Results.Stream(file.CreateReadStream(), contentType);
            }).ExcludeFromDescription();

            return routeGroup;
        }
    }
}
