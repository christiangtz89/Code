using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.VeterinaryClinics.DTOs;
using pcms.Application.VeterinaryClinics.Interfaces;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "VeterinaryClinics.View")]
public class VeterinaryClinicsController : ControllerBase
{
    private readonly IVeterinaryClinicService _clinicService;

    public VeterinaryClinicsController(
        IVeterinaryClinicService clinicService)
    {
        _clinicService = clinicService;
    }

    [HttpPost]
    [Authorize(Policy = "VeterinaryClinics.Manage")]
    public async Task<ActionResult<VeterinaryClinicDto>> Create(
        CreateVeterinaryClinicDto dto)
    {
        try
        {
            var clinic = await _clinicService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = clinic.Id },
                clinic);
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
    public async Task<ActionResult<PagedVeterinaryClinicsDto>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool isActive = true)
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
                message = "Page size must be between 1 and 100."
            });
        }

        var clinics = await _clinicService.GetAllAsync(
            page,
            pageSize,
            isActive);

        return Ok(clinics);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VeterinaryClinicDto>> GetById(
        Guid id)
    {
        var clinic = await _clinicService.GetByIdAsync(id);

        if (clinic == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Veterinary clinic not found."
            });
        }

        return Ok(clinic);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "VeterinaryClinics.Manage")]
    public async Task<ActionResult<VeterinaryClinicDto>> Update(
        Guid id,
        UpdateVeterinaryClinicDto dto)
    {
        try
        {
            var clinic = await _clinicService.UpdateAsync(id, dto);

            if (clinic == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Veterinary clinic not found."
                });
            }

            return Ok(clinic);
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
    [Authorize(Policy = "VeterinaryClinics.Manage")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var success = await _clinicService.DeactivateAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                success = false,
                message = "Active veterinary clinic not found."
            });
        }

        return NoContent();
    }

    [HttpPatch("{id:guid}/restore")]
    [Authorize(Policy = "VeterinaryClinics.Manage")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var success = await _clinicService.RestoreAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                success = false,
                message = "Inactive veterinary clinic not found."
            });
        }

        return NoContent();
    }

    [HttpGet("search")]
    public async Task<
        ActionResult<IEnumerable<VeterinaryClinicDto>>> Search(
        [FromQuery] string search,
        [FromQuery] bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return BadRequest(new
            {
                success = false,
                message = "Search term is required."
            });
        }

        var clinics = await _clinicService.SearchAsync(
            search,
            isActive);

        return Ok(clinics);
    }
}
