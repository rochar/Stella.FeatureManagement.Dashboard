namespace Stella.FeatureManagement.Dashboard.Services;

internal sealed class NoValidationFeatureChangeValidation()
    : IFeatureChangeValidation
{
    private readonly FeatureChangeValidationResult _result = new(false, string.Empty);

    public FeatureChangeValidationResult CanProceed(FeatureFlagDto featureFlag, FeatureChangeType changeType) => _result;
}