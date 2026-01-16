using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Stella.FeatureManagement.Dashboard.Data;

/// <summary>
/// Design-time factory for creating <see cref="FeatureFlagDbContext"/> instances.
/// Used by EF Core tools for migrations.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FeatureFlagDbContext>
{
    /// <inheritdoc/>
    public FeatureFlagDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FeatureFlagDbContext>();

        // Default connection string for design-time operations
        // This can be overridden using environment variables or command-line arguments
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING")
            ?? "postgresql://postgres:~%2A_WJAB%7D6NaFAD11GRF_qC@localhost:5432/exampledb";

        optionsBuilder.UseNpgsql(connectionString);

        return new FeatureFlagDbContext(optionsBuilder.Options);
    }
}
