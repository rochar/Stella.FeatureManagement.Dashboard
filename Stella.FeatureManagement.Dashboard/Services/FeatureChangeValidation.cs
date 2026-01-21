namespace Stella.FeatureManagement.Dashboard.Services;

internal sealed class FeatureChangeValidation(
    Func<FeatureFlagDto, FeatureChangeType, FeatureChangeValidationResult>
        featureChangeValidator)
    : IFeatureChangeValidation
{
    public FeatureChangeValidationResult CanProceed(FeatureFlagDto featureFlag, FeatureChangeType changeType)
    {
        return featureChangeValidator(featureFlag, changeType);
    }
}