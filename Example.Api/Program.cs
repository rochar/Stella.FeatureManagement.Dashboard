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

public partial class Program
{
}