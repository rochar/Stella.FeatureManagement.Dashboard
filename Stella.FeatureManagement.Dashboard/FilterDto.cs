namespace Stella.FeatureManagement.Dashboard;

/// <summary>
/// Represents a registered feature filter.
/// </summary>
/// <param name="Name">The name of the filter (from FilterAliasAttribute or class name).</param>
/// <param name="DefaultSettings">Default settings for the filter as JSON string.</param>
public record FilterDto(string Name, string? DefaultSettings);