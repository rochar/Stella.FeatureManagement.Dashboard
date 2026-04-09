using Microsoft.FeatureManagement;
using Stella.FeatureManagement.Dashboard;

namespace Example.Api.FeatureManager;

[FilterAlias("TestFilter")]
internal sealed class TestFilter(IFeatureEvaluationRequestContext requestContext) : IFeatureFilter
{
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        var settings = context.Parameters.Get<TestFilterSettings>();

        // Check if an "id" query string parameter matches any configured ID
        if (requestContext.Parameters.TryGetValue("id", out var idValue) &&
            int.TryParse(idValue, out var id))
        {
            return Task.FromResult(settings!.Ids.Contains(id));
        }

        var isEnabled = settings!.Ids.Contains(2);

        return Task.FromResult(isEnabled);
    }
}