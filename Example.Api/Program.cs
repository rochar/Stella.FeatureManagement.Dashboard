using Example.Api.FeatureManager;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Scalar.AspNetCore;
using Stella.FeatureManagement.Dashboard;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

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
    .AddFeaturesDashboard(options => options.UseNpgsql(builder.Configuration.GetConnectionString("features")));


var app = builder.Build();

// Enable CORS middleware - must be before other middleware
app.UseCors(dashboardCorsPolicy);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var featureDashboard = app.UseFeaturesDashboard(configureCors: policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader())
    .OnFeatureChanging((featureFlag, _) =>
    {
        if (featureFlag.Name == "MyFlag") // MyFlag is readonly
            return new FeatureChangeValidationResult(true, "MyFlag can not be updated!");

        return new FeatureChangeValidationResult(false, string.Empty);
    });
;

await featureDashboard.MigrateFeaturesDatabaseAsync();
await featureDashboard.RegisterManagedFeaturesAsync([
    new ManagedFeature("CustomFilterFlag", string.Empty, true,
        new FilterOptions("TestFilter", new TestFilterSettings { Ids = [3, 4] })),
    new ManagedFeature("MyFlag", "My feature flag description", true),
    new ManagedFeature("AnotherFlag", string.Empty, false),
    new ManagedFeature("FilteredFlag", string.Empty, true,
        new FilterOptions("Microsoft.Percentage", new PercentageFilterSettings { Value = 50 }))
]);
app.Run();


public partial class Program
{
}