using Microsoft.AspNetCore.Http;

namespace Stella.FeatureManagement.Dashboard;

/// <summary>
///     Default implementation of <see cref="IFeatureEvaluationRequestContext" />.
///     Registered as a singleton and reads per-request data from <c>HttpContext.Items</c>,
///     allowing safe injection into singleton feature filters.
/// </summary>
internal sealed class FeatureEvaluationRequestContext(IHttpContextAccessor httpContextAccessor) : IFeatureEvaluationRequestContext
{
    internal const string HttpContextItemsKey = "Stella.FeatureEvaluation.Parameters";

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Parameters =>
        httpContextAccessor.HttpContext?.Items[HttpContextItemsKey] as IReadOnlyDictionary<string, string> ?? Empty;
}
