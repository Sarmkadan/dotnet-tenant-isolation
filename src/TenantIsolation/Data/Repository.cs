#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection; // For service provider to resolve factory

namespace TenantIsolation.Data;

/// <summary>
/// Generic repository for CRUD operations with tenant isolation
/// </summary>
public abstract class Repository<TEntity> where TEntity : class
{
    private readonly ITenantDbContextFactory<TenantDbContext> _contextFactory;
    private TenantDbContext? _context; // Lazy-loaded DbContext instance

    protected TenantDbContext Context
    {
        get
        {
            // Create a new DbContext instance on first access within the current scope,
            // ensuring it's tenant-aware.
            _context ??= _contextFactory.Create();
            return _context;
        }
    }

    protected readonly DbSet<TEntity> DbSet;

    protected Repository(ITenantDbContextFactory<TenantDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        DbSet = Context.Set<TEntity>(); // Use the lazy-loaded Context
    }

    /// <summary>
    /// Get entity by primary key
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    /// <summary>
    /// Get all entities
    /// </summary>
    public virtual async Task<List<TEntity>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    /// <summary>
    /// Get entities with pagination
    /// </summary>
    public virtual async Task<(List<TEntity> items, int total)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var total = await DbSet.CountAsync();
        var items = await DbSet
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <summary>
    /// Find entities matching criteria
    /// </summary>
    public virtual async Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    /// <summary>
    /// Find first entity matching criteria
    /// </summary>
    public virtual async Task<TEntity?> FindFirstAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await DbSet.FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// Add new entity
    /// </summary>
    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Add multiple entities
    /// </summary>
    public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await DbSet.AddRangeAsync(entities);
        await Context.SaveChangesAsync();
        return entities;
    }

    /// <summary>
    /// Update entity
    /// </summary>
    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Delete entity by primary key
    /// </summary>
    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity == null)
            return false;

        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Delete entity
    /// </summary>
    public virtual async Task<bool> DeleteAsync(TEntity entity)
    {
        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Delete multiple entities
    /// </summary>
    public virtual async Task<int> DeleteRangeAsync(IEnumerable<TEntity> entities)
    {
        DbSet.RemoveRange(entities);
        return await Context.SaveChangesAsync();
    }

    /// <summary>
    /// Check if entity exists
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await DbSet.AnyAsync(predicate);
    }

    /// <summary>
    /// Count entities matching criteria
    /// </summary>
    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        return predicate == null
            ? await DbSet.CountAsync()
            : await DbSet.CountAsync(predicate);
    }

    /// <summary>
    /// Get queryable for custom queries
    /// </summary>
    public virtual IQueryable<TEntity> AsQueryable()
    {
        return DbSet.AsQueryable();
    }

    /// <summary>
    /// Execute bulk operation
    /// </summary>
    public virtual async Task<int> BulkUpdateAsync(
        Expression<Func<TEntity, bool>> predicate,
        Action<UpdateSettersBuilder<TEntity>> setPropertyCalls)
    {
        return await DbSet.Where(predicate).ExecuteUpdateAsync(setPropertyCalls);
    }

    /// <summary>
    /// Execute bulk delete operation
    /// </summary>
    public virtual async Task<int> BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await DbSet.Where(predicate).ExecuteDeleteAsync();
    }
}
