using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Pets.DTOs;
using pcms.Application.Pets.Interfaces;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PetsController : ControllerBase
{
    private readonly IPetService _petService;

    public PetsController(IPetService petService)
    {
        _petService = petService;
    }

    [HttpPost]
    public async Task<ActionResult<PetDto>> Create(
        CreatePetDto dto)
    {
        try
        {
            var pet = await _petService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = pet.Id },
                pet);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet]
public async Task<ActionResult<PagedPetsDto>> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] bool isActive = true)
    {
    if (page < 1)
    {
        return BadRequest(new
        {
            message = "Page must be greater than zero."
        });
    }

    if (pageSize < 1 || pageSize > 100)
    {
        return BadRequest(new
        {
            message = "Page size must be between 1 and 100."
        });
    }

    var pets = await _petService.GetAllAsync(
        page,
        pageSize,
        isActive);

    return Ok(pets);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PetDto>> GetById(Guid id)
    {
        var pet = await _petService.GetByIdAsync(id);

        if (pet == null)
        {
            return NotFound(new
            {
                message = "Pet not found."
            });
        }

        return Ok(pet);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PetDto>> Update(
        Guid id,
        UpdatePetDto dto)
    {
        var pet = await _petService.UpdateAsync(id, dto);

        if (pet == null)
        {
            return NotFound(new
            {
                message = "Pet not found."
            });
        }

        return Ok(pet);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var success = await _petService.DeactivateAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                message = "Active pet not found."
            });
        }

        return NoContent();
    }

    [HttpPatch("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var success = await _petService.RestoreAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                message = "Inactive pet not found."
            });
        }

        return NoContent();
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<
        ActionResult<IEnumerable<PetDto>>>
        GetByCustomer(
            Guid customerId,
            [FromQuery] bool? isActive = null)
{
        var pets =
            await _petService.GetByCustomerIdAsync(
                customerId,
                isActive);

    return Ok(pets);
}

    [HttpGet("search")]
public async Task<ActionResult<IEnumerable<PetDto>>> Search(
    [FromQuery] string search,
    [FromQuery] bool isActive = true)
{
    if (string.IsNullOrWhiteSpace(search))
    {
        return BadRequest(new
        {
            message = "Search term is required."
        });
    }

    var pets = await _petService.SearchAsync(
        search,
        isActive);

    return Ok(pets);
}
}