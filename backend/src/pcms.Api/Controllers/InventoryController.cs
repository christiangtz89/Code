using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Inventory;

namespace pcms.Api.Controllers;

[ApiController, Route("api/inventory")]
public class InventoryController(IInventoryService service, IFilamentService filament) : ControllerBase
{
    [HttpGet("current"), Authorize(Policy = "Inventory.View")]
    public Task<IEnumerable<InventoryItemDto>> Current([FromQuery] string? search) => service.GetCurrentAsync(search);

    [HttpGet("items/{supplyItemId:guid}/movements"), Authorize(Policy = "Inventory.View")]
    public Task<IEnumerable<InventoryMovementDto>> Movements(Guid supplyItemId) => service.GetMovementsAsync(supplyItemId);

    [HttpPost("movements"), Authorize(Policy = "Inventory.Manage")]
    public async Task<ActionResult<InventoryMovementDto>> Record(InventoryMovementInput input)
    {
        try
        {
            var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (Guid?)null;
            return Ok(await service.RecordAsync(input, userId));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("filament/{supplyItemId:guid}"), Authorize(Policy = "Inventory.View")]
    public async Task<ActionResult<FilamentSpecificationDto>> Filament(Guid supplyItemId) => (await filament.GetAsync(supplyItemId, false)) is { } x ? Ok(x) : NotFound();
    [HttpGet("filament/{supplyItemId:guid}/cost"), Authorize(Policy = "Purchasing.View")]
    public async Task<ActionResult<FilamentSpecificationDto>> FilamentCost(Guid supplyItemId) => (await filament.GetAsync(supplyItemId, true)) is { } x ? Ok(x) : NotFound();
    [HttpPut("filament"), Authorize(Policy = "Inventory.Manage")]
    public async Task<ActionResult<FilamentSpecificationDto>> SaveFilament(FilamentSpecificationInput input) { try { return Ok(await filament.SaveAsync(input)); } catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); } }
}
