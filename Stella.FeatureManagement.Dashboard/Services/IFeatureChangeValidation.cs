namespace Stella.FeatureManagement.Dashboard.Services;

internal interface IFeatureChangeValidation
{
    FeatureChangeValidationResult CanProceed(FeatureFlagDto featureFlag, FeatureChangeType changeType);
}