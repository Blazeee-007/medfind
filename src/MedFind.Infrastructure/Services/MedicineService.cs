using MedFind.Application.DTOs;
using MedFind.Application.Interfaces;
using MedFind.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MedFind.Infrastructure.Services;

public class MedicineService : IMedicineService
{
    private readonly MedFindDbContext _db;

    public MedicineService(MedFindDbContext db) => _db = db;

    public async Task<IReadOnlyList<MedicineDto>> SearchAsync(string? query, CancellationToken ct = default)
    {
        var q = _db.Medicines.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.ToLower();
            q = q.Where(m =>
                m.Name.ToLower().Contains(lower) ||
                m.GenericName!.ToLower().Contains(lower) ||
                m.Brand!.ToLower().Contains(lower));
        }

        return await q
            .OrderBy(m => m.Name)
            .Take(50)
            .Select(m => new MedicineDto(m.Id, m.Name, m.GenericName, m.Brand, m.Form))
            .ToListAsync(ct);
    }
}
