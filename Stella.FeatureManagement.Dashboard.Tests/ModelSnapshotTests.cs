using Microsoft.EntityFrameworkCore;
using Shouldly;
using Stella.FeatureManagement.Dashboard.Data;

namespace Stella.FeatureManagement.Dashboard.Tests;

public class ModelSnapshotTests
{
    [Fact]
    public void WhenModelIsComparedToSnapshotNoMigrationIsPending()
    {
        // The model diff never opens a connection, so a placeholder connection string is enough.
        var options = new DbContextOptionsBuilder<FeatureFlagDbContext>()
            .UseNpgsql("Host=localhost;Database=model-snapshot-only")
            .Options;

        using var context = new FeatureFlagDbContext(options);

        context.Database.HasPendingModelChanges().ShouldBeFalse();
    }
}
