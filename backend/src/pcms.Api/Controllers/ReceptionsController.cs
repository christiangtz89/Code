using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Receptions.DTOs;
using pcms.Application.Receptions.Interfaces;
using pcms.Application.Receptions.Exceptions;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReceptionsController : ControllerBase
{
    private readonly IReceptionService _receptionService;

    public ReceptionsController(
        IReceptionService receptionService)
    {
        _receptionService = receptionService;
    }

    [HttpPost]
    public async Task<ActionResult<ReceptionDto>> Create(
        CreateReceptionDto dto)
    {
        var receivedByUserId = GetCurrentUserId();

        if (receivedByUserId == null)
        {
            return Unauthorized(new
            {
                success = false,
                message =
                    "No se pudo identificar al usuario autenticado."
            });
        }

        try
        {
            var reception =
                await _receptionService.CreateAsync(
                    dto,
                    receivedByUserId.Value);

            return CreatedAtAction(
                nameof(GetById),
                new { id = reception.Id },
                reception);
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
    public async Task<ActionResult<PagedReceptionsDto>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var receptions =
                await _receptionService.GetAllAsync(
                    page,
                    pageSize);

            return Ok(receptions);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReceptionDto>> GetById(
        Guid id)
    {
        var reception =
            await _receptionService.GetByIdAsync(id);

        if (reception == null)
        {
            return NotFound(new
            {
                success = false,
                message = "La recepción no fue encontrada."
            });
        }

        return Ok(reception);
    }

    [HttpGet("qr/{qrCode}")]
    public async Task<ActionResult<ReceptionDto>> GetByQrCode(
        string qrCode)
    {
        var reception =
            await _receptionService.GetByQrCodeAsync(qrCode);

        if (reception == null)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "No se encontró una recepción con ese código QR."
            });
        }

        return Ok(reception);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReceptionDto>> Update(
    Guid id,
    UpdateReceptionDto dto)
    {
        try
        {
            var reception =
                await _receptionService.UpdateAsync(
                    id,
                    dto);

            if (reception == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "Recepción no encontrada."
                });
            }

            return Ok(reception);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (WeightRangeChangeConfirmationRequiredException ex)
        {
            return Conflict(new
            {
                success = false,

                code =
                    "WEIGHT_RANGE_CHANGE_CONFIRMATION_REQUIRED",

                message = ex.Message,

                weightChange = new
                {
                    previousWeightKg =
                        ex.PreviousWeightKg,

                    newWeightKg =
                        ex.NewWeightKg,

                    previousMinimumWeightKg =
                        ex.PreviousMinimumWeightKg,

                    previousMaximumWeightKg =
                        ex.PreviousMaximumWeightKg,

                    newMinimumWeightKg =
                        ex.NewMinimumWeightKg,

                    newMaximumWeightKg =
                        ex.NewMaximumWeightKg,

                    previousPrice =
                        ex.PreviousPrice,

                    newPrice =
                        ex.NewPrice
                }
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


    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var success =
            await _receptionService.DeactivateAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "No se encontró una recepción activa."
            });
        }

        return NoContent();
    }

    [HttpPatch("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var success =
            await _receptionService.RestoreAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "No se encontró una recepción inactiva."
            });
        }

        return NoContent();
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ReceptionDto>>>
        Search([FromQuery] string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "Debe proporcionar un término de búsqueda."
            });
        }

        var receptions =
            await _receptionService.SearchAsync(search);

        return Ok(receptions);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        return Guid.TryParse(userIdValue, out var userId)
            ? userId
            : null;
    }
}