namespace MedFind.Application.DTOs;

public record NearbyQuery(
    double Lat,
    double Lng,
    double RadiusKm = 5,
    string? Medicine = null,
    int Page = 1,
    int PageSize = 20
);

public record NearbyPharmacyDto(
    Guid Id,
    string Name,
    string? AddressLine,
    string? City,
    string? Phone,
    double DistanceMeters,
    IReadOnlyList<AvailabilityItemDto> Availability
);

public record AvailabilityItemDto(
    Guid MedicineId,
    string Name,
    string? GenericName,
    string? Brand,
    int Quantity,
    DateTimeOffset? VerifiedAt,
    int? VerifiedAgeMinutes
);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public record PharmacyDetailDto(
    Guid Id,
    string Name,
    string? AddressLine,
    string? City,
    string? Phone
);
