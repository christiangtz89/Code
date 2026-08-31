using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Auth;
using pcms.Application.Inventory;
using pcms.Domain.Enums;

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

    [HttpGet("cremations/{cremationId:guid}/preview")]
    [Authorize(Policy = PermissionCodes.InventoryScanOutgoing)]
    [Authorize(Policy = PermissionCodes.CremationsView)]
    public async Task<ActionResult<CremationScannerPreviewDto>> CremationPreview(
        Guid cremationId,
        [FromServices] ICremationScannerService cremationScanner)
    {
        try
        {
            return Ok(await cremationScanner.GetPreviewAsync(cremationId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("cremations/{cremationId:guid}/resolve/{scanCode}")]
    [Authorize(Policy = PermissionCodes.InventoryScanOutgoing)]
    [Authorize(Policy = PermissionCodes.CremationsView)]
    public async Task<ActionResult<CremationScannerResolveDto>> ResolveCremationItem(
        Guid cremationId,
        string scanCode,
        [FromQuery] CremationScannerMatchKind expectedKind,
        [FromQuery] CremationStatus expectedCremationStatus,
        [FromQuery] DateTime expectedReservationReservedAt,
        [FromQuery] Guid expectedUrnSupplyItemId,
        [FromQuery] string expectedUrnScanCode,
        [FromServices] ICremationScannerService cremationScanner)
    {
        try
        {
            return Ok(await cremationScanner.ResolveAsync(
                cremationId,
                scanCode,
                expectedKind,
                expectedCremationStatus,
                expectedReservationReservedAt,
                expectedUrnSupplyItemId,
                expectedUrnScanCode));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (CremationScannerConflictException ex)
        {
            return Conflict(new { message = ex.Message, currentPreview = ex.CurrentPreview });
        }
        catch (InventoryScannerConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("cremations/{cremationId:guid}/fulfill")]
    [Authorize(Policy = PermissionCodes.InventoryScanOutgoing)]
    [Authorize(Policy = PermissionCodes.CremationsView)]
    public async Task<ActionResult<CremationScannerPreviewDto>> FulfillCremation(
        Guid cremationId,
        CremationScannerFulfillInput input,
        [FromServices] ICremationScannerService cremationScanner)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId)) return Forbid();

        try
        {
            return Ok(await cremationScanner.FulfillAsync(cremationId, input, userId));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (CremationScannerConflictException ex)
        {
            return Conflict(new { message = ex.Message, currentPreview = ex.CurrentPreview });
        }
        catch (InventoryScannerConflictException ex)
        {
            return Conflict(new { message = ex.Message });
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
