namespace MedFind.Infrastructure.Caching;

/// <summary>
/// Builds stable, rounded cache keys for nearby and availability queries.
/// Coordinates are rounded to 2 decimal places (~1 km bucket) to raise hit rate.
/// </summary>
public static class CacheKeyBuilder
{
    // round to 2dp ≈ 1.1 km at equator — good bucket size for pharmacy search
    public static string Nearby(double lat, double lng, double radiusKm, string? medicine, int page)
    {
        var latR = Math.Round(lat, 2);
        var lngR = Math.Round(lng, 2);
        var med = string.IsNullOrWhiteSpace(medicine) ? "all" : medicine.Trim().ToLowerInvariant();
        return $"nearby:{latR}:{lngR}:{radiusKm}:{med}:p{page}";
    }

    public static string Availability(Guid pharmacyId) => $"availability:{pharmacyId}";
    public static string PharmacyVersion(Guid pharmacyId) => $"pharmacyVersion:{pharmacyId}";

    // Metric counters
    public const string HitsKey = "metrics:cache:hits";
    public const string MissesKey = "metrics:cache:misses";
}
