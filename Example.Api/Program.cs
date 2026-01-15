using Microsoft.FeatureManagement;
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

// Add Feature Management
builder.Services
    .AddFeatureManagement()
    .AddDashboard();

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("exampledb");

var app = builder.Build();

// Enable CORS middleware - must be before other middleware
app.UseCors(dashboardCorsPolicy);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseDashboard();

app.Run();

// Make Program class accessible to test projects
public partial class Program
{
}