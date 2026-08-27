using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        try
        {
            var cremation =
                await _cremationService.ChangeStatusAsync(
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
