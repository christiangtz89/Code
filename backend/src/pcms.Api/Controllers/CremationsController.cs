using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using pcms.Application.Cremations.DTOs;
using pcms.Application.Cremations.Interfaces;
using pcms.Domain.Enums;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Cremations.View")]
public class CremationsController : ControllerBase
{
    private readonly ICremationService _cremationService;

    public CremationsController(
        ICremationService cremationService)
    {
        _cremationService = cremationService;
    }

    [HttpPost]
    [Authorize(Policy = "Cremations.Manage")]
    public async Task<ActionResult<CremationDto>> Create(
        CreateCremationDto dto)
    {
        try
        {
            var cremation =
                await _cremationService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = cremation.Id },
                cremation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<PagedCremationsDto>> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] bool isActive = true)
    {
        if (page < 1)
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "La página debe ser mayor que cero."
            });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "El tamaño de página debe estar entre 1 y 100."
            });
        }

        var cremations =
    await _cremationService.GetAllAsync(
        page,
        pageSize,
        isActive);

        return Ok(cremations);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CremationDto>> GetById(
        Guid id)
    {
        var cremation =
            await _cremationService.GetByIdAsync(id);

        if (cremation == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Cremación no encontrada."
            });
        }

        return Ok(cremation);
    }

    [HttpGet("{id:guid}/operational-comments")]
    public async Task<ActionResult<IReadOnlyList<CremationOperationalCommentDto>>> GetOperationalComments(
        Guid id)
    {
        var comments = await _cremationService.GetOperationalCommentsAsync(id);

        if (comments is null)
        {
            return NotFound(new
            {
                success = false,
                message = "Cremación no encontrada."
            });
        }

        return Ok(comments);
    }

    [HttpGet("reception/{receptionId:guid}")]
    public async Task<ActionResult<CremationDto>>
        GetByReceptionId(Guid receptionId)
    {
        var cremation =
            await _cremationService.GetByReceptionIdAsync(
                receptionId);

        if (cremation == null)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "No se encontró una cremación para esta recepción."
            });
        }

        return Ok(cremation);
    }

    [HttpGet("status/{status}")]
    public async Task<
        ActionResult<IEnumerable<CremationDto>>> GetByStatus(
        CremationStatus status)
    {
        try
        {
            var cremations =
                await _cremationService.GetByStatusAsync(
                    status);

            return Ok(cremations);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Cremations.Manage")]
    public async Task<ActionResult<CremationDto>> Update(
        Guid id,
        UpdateCremationDto dto)
    {
        try
        {
            var cremation =
                await _cremationService.UpdateAsync(
                    id,
                    dto);

            if (cremation == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Cremación no encontrada."
                });
            }

            return Ok(cremation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "Cremations.Manage")]
    public async Task<ActionResult<CremationDto>> ChangeStatus(
        Guid id,
        ChangeCremationStatusDto dto)
    {
        var actorValue =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        if (!Guid.TryParse(actorValue, out var actorUserId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "No se pudo identificar al usuario autenticado."
            });
        }

        try
        {
            var cremation =
                await _cremationService.ChangeStatusAsync(
                    id,
                    dto,
                    actorUserId);

            if (cremation == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Cremación no encontrada."
                });
            }

            return Ok(cremation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpPost("{id:guid}/revalidate-start-payment")]
    [Authorize(Policy = "Cremations.Manage")]
    public async Task<ActionResult<RevalidateStartPaymentResultDto>>
        RevalidateStartPayment(
            Guid id,
            RevalidateStartPaymentRequestDto request)
    {
        var actorValue =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        if (!Guid.TryParse(actorValue, out var actorUserId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "No se pudo identificar al usuario autenticado."
            });
        }

        try
        {
            var result = await _cremationService
                .RevalidateStartPaymentAsync(id, request, actorUserId);

            return result is null
                ? NotFound(new
                {
                    success = false,
                    message = "Cremación no encontrada."
                })
                : Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                success = false,
                message = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                success = false,
                message = exception.Message
            });
        }
    }

    [HttpPost("{id:guid}/reassignments")]
    [Authorize(Policy = "Cremations.Manage")]
    public async Task<ActionResult<CremationDto>> Reassign(Guid id, ReassignCremationDto dto)
    {
        var actorValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
        if (!Guid.TryParse(actorValue, out var actorUserId)) return Unauthorized();
        try
        {
            var result = await _cremationService.ReassignAsync(id, dto, actorUserId);
            return result is null ? NotFound(new { message = "Cremación no encontrada." }) : Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
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

    [HttpGet("{id:guid}/reassignments")]
    public async Task<ActionResult<IReadOnlyList<CremationReassignmentDto>>> GetReassignments(Guid id)
    {
        var result = await _cremationService.GetReassignmentsAsync(id);
        return result is null ? NotFound(new { message = "Cremación no encontrada." }) : Ok(result);
    }

    [HttpPost("{id:guid}/instructions-amendments")]
    [Authorize(Policy = "Cremations.Manage")]
    public async Task<ActionResult<CremationDto>> AmendInstructions(
        Guid id, AmendCremationInstructionsDto dto)
    {
        var actorValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
        if (!Guid.TryParse(actorValue, out var actorUserId)) return Unauthorized();
        try
        {
            var result = await _cremationService.AmendInstructionsAsync(id, dto, actorUserId);
            return result is null ? NotFound(new { message = "Cremación no encontrada." }) : Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
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

    [HttpGet("{id:guid}/instructions-amendments")]
    public async Task<ActionResult<IReadOnlyList<CremationInstructionsAmendmentDto>>> GetInstructionsAmendments(Guid id)
    {
        var result = await _cremationService.GetInstructionsAmendmentsAsync(id);
        return result is null ? NotFound(new { message = "Cremación no encontrada." }) : Ok(result);
    }

    [HttpPost("{id:guid}/accessory-amendments")]
    [Authorize(Policy = "Cremations.Manage")]
    public async Task<ActionResult<CremationDto>> AmendAccessory(
        Guid id, AmendCremationAccessoryDto dto)
    {
        var actorValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
        if (!Guid.TryParse(actorValue, out var actorUserId)) return Unauthorized();
        try
        {
            var result = await _cremationService.AmendAccessoryAsync(id, dto, actorUserId);
            return result is null ? NotFound(new { message = "Cremación no encontrada." }) : Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
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

    [HttpGet("{id:guid}/accessory-amendments")]
    public async Task<ActionResult<IReadOnlyList<CremationAccessoryAmendmentDto>>> GetAccessoryAmendments(Guid id)
    {
        var result = await _cremationService.GetAccessoryAmendmentsAsync(id);
        return result is null ? NotFound(new { message = "Cremación no encontrada." }) : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Cremations.Manage")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var success =
            await _cremationService.DeactivateAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "No se encontró una cremación activa."
            });
        }

        return NoContent();
    }

    [HttpPatch("{id:guid}/restore")]
    [Authorize(Policy = "Cremations.Manage")]
    public async Task<IActionResult> Restore(Guid id)
    {
        try
        {
            var success =
                await _cremationService.RestoreAsync(id);

            if (!success)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "No se encontró una cremación inactiva."
                });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpGet("search")]
    public async Task<
    ActionResult<IEnumerable<CremationDto>>> Search(
    [FromQuery] string search,
    [FromQuery] bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "El término de búsqueda es obligatorio."
            });
        }

        var cremations =
    await _cremationService.SearchAsync(
        search,
        isActive);

        return Ok(cremations);
    }

    [HttpGet("options/receptions")]
    public async Task<
    ActionResult<
        IEnumerable<CremationReceptionOptionDto>>>
    GetAvailableReceptionOptions()
    {
        var receptions =
            await _cremationService
                .GetAvailableReceptionOptionsAsync();

        return Ok(receptions);
    }

    [HttpGet("options/users")]
    public async Task<
        ActionResult<
            IEnumerable<CremationUserOptionDto>>>
        GetActiveUserOptions()
    {
        var users =
            await _cremationService
                .GetActiveUserOptionsAsync();

        return Ok(users);
    }
}
