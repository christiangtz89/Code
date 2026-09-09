using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Common;
using pcms.Application.Veterinarians.DTOs;
using pcms.Application.Veterinarians.Interfaces;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Veterinarians.View")]
public class VeterinariansController : ControllerBase
{
    private readonly IVeterinarianService _veterinarianService;

    public VeterinariansController(
        IVeterinarianService veterinarianService)
    {
        _veterinarianService = veterinarianService;
    }

    [HttpPost]
    [Authorize(Policy = "Veterinarians.Manage")]
    public async Task<ActionResult<VeterinarianDto>> Create(
        CreateVeterinarianDto dto)
    {
        try
        {
            var veterinarian =
                await _veterinarianService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = veterinarian.Id },
                veterinarian);
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
    public async Task<ActionResult<PagedVeterinariansDto>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool isActive = true,
        [FromQuery] Guid? veterinaryClinicId = null)
    {
        if (page < 1)
        {
            return BadRequest(new
            {
                success = false,
                message = "Page must be greater than zero."
            });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "Page size must be between 1 and 100."
            });
        }

        var veterinarians =
    await _veterinarianService.GetAllAsync(
        page,
        pageSize,
        isActive,
        veterinaryClinicId);

        return Ok(veterinarians);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VeterinarianDto>> GetById(
        Guid id)
    {
        var veterinarian =
            await _veterinarianService.GetByIdAsync(id);

        if (veterinarian == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Veterinarian not found."
            });
        }

        return Ok(veterinarian);
    }

    [HttpGet("clinic/{veterinaryClinicId:guid}")]
    public async Task<
        ActionResult<IEnumerable<VeterinarianDto>>> GetByClinicId(
        Guid veterinaryClinicId,
        [FromQuery] bool? isActive = null)
    {
        var veterinarians =
            await _veterinarianService.GetByClinicIdAsync(
                veterinaryClinicId,
                isActive);

        return Ok(veterinarians);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Veterinarians.Manage")]
    public async Task<ActionResult<VeterinarianDto>> Update(
        Guid id,
        UpdateVeterinarianDto dto)
    {
        try
        {
            var veterinarian =
                await _veterinarianService.UpdateAsync(id, dto);

            if (veterinarian == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Veterinarian not found."
                });
            }

            return Ok(veterinarian);
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
    [Authorize(Policy = "Veterinarians.Manage")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var success =
            await _veterinarianService.DeactivateAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                success = false,
                message = "Active veterinarian not found."
            });
        }

        return NoContent();
    }

    [HttpPatch("{id:guid}/restore")]
    [Authorize(Policy = "Veterinarians.Manage")]
    public async Task<IActionResult> Restore(Guid id)
    {
        try
        {
            var success =
                await _veterinarianService.RestoreAsync(id);

            if (!success)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Inactive veterinarian not found."
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
        ActionResult<PagedVeterinariansDto>> Search(
        [FromQuery] string search,
        [FromQuery] bool isActive = true,
        [FromQuery] Guid? veterinaryClinicId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return BadRequest(new
            {
                success = false,
                message = "Search term is required."
            });
        }

        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid pagination parameters."
            });
        }

        var veterinarians =
            await _veterinarianService.SearchAsync(
                search,
                isActive,
                veterinaryClinicId,
                page,
                pageSize);

        return Ok(veterinarians);
    }

    [HttpGet("clinic-options")]
    public async Task<
        ActionResult<PaginatedResult<VeterinarianClinicOptionDto>>>
        GetClinicOptions(
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
    {
        var options =
            await _veterinarianService.GetClinicOptionsAsync(
                search,
                isActive,
                page,
                pageSize);

        return Ok(options);
    }
}
