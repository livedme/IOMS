using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Domain.Entities;

namespace TradeFlow.Infrastructure.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<T>> GetAllAsync(CancellationToken ct = default);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    IQueryable<T> Query();
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
    void SoftDelete(T entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    private static readonly string EntityName = typeof(T).Name;
    private const int SlowQueryEventId = 3000;

    protected readonly Data.ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;
    private readonly ILogger<Repository<T>> _logger;

    public Repository(Data.ApplicationDbContext context, ILogger<Repository<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FindAsync([id], ct);

    public async Task<List<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.AsNoTracking().ToListAsync(ct);

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        var started = Stopwatch.GetTimestamp();
        var results = await _dbSet.AsNoTracking().Where(predicate).ToListAsync(ct);
        LogQueryIfSlow("Find", results.Count, started);
        return results;
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, ct);

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.AsNoTracking().AnyAsync(predicate, ct);

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate == null
            ? await _dbSet.AsNoTracking().CountAsync(ct)
            : await _dbSet.AsNoTracking().CountAsync(predicate, ct);

    public IQueryable<T> Query() => _dbSet;

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        _logger.LogInformation("Staged {Entity} {EntityId} for insert", EntityName, entity.Id);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        await _dbSet.AddRangeAsync(entities, ct);
        _logger.LogInformation("Staged {Count} {Entity} records for insert", entities.Count(), EntityName);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
        _logger.LogInformation("Staged {Entity} {EntityId} for update", EntityName, entity.Id);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
        _logger.LogInformation("Staged {Entity} {EntityId} for delete", EntityName, entity.Id);
    }

    public void SoftDelete(T entity)
    {
        entity.IsDeleted = true;
        _dbSet.Update(entity);
        _logger.LogInformation("Soft-deleted {Entity} {EntityId}", EntityName, entity.Id);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        var started = Stopwatch.GetTimestamp();
        var affected = await _context.SaveChangesAsync(ct);
        _logger.LogInformation(
            "Persisted {ChangeCount} change(s) for {Entity} in {ElapsedMs:F0} ms",
            affected,
            EntityName,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds);
    }

    private void LogQueryIfSlow(string operation, int resultCount, long startedTimestamp)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds;
        if (elapsedMs < 400)
            return;

        _logger.LogWarning(
            SlowQueryEventId,
            "{Entity}.{Operation} returned {ResultCount} rows in {ElapsedMs:F0} ms",
            EntityName,
            operation,
            resultCount,
            elapsedMs);
    }
}
