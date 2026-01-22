namespace Stella.FeatureManagement.Dashboard.Services;

internal sealed class FeatureChangeValidation(
    Func<FeatureFlagDto, FeatureChangeType, FeatureChangeValidationResult>
        featureChangeValidator,
    IManagedFeatureRegistration featureRegistration)
    : IFeatureChangeValidation
{
    private readonly System.Text.Json.JsonSerializerOptions _options = new()
    {
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };

    public FeatureChangeValidationResult CanProceed(FeatureFlagDto featureFlag, FeatureChangeType changeType)
    {

        if (changeType != FeatureChangeType.Delete && featureRegistration.IsFeatureRegistered(featureFlag.Name))
        {
            var registeredFilter = featureRegistration.GetFeatureRegistration(featureFlag.Name);
            if (registeredFilter is not null)
            {
                //assume only one filter per feature for managed features, ui limitation
                var newFilter = featureFlag.Filters?.FirstOrDefault(f => f.FilterType == registeredFilter.TypeName);
                //filter is mandatory by default
                if (newFilter is null)
                    return new FeatureChangeValidationResult(true,
                        $"{featureFlag.Name} must have a {registeredFilter.TypeName} filter.");

                try
                {
                    var settingsType = registeredFilter.DefaultSettings.GetType();
                    var settings =
                        System.Text.Json.JsonSerializer.Deserialize(newFilter.Parameters ?? "{}", settingsType, _options);

                    if (settings is null)
                        return new FeatureChangeValidationResult(true,
                            $"{featureFlag.Name} filter parameters are not valid JSON for {settingsType.Name}.");
                }
                catch (System.Text.Json.JsonException)
                {
                    return new FeatureChangeValidationResult(true,
                        $"{featureFlag.Name} filter parameters are not valid JSON for {registeredFilter.DefaultSettings.GetType().Name}.");
                }
            }
        }

        return featureChangeValidator(featureFlag, changeType);
    }
}