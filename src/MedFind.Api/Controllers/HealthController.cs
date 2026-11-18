using MedFind.Application.Interfaces;
using MedFind.Infrastructure.Caching;
using Microsoft.AspNetCore.Mvc;

namespace MedFind.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ICacheService _cache;

    public HealthController(ICacheService cache) => _cache = cache;

    /// <summary>GET /health/cache — cache metrics for demo</summary>
    [HttpGet("cache")]
    public async Task<IActionResult> CacheMetrics(CancellationToken ct)
    {
        var hits = await _cache.GetCounterAsync(CacheKeyBuilder.HitsKey, ct);
        var misses = await _cache.GetCounterAsync(CacheKeyBuilder.MissesKey, ct);
        var total = hits + misses;
        var hitRate = total > 0 ? Math.Round((double)hits / total, 4) : 0;

        return Ok(new { hits, misses, hitRate });
    }
}
