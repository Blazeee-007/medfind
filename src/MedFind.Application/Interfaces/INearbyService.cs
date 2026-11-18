using MedFind.Application.DTOs;

namespace MedFind.Application.Interfaces;

public interface INearbyService
{
    Task<PagedResult<NearbyPharmacyDto>> GetNearbyAsync(NearbyQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<AvailabilityItemDto>> GetAvailabilityAsync(Guid pharmacyId, CancellationToken ct = default);
    Task<PharmacyDetailDto?> GetPharmacyByIdAsync(Guid pharmacyId, CancellationToken ct = default);
}
