namespace Stella.FeatureManagement.Dashboard.Tests;

internal sealed record FeatureStateResponse(string Name, bool IsEnabled, string? Description, List<FeatureFilterResponse> Filters, string? Application = null, DateTime? CreatedAt = null, DateTime? UpdatedAt = null);