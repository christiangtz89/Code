using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.CremationPackages.DTOs;
using pcms.Application.CremationPackages.Interfaces;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CremationPackagesController : ControllerBase
{
    private readonly ICremationPackageService _cremationPackageService;

    public CremationPackagesController(
        ICremationPackageService cremationPackageService)
    {
        _cremationPackageService = cremationPackageService;
    }

    // Internal PCMS catalog.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CremationPackageDto>>> GetAll(
        [FromQuery] bool includeInactive = false)
    {
        var packages = await _cremationPackageService.GetAllAsync(
            includeInactive,
            publicOnly: false);

        return Ok(packages);
    }

    // Public catalog for the future landing page.
    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<ActionResult<IEnumerable<CremationPackageDto>>> GetPublic()
    {
        var packages = await _cremationPackageService.GetAllAsync(
            includeInactive: false,
            publicOnly: true);

        return Ok(packages);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CremationPackageDto>> GetById(
        Guid id)
    {
        var package =
            await _cremationPackageService.GetByIdAsync(id);

        if (package is null)
        {
            return NotFound(new
            {
                message = "El paquete de cremación no fue encontrado."
            });
        }

        return Ok(package);
    }

    [HttpPost]
    public async Task<ActionResult<CremationPackageDto>> Create(
        [FromBody] CreateCremationPackageDto dto)
    {
        try
        {
            var package =
                await _cremationPackageService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = package.Id },
                package);
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
    public async Task<ActionResult<CremationPackageDto>> Update(
        Guid id,
        [FromBody] UpdateCremationPackageDto dto)
    {
        try
        {
            var package =
                await _cremationPackageService.UpdateAsync(id, dto);

            if (package is null)
            {
                return NotFound(new
                {
                    message =
                        "El paquete de cremación no fue encontrado."
                });
            }

            return Ok(package);
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