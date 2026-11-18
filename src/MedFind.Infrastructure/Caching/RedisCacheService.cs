using MedFind.Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace MedFind.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<T>(value!, _json);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
    {
        var serialized = JsonSerializer.Serialize(value, _json);
        await _db.StringSetAsync(key, serialized, ttl);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
        => _db.KeyDeleteAsync(key);

    public Task IncrementAsync(string key, CancellationToken ct = default)
        => _db.StringIncrementAsync(key);

    public async Task<long> GetCounterAsync(string key, CancellationToken ct = default)
    {
        var v = await _db.StringGetAsync(key);
        return v.IsNullOrEmpty ? 0 : (long)v;
    }
}
