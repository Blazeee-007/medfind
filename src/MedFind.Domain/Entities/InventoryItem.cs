namespace MedFind.Domain.Entities;

public class InventoryItem
{
    public Guid Id { get; set; }
    public Guid PharmacyId { get; set; }
    public Guid MedicineId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Nav
    public Pharmacy Pharmacy { get; set; } = default!;
    public Medicine Medicine { get; set; } = default!;
}
