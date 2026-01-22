using Microsoft.FeatureManagement;

namespace Example.Api.FeatureManager;

[FilterAlias("TestFilter")]
internal sealed class TestFilter(IHttpContextAccessor httpContextAccessor) : IFeatureFilter
{
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {

        var settings = context.Parameters.Get<TestFilterSettings>();

        var isEnabled = settings!.Ids.Contains(2);

        return Task.FromResult(isEnabled);
    }
}