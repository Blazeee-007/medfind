using MedFind.Application.DTOs;

namespace MedFind.Application.Interfaces;

public interface IMedicineService
{
    Task<IReadOnlyList<MedicineDto>> SearchAsync(string? query, CancellationToken ct = default);
}
