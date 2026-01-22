namespace Stella.FeatureManagement.Dashboard.Services;

internal interface IFeatureChangeValidation
{
    FeatureChangeValidationResult CanProceed(FeatureFlagDto featureFlag, FeatureChangeType changeType);
    void RegisterCustomValidation(Func<FeatureFlagDto, FeatureChangeType, FeatureChangeValidationResult> featureChangeValidator);
}