using Microsoft.EntityFrameworkCore;

namespace Stella.FeatureManagement.Dashboard.Data;

/// <summary>
/// Database context for feature flags.
/// </summary>
public class FeatureFlagDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="FeatureFlagDbContext"/>.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public FeatureFlagDbContext(DbContextOptions<FeatureFlagDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the feature flags.
    /// </summary>
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    /// <summary>
    /// Gets or sets the feature filters.
    /// </summary>
    public DbSet<FeatureFilter> FeatureFilters => Set<FeatureFilter>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("features");

        modelBuilder.Entity<FeatureFlag>(entity =>
        {
            entity.ToTable("FeatureFlags");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();

            entity.HasMany(e => e.Filters)
                .WithOne(f => f.FeatureFlag)
                .HasForeignKey(f => f.FeatureFlagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FeatureFilter>(entity =>
        {
            entity.ToTable("FeatureFilters");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FilterType).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Parameters).HasMaxLength(4000);
        });
    }
}
