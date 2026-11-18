using FluentValidation;
using MedFind.Application.DTOs;
using MedFind.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedFind.Api.Controllers;

[ApiController]
[Route("api/pharmacy")]
[Authorize(Roles = "PharmacyAdmin")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;
    private readonly IValidator<UpdateStockRequest> _updateValidator;
    private readonly IValidator<AddInventoryRequest> _addValidator;

    public InventoryController(
        IInventoryService inventory,
        IValidator<UpdateStockRequest> updateValidator,
        IValidator<AddInventoryRequest> addValidator)
    {
        _inventory = inventory;
        _updateValidator = updateValidator;
        _addValidator = addValidator;
    }

    private Guid PharmacyId =>
        Guid.Parse(User.FindFirstValue("pharmacyId")
            ?? throw new InvalidOperationException("pharmacyId claim missing"));

    /// <summary>GET /api/pharmacy/inventory — admin's own inventory</summary>
    [HttpGet("inventory")]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventory(CancellationToken ct)
    {
        var result = await _inventory.GetInventoryAsync(PharmacyId, ct);
        return Ok(result);
    }

    /// <summary>PUT /api/pharmacy/inventory/{medicineId} — update stock + invalidate cache</summary>
    [HttpPut("inventory/{medicineId:guid}")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStock(Guid medicineId, [FromBody] UpdateStockRequest request, CancellationToken ct)
    {
        var v = await _updateValidator.ValidateAsync(request, ct);
        if (!v.IsValid)
        {
            foreach (var e in v.Errors) ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
            return ValidationProblem();
        }

        var result = await _inventory.UpdateStockAsync(PharmacyId, medicineId, request.Quantity, ct);
        return Ok(result);
    }

    /// <summary>POST /api/pharmacy/inventory — add medicine to pharmacy inventory</summary>
    [HttpPost("inventory")]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddInventoryItem([FromBody] AddInventoryRequest request, CancellationToken ct)
    {
        var v = await _addValidator.ValidateAsync(request, ct);
        if (!v.IsValid)
        {
            foreach (var e in v.Errors) ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
            return ValidationProblem();
        }

        var result = await _inventory.AddInventoryItemAsync(PharmacyId, request.MedicineId, request.Quantity, ct);
        return CreatedAtAction(nameof(GetInventory), result);
    }
}
