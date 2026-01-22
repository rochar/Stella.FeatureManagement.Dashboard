using Microsoft.Extensions.Logging;

namespace Stella.FeatureManagement.Dashboard.Services;

internal sealed class FeatureChangeValidation(
    IManagedFeatureRegistration featureRegistration,
    ILogger<FeatureChangeValidation> logger) : IFeatureChangeValidation
{
    private readonly System.Text.Json.JsonSerializerOptions _options = new()
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    private Func<FeatureFlagDto, FeatureChangeType, FeatureChangeValidationResult>? _customValidation;

    public FeatureChangeValidationResult CanProceed(FeatureFlagDto featureFlag, FeatureChangeType changeType)
    {
        logger.LogDebug("Validating {ChangeType} operation for feature {FeatureName}", changeType, featureFlag.Name);

        if (changeType != FeatureChangeType.Delete && featureRegistration.IsFeatureRegistered(featureFlag.Name))
        {
            logger.LogDebug("Feature {FeatureName} is a registered managed feature, validating filters",
                featureFlag.Name);

            var registeredFilter = featureRegistration.GetFeatureRegistration(featureFlag.Name);
            if (registeredFilter is not null)
            {
                logger.LogDebug("Feature {FeatureName} has registered filter {FilterType}", featureFlag.Name,
                    registeredFilter.TypeName);

                //assume only one filter per feature for managed features, ui limitation
                var newFilter = featureFlag.Filters?.FirstOrDefault(f => f.FilterType == registeredFilter.TypeName);
                //filter is mandatory by default
                if (newFilter is null)
                {
                    logger.LogWarning("Feature {FeatureName} is missing required filter {FilterType}", featureFlag.Name,
                        registeredFilter.TypeName);
                    return new FeatureChangeValidationResult(true,
                        $"{featureFlag.Name} must have a {registeredFilter.TypeName} filter.");
                }

                try
                {
                    var settingsType = registeredFilter.DefaultSettings.GetType();
                    logger.LogDebug("Deserializing filter parameters for feature {FeatureName} as {SettingsType}",
                        featureFlag.Name, settingsType.Name);

                    var settings =
                        System.Text.Json.JsonSerializer.Deserialize(newFilter.Parameters ?? "{}", settingsType,
                            _options);

                    if (settings is null)
                    {
                        logger.LogWarning(
                            "Feature {FeatureName} filter parameters deserialized to null for {SettingsType}",
                            featureFlag.Name, settingsType.Name);
                        return new FeatureChangeValidationResult(true,
                            $"{featureFlag.Name} filter parameters are not valid JSON for {settingsType.Name}.");
                    }

                    logger.LogDebug("Filter parameters validated successfully for feature {FeatureName}",
                        featureFlag.Name);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    logger.LogWarning(ex,
                        "Feature {FeatureName} filter parameters are not valid JSON for {SettingsType}",
                        featureFlag.Name, registeredFilter.DefaultSettings.GetType().Name);
                    return new FeatureChangeValidationResult(true,
                        $"{featureFlag.Name} filter parameters are not valid JSON for {registeredFilter.DefaultSettings.GetType().Name}.");
                }
            }
        }

        if (_customValidation is not null)
        {
            var result = _customValidation(featureFlag, changeType);

            if (result.Cancel)
                logger.LogDebug(
                    "Custom validator cancelled {ChangeType} for feature {FeatureName}: {CancellationMessage}",
                    changeType,
                    featureFlag.Name, result.CancellationMessage);
            else
                logger.LogDebug("Custom validation passed for {ChangeType} operation on feature {FeatureName}",
                    changeType,
                    featureFlag.Name);

            return result;
        }

        return new FeatureChangeValidationResult(false, string.Empty);
    }

    public void RegisterCustomValidation(
        Func<FeatureFlagDto, FeatureChangeType, FeatureChangeValidationResult> featureChangeValidator)
    {
        _customValidation = featureChangeValidator;
    }
}