using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradeFlow.Infrastructure.Data;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Tenant-scoped cache with a two-level hierarchy: an in-process <see cref="IMemoryCache"/> (L1)
/// in front of <see cref="IDistributedCache"/> (L2). L1 serves repeat reads inside a single
/// instance without a network hop, while L2 keeps instances consistent when Redis is configured.
/// </summary>
public interface ITenantCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan lifetime, CancellationToken cancellationToken = default);

    /// <summary>Removes a single key from both levels.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every key registered under <paramref name="prefix"/> for the current tenant. Used to
    /// invalidate whole feature areas (for example every <c>products:*</c> entry) after a write.
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read-through helper that returns the cached value or invokes <paramref name="factory"/>,
    /// stores its result, and returns it. Concurrent callers for the same key collapse onto a
    /// single factory invocation (cache-stampede protection).
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan lifetime,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default);
}

public sealed class TenantCache : ITenantCache
{
    public const string KeyPrefix = "ioms:tenant";
    private const int CacheHitEventId = 2000;
    private const int CacheMissEventId = 2001;
    private const int CacheEvictionEventId = 2002;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FactoryLocks = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> KnownKeys = new(StringComparer.Ordinal);

    private readonly IDistributedCache _cache;
    private readonly IMemoryCache _memoryCache;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantCache> _logger;
    private readonly CacheOptions _options;

