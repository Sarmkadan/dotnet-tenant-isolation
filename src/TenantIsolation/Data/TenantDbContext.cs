#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.EntityFrameworkCore;
using TenantIsolation.Models;

namespace TenantIsolation.Data;

/// <summary>
/// Entity Framework Core DbContext for tenant isolation system
/// </summary>
public class TenantDbContext : DbContext
{
    /// <summary>
    /// Current tenant identifier filter
    /// </summary>
    private Guid? _currentTenantId;

    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Set current tenant for query filtering
    /// </summary>
    public void SetCurrentTenant(Guid tenantId)
    {
        _currentTenantId = tenantId;
    }

    /// <summary>
    /// Clear current tenant filter
    /// </summary>
    public void ClearCurrentTenant()
    {
        _currentTenantId = null;
    }

    /// <summary>
    /// Get current tenant identifier
    /// </summary>
    public Guid? GetCurrentTenant()
    {
        return _currentTenantId;
    }

    // DbSets for domain models
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConfiguration> TenantConfigurations { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<DataIsolationPolicy> DataIsolationPolicies { get; set; }
    public DbSet<TenantFeature> TenantFeatures { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Tenant entity
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AdminEmail).IsRequired();
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
        });

        // Configure TenantConfiguration entity
        modelBuilder.Entity<TenantConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Key }).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Value).IsRequired();
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TenantConnectionString entity
        modelBuilder.Entity<TenantConnectionString>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.IsPrimary });
            entity.HasIndex(e => e.TenantId);
            entity.Property(e => e.ConnectionString).IsRequired();
            entity.Property(e => e.DatabaseType).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Organization entity
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Slug });
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContactEmail).IsRequired();
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Users)
                .WithOne(u => u.Organization)
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            entity.HasIndex(e => new { e.OrganizationId, e.IsActive });
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.LastLoginAt);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Preferences).HasColumnType("nvarchar(max)");
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure DataIsolationPolicy entity
        modelBuilder.Entity<DataIsolationPolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.EntityType });
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.PolicyType);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FilterRule).HasColumnType("nvarchar(max)");
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TenantFeature entity
        modelBuilder.Entity<TenantFeature>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.FeatureKey }).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.IsEnabled);
            entity.Property(e => e.FeatureKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure global query filters for soft deletes
        modelBuilder.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Organization>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);

        // Configure query filter for tenant isolation
        if (_currentTenantId.HasValue)
        {
            var tenantId = _currentTenantId.Value;
            modelBuilder.Entity<TenantConfiguration>().HasQueryFilter(e => e.TenantId == tenantId);
            modelBuilder.Entity<TenantConnectionString>().HasQueryFilter(e => e.TenantId == tenantId);
            modelBuilder.Entity<Organization>().HasQueryFilter(e => e.TenantId == tenantId);
            modelBuilder.Entity<User>().HasQueryFilter(e => e.TenantId == tenantId);
            modelBuilder.Entity<DataIsolationPolicy>().HasQueryFilter(e => e.TenantId == tenantId);
            modelBuilder.Entity<TenantFeature>().HasQueryFilter(e => e.TenantId == tenantId);
        }
    }

    /// <summary>
    /// Save changes with current tenant enforcement
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Set CreatedAt and UpdatedAt timestamps
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Tenant || entry.Entity is Organization || entry.Entity is User)
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Synchronous save changes override
    /// </summary>
    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Tenant || entry.Entity is Organization || entry.Entity is User)
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }
        }

        return base.SaveChanges();
    }
}
