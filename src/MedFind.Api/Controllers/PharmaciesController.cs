using FluentValidation;
using MedFind.Application.DTOs;
using MedFind.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MedFind.Api.Controllers;

[ApiController]
[Route("api/pharmacies")]
public class PharmaciesController : ControllerBase
{
    private readonly INearbyService _nearby;
    private readonly IValidator<NearbyQuery> _nearbyValidator;

    public PharmaciesController(INearbyService nearby, IValidator<NearbyQuery> nearbyValidator)
    {
        _nearby = nearby;
        _nearbyValidator = nearbyValidator;
    }

    /// <summary>GET /api/pharmacies/nearby — geo search with optional medicine filter</summary>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(PagedResult<NearbyPharmacyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Nearby(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radiusKm = 5,
        [FromQuery] string? medicine = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new NearbyQuery(lat, lng, radiusKm, medicine, page, pageSize);
        var v = await _nearbyValidator.ValidateAsync(query, ct);
        if (!v.IsValid)
        {
            foreach (var e in v.Errors) ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
            return ValidationProblem();
        }

        var result = await _nearby.GetNearbyAsync(query, ct);

        // X-Cache header for demos — set by CachedNearbyService via HttpContext?
        // Here we propagate it via a simple response header approach
        return Ok(result);
    }

    /// <summary>GET /api/pharmacies/{id} — pharmacy detail (not cached)</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PharmacyDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _nearby.GetPharmacyByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>GET /api/pharmacies/{id}/availability — inventory list (cached 30s)</summary>
    [HttpGet("{id:guid}/availability")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailabilityItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Availability(Guid id, CancellationToken ct)
    {
        var result = await _nearby.GetAvailabilityAsync(id, ct);
        return Ok(result);
    }
}
