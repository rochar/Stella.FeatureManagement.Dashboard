using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Stella.FeatureManagement.Dashboard.Services;

internal sealed class FeatureChangeValidation(
    IFeatureFilterRepository filterRepository,
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

        
        if (changeType != FeatureChangeType.Delete && (featureFlag.Filters is not null and { Count: > 0 }))
        {
            foreach (var filter in featureFlag.Filters)
            {
                var registeredFilter = filterRepository.GetFilterByName(filter.FilterType);
                if (registeredFilter is not null)
                {
                    logger.LogDebug("Validating filter {FilterType} for feature {FeatureName}", filter.FilterType,
                        featureFlag.Name);

                    try
                    {
                        var settingsType = registeredFilter.SettingsType;

                        if (settingsType is null)
                        {
                            logger.LogDebug("Filter {FilterType} does not have a settings type, skipping parameter validation", filter.FilterType);
                            continue;
                        }

                        logger.LogDebug("Deserializing filter parameters for feature {FeatureName} as {SettingsType}",
                            featureFlag.Name, settingsType.Name);

                        var settings =
                            System.Text.Json.JsonSerializer.Deserialize(filter.Parameters ?? "{}", settingsType,
                                _options);

                        if (settings is null)
                        {
                            logger.LogWarning(
                                "Feature {FeatureName} filter parameters deserialized to null for {SettingsType}",
                                featureFlag.Name, settingsType.Name);
                            return new FeatureChangeValidationResult(true,
                                $"{featureFlag.Name} filter parameters are not valid JSON for {settingsType.Name}.");
                        }

                        logger.LogDebug("Filter parameters validated successfully for filter {FilterType} on feature {FeatureName}",
                            filter.FilterType, featureFlag.Name);
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        logger.LogWarning(ex,
                            "Feature {FeatureName} filter parameters are not valid JSON for {FilterType}",
                            featureFlag.Name, filter.FilterType);
                        return new FeatureChangeValidationResult(true,
                            $"{featureFlag.Name} filter parameters are not valid JSON for {filter.FilterType}.");
                    }
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