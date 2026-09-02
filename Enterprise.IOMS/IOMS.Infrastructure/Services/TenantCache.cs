using System.Text.Json;
using IOMS.Infrastructure.Data;
using Microsoft.Extensions.Caching.Distributed;

namespace IOMS.Infrastructure.Services;

public interface ITenantCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan lifetime, CancellationToken cancellationToken = default);
}

public sealed class TenantCache : ITenantCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDistributedCache _cache;
    private readonly ITenantProvider _tenantProvider;

    public TenantCache(IDistributedCache cache, ITenantProvider tenantProvider)
    {
        _cache = cache;
        _tenantProvider = tenantProvider;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var payload = await _cache.GetStringAsync(BuildKey(key), cancellationToken);
        return payload is null ? default : JsonSerializer.Deserialize<T>(payload, SerializerOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = lifetime
        };

        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        return _cache.SetStringAsync(BuildKey(key), payload, options, cancellationToken);
    }

    private string BuildKey(string key) => $"ioms:tenant:{_tenantProvider.GetTenantId():N}:{key}";
}
