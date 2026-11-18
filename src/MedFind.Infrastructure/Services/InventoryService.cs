using MedFind.Application.DTOs;
using MedFind.Application.Interfaces;
using MedFind.Infrastructure.Caching;
using MedFind.Infrastructure.Data;
using MedFind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MedFind.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly MedFindDbContext _db;
    private readonly ICacheService _cache;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(MedFindDbContext db, ICacheService cache, ILogger<InventoryService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetInventoryAsync(Guid pharmacyId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var staleThreshold = now.AddMinutes(-60); // default

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

        return items.Select(i =>
        {
            var ageMin = i.VerifiedAt.HasValue ? (int)(now - i.VerifiedAt.Value).TotalMinutes : (int?)null;
            return new InventoryItemDto(
                i.MedicineId, i.MedicineName, i.GenericName, i.Brand,
                i.Quantity, i.VerifiedAt, ageMin,
                NeedsUpdate: !i.VerifiedAt.HasValue || i.VerifiedAt < staleThreshold
            );
        }).ToList();
    }

    public async Task<InventoryItemDto> UpdateStockAsync(Guid pharmacyId, Guid medicineId, int quantity, CancellationToken ct = default)
    {
        var item = await _db.InventoryItems
            .Include(i => i.Medicine)
            .FirstOrDefaultAsync(i => i.PharmacyId == pharmacyId && i.MedicineId == medicineId, ct)
            ?? throw new NotFoundException($"Medicine {medicineId} not found in this pharmacy's inventory.");

        item.Quantity = quantity;
        item.VerifiedAt = DateTimeOffset.UtcNow;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Invalidate availability cache immediately (write-through delete)
        await _cache.DeleteAsync(CacheKeyBuilder.Availability(pharmacyId), ct);
        await _cache.IncrementAsync(CacheKeyBuilder.PharmacyVersion(pharmacyId), ct);
        _logger.LogInformation("Stock updated: pharmacy={PharmacyId} medicine={MedicineId} qty={Qty} → cache invalidated",
            pharmacyId, medicineId, quantity);

        var now = DateTimeOffset.UtcNow;
        return new InventoryItemDto(
            item.MedicineId, item.Medicine.Name, item.Medicine.GenericName, item.Medicine.Brand,
            item.Quantity, item.VerifiedAt, 0, NeedsUpdate: false);
    }

    public async Task<InventoryItemDto> AddInventoryItemAsync(Guid pharmacyId, Guid medicineId, int quantity, CancellationToken ct = default)
    {
        if (await _db.InventoryItems.AnyAsync(i => i.PharmacyId == pharmacyId && i.MedicineId == medicineId, ct))
            throw new ConflictException("This medicine is already in the pharmacy's inventory.");

        var medicine = await _db.Medicines.AsNoTracking().FirstOrDefaultAsync(m => m.Id == medicineId, ct)
            ?? throw new NotFoundException($"Medicine {medicineId} not found.");

        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            PharmacyId = pharmacyId,
            MedicineId = medicineId,
            Quantity = quantity,
            VerifiedAt = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync(ct);

        return new InventoryItemDto(medicine.Id, medicine.Name, medicine.GenericName, medicine.Brand,
            quantity, null, null, NeedsUpdate: true);
    }
}
