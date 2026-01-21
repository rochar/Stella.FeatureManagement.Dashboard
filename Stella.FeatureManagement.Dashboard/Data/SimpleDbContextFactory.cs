using Microsoft.EntityFrameworkCore;

namespace Stella.FeatureManagement.Dashboard.Data;

/// <summary>
/// Simple DbContext factory implementation for creating DbContext instances.
/// </summary>
internal class SimpleDbContextFactory<TContext>(DbContextOptions<TContext> options) : IDbContextFactory<TContext>
    where TContext : DbContext
{
    public TContext CreateDbContext()
    {
        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }
}
