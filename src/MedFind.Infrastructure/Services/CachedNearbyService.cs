using MedFind.Application.DTOs;
using MedFind.Application.Interfaces;
using MedFind.Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace MedFind.Infrastructure.Services;

/// <summary>
/// Caching decorator for INearbyService.
/// Pattern: try cache → on miss: call inner → store in cache.
/// Cache-key builder is in CacheKeyBuilder (testable in isolation).
/// 
/// Why decorator, not in the service itself?
///   → Service stays testable without Redis; cache logic is a cross-cutting concern.
///   → Can be enabled/disabled via DI configuration.
/// </summary>
public class CachedNearbyService : INearbyService
{
    private readonly INearbyService _inner;
    private readonly ICacheService _cache;
    private readonly ILogger<CachedNearbyService> _logger;

    private static readonly TimeSpan NearbyTtl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan AvailabilityTtl = TimeSpan.FromSeconds(30);

    public CachedNearbyService(INearbyService inner, ICacheService cache, ILogger<CachedNearbyService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<NearbyPharmacyDto>> GetNearbyAsync(NearbyQuery q, CancellationToken ct = default)
    {
        var key = CacheKeyBuilder.Nearby(q.Lat, q.Lng, q.RadiusKm, q.Medicine, q.Page);
        var cached = await _cache.GetAsync<PagedResult<NearbyPharmacyDto>>(key, ct);

        if (cached is not null)
        {
            _logger.LogInformation("Cache HIT  key={Key}", key);
            await _cache.IncrementAsync(CacheKeyBuilder.HitsKey, ct);
            return cached;
        }

        _logger.LogInformation("Cache MISS key={Key}", key);
        await _cache.IncrementAsync(CacheKeyBuilder.MissesKey, ct);

        var result = await _inner.GetNearbyAsync(q, ct);
        await _cache.SetAsync(key, result, NearbyTtl, ct);
        return result;
    }

    public async Task<IReadOnlyList<AvailabilityItemDto>> GetAvailabilityAsync(Guid pharmacyId, CancellationToken ct = default)
    {
        var key = CacheKeyBuilder.Availability(pharmacyId);
        var cached = await _cache.GetAsync<List<AvailabilityItemDto>>(key, ct);

        if (cached is not null)
        {
            _logger.LogInformation("Cache HIT  key={Key}", key);
            await _cache.IncrementAsync(CacheKeyBuilder.HitsKey, ct);
            return cached;
        }

        _logger.LogInformation("Cache MISS key={Key}", key);
        await _cache.IncrementAsync(CacheKeyBuilder.MissesKey, ct);

        var result = await _inner.GetAvailabilityAsync(pharmacyId, ct);
        await _cache.SetAsync(key, result.ToList(), AvailabilityTtl, ct);
        return result;
    }

    // Pharmacy detail is NOT cached (cheap, low-traffic, see CACHING_STRATEGY.md)
    public Task<PharmacyDetailDto?> GetPharmacyByIdAsync(Guid pharmacyId, CancellationToken ct = default)
        => _inner.GetPharmacyByIdAsync(pharmacyId, ct);
}
