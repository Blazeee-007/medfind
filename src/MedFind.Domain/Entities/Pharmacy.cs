using NetTopologySuite.Geometries;

namespace MedFind.Domain.Entities;

public class Pharmacy
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = default!;
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    // PostGIS geography(Point,4326) — stored as (longitude, latitude)
    public Point Location { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Nav
    public User Owner { get; set; } = default!;
    public ICollection<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
}
