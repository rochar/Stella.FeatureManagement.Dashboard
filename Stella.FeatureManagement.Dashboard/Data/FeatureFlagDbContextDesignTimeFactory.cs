using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Stella.FeatureManagement.Dashboard.Data;

/// <summary>
/// Design-time factory for creating <see cref="FeatureFlagDbContext"/> during migrations.
/// This is used by EF Core tools (dotnet ef migrations add, etc.)
/// </summary>
public class FeatureFlagDbContextDesignTimeFactory : IDesignTimeDbContextFactory<FeatureFlagDbContext>
{
    /// <inheritdoc/>
    public FeatureFlagDbContext CreateDbContext(string[] args)
    {
        // Default to SQLite for design-time operations (migrations generation)
        // The actual provider is configured by the consuming application
        var optionsBuilder = new DbContextOptionsBuilder<FeatureFlagDbContext>();
        optionsBuilder.UseSqlite("Data Source=design_time.db");

        return new FeatureFlagDbContext(optionsBuilder.Options);
    }
}
