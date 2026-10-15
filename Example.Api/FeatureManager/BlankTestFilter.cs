using Microsoft.FeatureManagement;
using Stella.FeatureManagement.Dashboard;

namespace Example.Api.FeatureManager;

/// <summary>
///     Registered without default settings, so the dashboard opens a blank editor for it.
/// </summary>
[FilterAlias("BlankTestFilter")]
internal sealed class BlankTestFilter(IFeatureEvaluationRequestContext requestContext) : IFeatureFilter
{
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        // A filter saved blank has no parameters, so Get returns null: it then matches nobody.
        var settings = context.Parameters.Get<BlankTestFilterSettings>();

        var isEnabled = settings is not null
                        && requestContext.Parameters.TryGetValue("id", out var idValue)
                        && int.TryParse(idValue, out var id)
                        && settings.Ids.Contains(id);

        return Task.FromResult(isEnabled);
    }
}
