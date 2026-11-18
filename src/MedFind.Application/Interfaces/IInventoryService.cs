using MedFind.Application.DTOs;

namespace MedFind.Application.Interfaces;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryItemDto>> GetInventoryAsync(Guid pharmacyId, CancellationToken ct = default);
    Task<InventoryItemDto> UpdateStockAsync(Guid pharmacyId, Guid medicineId, int quantity, CancellationToken ct = default);
    Task<InventoryItemDto> AddInventoryItemAsync(Guid pharmacyId, Guid medicineId, int quantity, CancellationToken ct = default);
}
