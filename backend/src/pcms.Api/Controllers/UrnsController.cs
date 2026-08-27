using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Urns.DTOs;
using pcms.Application.Urns.Interfaces;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Urns.View")]
public class UrnsController : ControllerBase
{
    private readonly IUrnService _urnService;

    public UrnsController(IUrnService urnService)
    {
        _urnService = urnService;
    }

    // Internal PCMS catalog.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UrnDto>>> GetAll(
        [FromQuery] bool includeInactive = false)
    {
        var urns = await _urnService.GetAllAsync(
            includeInactive,
            publicOnly: false);

        return Ok(urns);
    }

    // Public catalog for the future landing page.
    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<ActionResult<IEnumerable<UrnDto>>> GetPublic()
    {
        var urns = await _urnService.GetAllAsync(
            includeInactive: false,
            publicOnly: true);

        return Ok(urns);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UrnDto>> GetById(Guid id)
    {
        var urn = await _urnService.GetByIdAsync(id);

        if (urn is null)
        {
            return NotFound(new
            {
                message = "La urna no fue encontrada."
            });
        }

        return Ok(urn);
    }

    [HttpPost]
    [Authorize(Policy = "Urns.Manage")]
    public async Task<ActionResult<UrnDto>> Create(
        [FromBody] CreateUrnDto dto)
    {
        try
        {
            var urn = await _urnService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = urn.Id },
                urn);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Urns.Manage")]
    public async Task<ActionResult<UrnDto>> Update(
        Guid id,
        [FromBody] UpdateUrnDto dto)
    {
        try
        {
            var urn = await _urnService.UpdateAsync(id, dto);

            if (urn is null)
            {
                return NotFound(new
                {
                    message = "La urna no fue encontrada."
                });
            }

            return Ok(urn);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