    public TenantCache(
        IDistributedCache cache,
        IMemoryCache memoryCache,
        ITenantProvider tenantProvider,
        IOptions<CacheOptions> options,
        ILogger<TenantCache> logger)
    {
        _cache = cache;
        _memoryCache = memoryCache;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = BuildKey(key);
        if (fullKey is null)
            return default;

        if (_memoryCache.TryGetValue(fullKey, out object? l1) && l1 is T l1Value)
        {
            LogHit(key, "L1", l1Value);
            return l1Value;
        }

        try
        {
            var payload = await _cache.GetStringAsync(fullKey, cancellationToken);
            if (payload is null)
            {
                LogMiss(key);
                return default;
            }

            var value = JsonSerializer.Deserialize<T>(payload, SerializerOptions);
            if (value is not null)
                StoreInMemory(fullKey, value, _options.MemoryLifetime);
            return value;
        }
        catch (JsonException ex)
        {
            // A poisoned entry (schema change, truncated write) must not take the request down.
            _logger.LogWarning(ex, "Discarding unreadable cache entry for '{CacheKey}'.", key);
            await RemoveAsync(key, cancellationToken);
            return default;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The cache is an optimisation, not a dependency. If Redis is unreachable every read
            // would otherwise throw and take the page down with it, so a distributed-cache outage
            // degrades to "cache miss" and the caller falls back to the database.
            _logger.LogWarning(ex, "Distributed cache read failed for '{CacheKey}'; treating as a miss.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        var fullKey = BuildKey(key);
        if (fullKey is null)
            return;

        StoreInMemory(fullKey, value, lifetime);

        try
        {
            var payload = JsonSerializer.Serialize(value, SerializerOptions);
            var entryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lifetime
            };
            await _cache.SetStringAsync(fullKey, payload, entryOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The cache is an optimisation; a write failure must not fail the business operation.
            _logger.LogWarning(ex, "Failed to write cache entry '{CacheKey}'.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = BuildKey(key);
        if (fullKey is null)
            return;

        _memoryCache.Remove(fullKey);
        KnownKeys.TryRemove(fullKey, out _);

        try
        {
            await _cache.RemoveAsync(fullKey, cancellationToken);
            _logger.LogDebug(CacheEvictionEventId, "Evicted cache entry '{CacheKey}'.", key);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to evict cache entry '{CacheKey}'.", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // KNOWN LIMITATION (single-instance safe, multi-instance best-effort): prefix eviction can
        // only remove keys this process has observed, because IDistributedCache cannot enumerate.
        // On another instance a matching entry stays valid until its TTL expires. Bounded by
        // CacheOptions this is at most MemoryLifetime for the L1 copy and the caller's lifetime for
        // the L2 copy. Making this exact across instances needs either a generation token folded
        // into the key or Redis pub/sub invalidation; neither is wired up yet.
        var tenantPrefix = BuildPrefix(prefix);
        if (tenantPrefix is null)
            return;

        // Drop the local L1 entries and collect the L2 keys tracked for this prefix.
        var stale = new List<string>();
        foreach (var tracked in KnownKeys.Keys)
        {
            if (!tracked.StartsWith(tenantPrefix, StringComparison.Ordinal))
                continue;

            _memoryCache.Remove(tracked);
            KnownKeys.TryRemove(tracked, out _);
            stale.Add(tracked);
        }

        if (stale.Count == 0)
            return;

        try
        {
            await Task.WhenAll(stale.Select(key => _cache.RemoveAsync(key, cancellationToken)));
            _logger.LogDebug(CacheEvictionEventId, "Evicted {Count} cache entries under '{CachePrefix}'.", stale.Count, tenantPrefix);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to evict cache entries under '{CachePrefix}'.", tenantPrefix);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan lifetime,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
            return cached;

        var fullKey = BuildKey(key);
        if (fullKey is null)
            return await factory(cancellationToken);

        // One factory invocation per key: concurrent readers wait rather than stampeding the DB.
        var gate = FactoryLocks.GetOrAdd(fullKey, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var doubleChecked = await GetAsync<T>(key, cancellationToken);
            if (doubleChecked is not null)
                return doubleChecked;

            var started = Stopwatch.GetTimestamp();
            var value = await factory(cancellationToken);
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

            await SetAsync(key, value, lifetime, cancellationToken);

            if (elapsedMs > 500)
            {
                _logger.LogInformation(
                    "Cache fill for '{CacheKey}' required {ElapsedMs:F0} ms of source work.",
                    key,
                    elapsedMs);
            }

            return value;
        }
        finally
        {
            gate.Release();
            FactoryLocks.TryRemove(new KeyValuePair<string, SemaphoreSlim>(fullKey, gate));
        }
    }

    private void StoreInMemory<T>(string fullKey, T value, TimeSpan lifetime)
    {
        var ttl = lifetime < _options.MemoryLifetime ? lifetime : _options.MemoryLifetime;
        _memoryCache.Set(fullKey, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            Size = 1
        });
        KnownKeys[fullKey] = 0;
    }

    private string? BuildKey(string key)
    {
        var tenantId = _tenantProvider.TryGetTenantId();
        if (tenantId is null)
        {
            _logger.LogWarning("Skipping cache for '{CacheKey}': no tenant could be resolved.", key);
            return null;
        }

        return $"{KeyPrefix}:{tenantId.Value:N}:{key}";
    }

    private string? BuildPrefix(string prefix)
    {
        var tenantId = _tenantProvider.TryGetTenantId();
        if (tenantId is null)
        {
            _logger.LogWarning("Skipping cache invalidation for '{CachePrefix}': no tenant could be resolved.", prefix);
            return null;
        }

        return $"{KeyPrefix}:{tenantId.Value:N}:{prefix}";
    }

    private void LogHit<T>(string key, string level, T value)
    {
        if (!_options.LogHits)
            return;

        _logger.LogDebug(CacheHitEventId, "Cache hit ({Level}) for '{CacheKey}' ({Size} bytes approx).", level, key, EstimateSize(value));
    }

    private void LogMiss(string key)
    {
        if (_options.LogHits)
            _logger.LogDebug(CacheMissEventId, "Cache miss for '{CacheKey}'.", key);
    }

    private static long EstimateSize<T>(T value) => value switch
    {
        null => 0,
        string s => s.Length,
        System.Collections.ICollection c => c.Count,
        _ => 1
    };
}

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>Upper bound on an L1 entry's lifetime, regardless of the requested TTL.</summary>
    public TimeSpan MemoryLifetime { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Emits a Debug line per hit/miss. Useful when tuning, noisy at scale.</summary>
    public bool LogHits { get; set; }

    /// <summary>Size limit on the shared in-process cache. Size units are not tracked per entry.</summary>
    public long MemoryCacheSizeLimitBytes { get; set; } = 256 * 1024 * 1024;
}
