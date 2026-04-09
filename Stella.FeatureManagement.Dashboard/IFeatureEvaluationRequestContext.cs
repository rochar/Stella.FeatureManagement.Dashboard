namespace Stella.FeatureManagement.Dashboard;

/// <summary>
///     Provides access to the query string parameters from the current HTTP request
///     for use in custom feature filters via dependency injection.
///     Registered as a singleton and reads per-request data from <c>HttpContext.Items</c>,
///     following the same pattern as <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor" />.
/// </summary>
/// <example>
///     <code>
///     // In a custom feature filter:
///     public class MyFilter(IFeatureEvaluationRequestContext requestContext) : IFeatureFilter
///     {
///         public Task&lt;bool&gt; EvaluateAsync(FeatureFilterEvaluationContext context)
///         {
///             var userId = requestContext.Parameters.GetValueOrDefault("userId");
///             return Task.FromResult(userId is not null);
///         }
///     }
///     </code>
/// </example>
public interface IFeatureEvaluationRequestContext
{
    /// <summary>
    ///     Gets the query string parameters from the current feature evaluation HTTP request.
    ///     Each key-value pair represents a query string parameter name and its value.
    ///     Returns an empty dictionary when accessed outside of a feature evaluation request.
    /// </summary>
    IReadOnlyDictionary<string, string> Parameters { get; }
}

