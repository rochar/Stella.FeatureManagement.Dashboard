namespace Stella.FeatureManagement.Dashboard;

public record FeatureChangeValidationResult(bool Cancel, string? CancellationMessage);