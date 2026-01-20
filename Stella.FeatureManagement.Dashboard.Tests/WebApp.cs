using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stella.FeatureManagement.Dashboard.Data;
using Testcontainers.PostgreSql;

namespace Stella.FeatureManagement.Dashboard.Tests;

/// <summary>
/// Custom WebApplicationFactory that uses Testcontainers for PostgreSQL database.
/// </summary>
public class WebApp : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ApiBaseUrl = "features/dashboardapi";

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove all existing DbContextFactory registrations
            var descriptors = services
                .Where(d => d.ServiceType == typeof(IDbContextFactory<FeatureFlagDbContext>))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Add DbContextFactory with Testcontainers PostgreSQL
            services.AddDbContextFactory<FeatureFlagDbContext>(options =>
                options.UseNpgsql(_postgresContainer.GetConnectionString()));
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        // Apply migrations to create the database schema
        var options = new DbContextOptionsBuilder<FeatureFlagDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        await using var context = new FeatureFlagDbContext(options);
        await context.Database.MigrateAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
