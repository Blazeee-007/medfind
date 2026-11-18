using MedFind.Application.DTOs;
using MedFind.Application.Interfaces;
using MedFind.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace MedFind.Infrastructure.Services;

/// <summary>
/// Core nearby/availability logic — no caching here.
/// Caching is applied via the decorator CachedNearbyService.
/// </summary>
public class NearbyService : INearbyService
{
    private readonly MedFindDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<NearbyService> _logger;

    public NearbyService(MedFindDbContext db, IConfiguration config, ILogger<NearbyService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    private int StaleAfterMinutes =>
        int.TryParse(_config["MEDFIND_STALE_AFTER_MINUTES"], out var v) ? v : 60;

    public async Task<PagedResult<NearbyPharmacyDto>> GetNearbyAsync(NearbyQuery q, CancellationToken ct = default)
    {
        var radiusMeters = q.RadiusKm * 1000;
        var searchPoint = new Point(q.Lng, q.Lat) { SRID = 4326 };
        var staleThreshold = DateTimeOffset.UtcNow.AddMinutes(-StaleAfterMinutes);

        // Build query: pharmacies within radius that have available stock
        var baseQuery = _db.Pharmacies
            .AsNoTracking()
            .Where(p => p.IsActive && p.Location.IsWithinDistance(searchPoint, radiusMeters));

        // If medicine filter given, apply it
        if (!string.IsNullOrWhiteSpace(q.Medicine))
        {
            var medFilter = q.Medicine.ToLower();
            baseQuery = baseQuery.Where(p => p.Inventory.Any(i =>
                (i.Medicine.Name.ToLower().Contains(medFilter) ||
                 i.Medicine.GenericName!.ToLower().Contains(medFilter) ||
                 i.Medicine.Brand!.ToLower().Contains(medFilter)) &&
                i.Quantity > 0 &&
                i.VerifiedAt >= staleThreshold));
        }
        else
        {
            baseQuery = baseQuery.Where(p =>
                p.Inventory.Any(i => i.Quantity > 0 && i.VerifiedAt >= staleThreshold));
        }

        var totalCount = await baseQuery.CountAsync(ct);

        var pharmacies = await baseQuery
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.AddressLine,
                p.City,
                p.Phone,
                DistanceMeters = p.Location.Distance(searchPoint),
                Availability = p.Inventory
                    .Where(i => i.Quantity > 0 && i.VerifiedAt >= staleThreshold)
                    .Select(i => new
                    {
                        i.MedicineId,
                        MedicineName = i.Medicine.Name,
                        i.Medicine.GenericName,
                        i.Medicine.Brand,
                        i.Quantity,
                        i.VerifiedAt,
                    })
                    .ToList()
            })
            .OrderBy(p => p.DistanceMeters)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var items = pharmacies.Select(p => new NearbyPharmacyDto(
            p.Id,
            p.Name,
            p.AddressLine,
            p.City,
            p.Phone,
            Math.Round(p.DistanceMeters, 1),
            p.Availability.Select(a => new AvailabilityItemDto(
                a.MedicineId,
                a.MedicineName,
                a.GenericName,
                a.Brand,
                a.Quantity,
                a.VerifiedAt,
                a.VerifiedAt.HasValue ? (int)(now - a.VerifiedAt.Value).TotalMinutes : null
            )).ToList()
        )).ToList();

        return new PagedResult<NearbyPharmacyDto>(items, q.Page, q.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<AvailabilityItemDto>> GetAvailabilityAsync(Guid pharmacyId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var items = await _db.InventoryItems
            .AsNoTracking()
            .Where(i => i.PharmacyId == pharmacyId)
            .Select(i => new
            {
                i.MedicineId,
                MedicineName = i.Medicine.Name,
                i.Medicine.GenericName,
                i.Medicine.Brand,
                i.Quantity,
                i.VerifiedAt,
            })
            .OrderBy(i => i.MedicineName)
            .ToListAsync(ct);

        return items.Select(i => new AvailabilityItemDto(
            i.MedicineId,
            i.MedicineName,
            i.GenericName,
            i.Brand,
            i.Quantity,
            i.VerifiedAt,
            i.VerifiedAt.HasValue ? (int)(now - i.VerifiedAt.Value).TotalMinutes : null
        )).ToList();
    }

    public async Task<PharmacyDetailDto?> GetPharmacyByIdAsync(Guid pharmacyId, CancellationToken ct = default)
    {
        var p = await _db.Pharmacies
            .AsNoTracking()
            .Where(x => x.Id == pharmacyId && x.IsActive)
            .Select(x => new PharmacyDetailDto(x.Id, x.Name, x.AddressLine, x.City, x.Phone))
            .FirstOrDefaultAsync(ct);
        return p;
    }
}
