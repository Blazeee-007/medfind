namespace MedFind.Application.DTOs;

public record MedicineDto(Guid Id, string Name, string? GenericName, string? Brand, string? Form);

public record InventoryItemDto(
    Guid MedicineId,
    string Name,
    string? GenericName,
    string? Brand,
    int Quantity,
    DateTimeOffset? VerifiedAt,
    int? VerifiedAgeMinutes,
    bool NeedsUpdate
);

public record UpdateStockRequest(int Quantity);
public record AddInventoryRequest(Guid MedicineId, int Quantity);
