using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Auth;
using pcms.Application.Inventory;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/inventory/scanner")]
public sealed class InventoryScannerController(IInventoryScannerService service) : ControllerBase
{
    [HttpGet("resolve/{scanCode}")]
    [Authorize(Policy = "Inventory.Scanner.Resolve")]
    public async Task<ActionResult<InventoryScannerResolveDto>> Resolve(string scanCode)
    {
        try
        {
            return Ok(await service.ResolveAsync(scanCode, CanRecordOutgoing()));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InventoryScannerConflictException ex)
        {
            return Conflict(new { message = ex.Message, currentState = ex.CurrentState });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("outgoing")]
    [Authorize(Policy = PermissionCodes.InventoryScanOutgoing)]
    public async Task<ActionResult<InventoryScannerOutgoingDto>> RecordOutgoing(
        InventoryScannerOutgoingInput input)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Forbid();

        try
        {
            return Ok(await service.RecordOutgoingAsync(input, userId));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InventoryScannerConflictException ex)
        {
            return Conflict(new { message = ex.Message, currentState = ex.CurrentState });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private bool CanRecordOutgoing() =>
        User.HasClaim("pcms_owner", "true") ||
        PermissionImplications.Satisfies(
            User.FindAll("permission").Select(x => x.Value),
            PermissionCodes.InventoryScanOutgoing);
}
