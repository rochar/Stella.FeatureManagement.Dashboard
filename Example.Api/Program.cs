using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Scalar.AspNetCore;
using Stella.FeatureManagement.Dashboard;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS with a named policy for the dashboard (allows any origin in development for Aspire)
const string dashboardCorsPolicy = "DashboardCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(dashboardCorsPolicy, policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.AddServiceDefaults();

// Add Feature Management with Dashboard 
builder.Services
    .AddFeatureManagement()
    .AddFeaturesDashboard(options => options.UseNpgsql(builder.Configuration.GetConnectionString("features")))
    .OnFeatureChanging((featureFlag, _) =>
    {
        if (featureFlag.Name == "MyFlag") // MyFlag is readonly
            return new FeatureChangeValidationResult(true, "MyFlag can not be updated!");

        if (featureFlag.Name == "FilteredFlag") //Validate a filter
            return ValidateFilteredFlag(featureFlag);

        return new FeatureChangeValidationResult(false, string.Empty);
    });

var app = builder.Build();

// Enable CORS middleware - must be before other middleware
app.UseCors(dashboardCorsPolicy);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapFeaturesDashboardEndpoints(configureCors: policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());
await app.MigrateFeaturesDatabaseAsync();
await app.InitializeFeaturesDashboardAsync((o) =>
{
    o.AddIfNotExists = new Dictionary<string, FeatureConfig>
    {
        { "MyFlag", new FeatureConfig(true, "My feature flag description") },
        { "AnotherFlag", new FeatureConfig(false) },
        {
            "FilteredFlag",
            new FeatureConfig(true, Filter: new FeatureFilterConfig("Microsoft.Percentage",
                new PercentageFilterSettings { Value = 50 }))
        }
    };
});

app.Run();

FeatureChangeValidationResult ValidateFilteredFlag(FeatureFlagDto featureFlagDto)
{
    var percentageFilter = featureFlagDto.Filters?.FirstOrDefault(f => f.FilterType == "Microsoft.Percentage");
    if (percentageFilter is null)
        return new FeatureChangeValidationResult(true, "FilteredFlag must have a Microsoft.Percentage filter.");

    try
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
        };
        var settings =
            System.Text.Json.JsonSerializer.Deserialize<PercentageFilterSettings>(percentageFilter.Parameters ??
                "{}", options);
        if (settings is null)
            return new FeatureChangeValidationResult(true,
                "FilteredFlag filter parameters are not valid JSON for PercentageFilterSettings.");
    }
    catch (System.Text.Json.JsonException)
    {
        return new FeatureChangeValidationResult(true,
            "FilteredFlag filter parameters are not valid JSON for PercentageFilterSettings.");
    }

    return new FeatureChangeValidationResult(false, string.Empty);
    ;
}

public partial class Program
{
}