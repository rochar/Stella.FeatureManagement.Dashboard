namespace Stella.FeatureManagement.Dashboard.Tests;

internal record FeatureStateResponse(string Name, bool IsEnabled, string? Description, List<FeatureFilterResponse> Filters);