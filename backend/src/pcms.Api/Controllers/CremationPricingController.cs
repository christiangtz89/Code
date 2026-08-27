using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.CremationPricing.DTOs;
using pcms.Application.CremationPricing.Interfaces;
using pcms.Domain.Enums;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "CremationPricing.View")]
public class CremationPricingController : ControllerBase
{
    private readonly ICremationPricingService _cremationPricingService;

    public CremationPricingController(
        ICremationPricingService cremationPricingService)
    {
        _cremationPricingService = cremationPricingService;
    }

    [HttpGet("configuration")]
    public async Task<ActionResult<CremationPricingConfigurationDto>>
        GetConfiguration()
    {
        try
        {
            var configuration =
                await _cremationPricingService.GetConfigurationAsync();

            return Ok(configuration);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("configuration")]
    [Authorize(Policy = "CremationPricing.Manage")]
    public async Task<ActionResult<CremationPricingConfigurationDto>>
        UpdateConfiguration(
            [FromBody] UpdateCremationPricingConfigurationDto dto)
    {
        try
        {
            var configuration =
                await _cremationPricingService
                    .UpdateConfigurationAsync(dto);

            return Ok(configuration);
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

    [HttpGet("prices")]
    public async Task<ActionResult<IEnumerable<CremationPriceDto>>>
        GetPrices(
            [FromQuery] Guid? cremationPackageId = null,
            [FromQuery] CremationType? cremationType = null,
            [FromQuery] bool includeInactive = false)
    {
        try
        {
            var prices =
                await _cremationPricingService.GetPricesAsync(
                    cremationPackageId,
                    cremationType,
                    includeInactive);

            return Ok(prices);
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

    [HttpGet("prices/{id:guid}")]
    public async Task<ActionResult<CremationPriceDto>>
        GetPriceById(Guid id)
    {
        var price =
            await _cremationPricingService.GetPriceByIdAsync(id);

        if (price is null)
        {
            return NotFound(new
            {
                message = "El precio de cremación no fue encontrado."
            });
        }

        return Ok(price);
    }

    [HttpPost("prices")]
    [Authorize(Policy = "CremationPricing.Manage")]
    public async Task<ActionResult<CremationPriceDto>>
        CreatePrice(
            [FromBody] CreateCremationPriceDto dto)
    {
        try
        {
            var price =
                await _cremationPricingService.CreatePriceAsync(dto);

            return CreatedAtAction(
                nameof(GetPriceById),
                new { id = price.Id },
                price);
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

    [HttpPut("prices/{id:guid}")]
    [Authorize(Policy = "CremationPricing.Manage")]
    public async Task<ActionResult<CremationPriceDto>>
        UpdatePrice(
            Guid id,
            [FromBody] UpdateCremationPriceDto dto)
    {
        try
        {
            var price =
                await _cremationPricingService.UpdatePriceAsync(
                    id,
                    dto);

            if (price is null)
            {
                return NotFound(new
                {
                    message =
                        "El precio de cremación no fue encontrado."
                });
            }

            return Ok(price);
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

    [HttpGet("quote")]
    public async Task<ActionResult<CremationPriceQuoteDto>>
        GetQuote(
            [FromQuery] Guid cremationPackageId,
            [FromQuery] decimal weightKg,
            [FromQuery] CremationType? cremationType = null)
    {
        try
        {
            var quote =
                await _cremationPricingService.GetQuoteAsync(
                    cremationPackageId,
                    weightKg,
                    cremationType);

            return Ok(quote);
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
