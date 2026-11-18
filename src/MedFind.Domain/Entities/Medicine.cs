namespace MedFind.Domain.Entities;

public class Medicine
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;        // "Paracetamol 500mg"
    public string? GenericName { get; set; }             // "Paracetamol"
    public string? Brand { get; set; }                   // "Crocin"
    public string? Form { get; set; }                    // "tablet" | "syrup" | …
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Nav
    public ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
