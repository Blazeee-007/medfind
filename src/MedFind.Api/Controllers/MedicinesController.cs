using MedFind.Application.DTOs;
using MedFind.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MedFind.Api.Controllers;

[ApiController]
[Route("api/medicines")]
public class MedicinesController : ControllerBase
{
    private readonly IMedicineService _medicines;

    public MedicinesController(IMedicineService medicines) => _medicines = medicines;

    /// <summary>GET /api/medicines?q=paracet — autocomplete</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MedicineDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken ct)
    {
        var result = await _medicines.SearchAsync(q, ct);
        return Ok(result);
    }
}
