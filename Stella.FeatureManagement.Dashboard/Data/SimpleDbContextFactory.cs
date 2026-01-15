using Microsoft.EntityFrameworkCore;

namespace Stella.FeatureManagement.Dashboard.Data;

/// <summary>
/// Simple DbContext factory implementation for creating DbContext instances.
/// </summary>
internal class SimpleDbContextFactory<TContext> : IDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly DbContextOptions<TContext> _options;

    public SimpleDbContextFactory(DbContextOptions<TContext> options)
    {
        _options = options;
    }

    public TContext CreateDbContext()
    {
        return (TContext)Activator.CreateInstance(typeof(TContext), _options)!;
    }
}
