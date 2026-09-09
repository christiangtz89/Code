using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Common;
using pcms.Application.VeterinaryRequests.DTOs;
using pcms.Application.VeterinaryRequests.Interfaces;
using pcms.Domain.Enums;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "VeterinaryRequests.View")]
public class VeterinaryRequestsController
    : ControllerBase
{
    private readonly IVeterinaryRequestService
        _veterinaryRequestService;

    public VeterinaryRequestsController(
        IVeterinaryRequestService
            veterinaryRequestService)
    {
        _veterinaryRequestService =
            veterinaryRequestService;
    }

    [HttpPost]
    [Authorize(Policy = "VeterinaryRequests.Manage")]
    public async Task<ActionResult<VeterinaryRequestDto>>
        Create(
            [FromBody]
            CreateVeterinaryRequestDto dto)
    {
        var userId =
            GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(
                "No fue posible identificar al usuario autenticado.");
        }

        try
        {
            var request =
                await _veterinaryRequestService
                    .CreateAsync(
                        dto,
                        userId.Value);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = request.Id
                },
                request);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<
        ActionResult<PagedVeterinaryRequestsDto>>
        GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery]
            VeterinaryRequestStatus? status = null)
    {
        if (page < 1)
        {
            return BadRequest(
                "La página debe ser mayor que cero.");
        }

        if (pageSize < 1 ||
            pageSize > 100)
        {
            return BadRequest(
                "El tamaño de página debe estar entre 1 y 100.");
        }

        if (status.HasValue &&
            !Enum.IsDefined(
                typeof(VeterinaryRequestStatus),
                status.Value))
        {
            return BadRequest(
                "El estado de la solicitud no es válido.");
        }

        try
        {
            var result =
                await _veterinaryRequestService
                    .GetAllAsync(
                        page,
                        pageSize,
                        status);

            return Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<
        ActionResult<VeterinaryRequestDto>>
        GetById(Guid id)
    {
        var request =
            await _veterinaryRequestService
                .GetByIdAsync(id);

        if (request == null)
        {
            return NotFound(
                "No se encontró la solicitud veterinaria.");
        }

        return Ok(request);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "VeterinaryRequests.Manage")]
    public async Task<
        ActionResult<VeterinaryRequestDto>>
        Update(
            Guid id,
            [FromBody]
            UpdateVeterinaryRequestDto dto)
    {
        try
        {
            var request =
                await _veterinaryRequestService
                    .UpdateAsync(
                        id,
                        dto);

            if (request == null)
            {
                return NotFound(
                    "No se encontró la solicitud veterinaria.");
            }

            return Ok(request);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "VeterinaryRequests.Manage")]
    public async Task<
        ActionResult<VeterinaryRequestDto>>
        ChangeStatus(
            Guid id,
            [FromBody]
            ChangeVeterinaryRequestStatusDto dto)
    {
        var userId =
            GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(
                "No fue posible identificar al usuario autenticado.");
        }

        if (!Enum.IsDefined(
                typeof(VeterinaryRequestStatus),
                dto.Status))
        {
            return BadRequest(
                "El estado de la solicitud no es válido.");
        }

        try
        {
            var request =
                await _veterinaryRequestService
                    .ChangeStatusAsync(
                        id,
                        dto,
                        userId.Value);

            if (request == null)
            {
                return NotFound(
                    "No se encontró la solicitud veterinaria.");
            }

            return Ok(request);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/convert")]
    [Authorize(Policy = "VeterinaryRequests.Manage")]
    public async Task<
        ActionResult<VeterinaryRequestDto>>
        Convert(
            Guid id,
            [FromBody]
            ConvertVeterinaryRequestDto dto)
    {
        var userId =
            GetCurrentUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(
                "No fue posible identificar al usuario autenticado.");
        }

        try
        {
            var request =
                await _veterinaryRequestService
                    .ConvertAsync(
                        id,
                        dto,
                        userId.Value);

            if (request == null)
            {
                return NotFound(
                    "No se encontró la solicitud veterinaria.");
            }

            return Ok(request);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("search")]
    public async Task<
        ActionResult<PagedVeterinaryRequestsDto>>
        Search(
            [FromQuery] string search,
            [FromQuery]
            VeterinaryRequestStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return BadRequest(
                "Debe proporcionar un término de búsqueda.");
        }

        if (status.HasValue &&
            !Enum.IsDefined(
                typeof(VeterinaryRequestStatus),
                status.Value))
        {
            return BadRequest(
                "El estado de la solicitud no es válido.");
        }

        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(
                "Los parámetros de paginación no son válidos.");
        }

        var requests =
            await _veterinaryRequestService
                .SearchAsync(
                    search,
                    status,
                    page,
                    pageSize);

        return Ok(requests);
    }

    [HttpGet("lookup/clinics")]
    [Authorize(Policy = "VeterinaryRequests.Manage")]
    public async Task<ActionResult<
        PaginatedResult<VeterinaryRequestClinicOptionDto>>>
        GetClinicOptions(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
    {
        return Ok(await _veterinaryRequestService
            .GetClinicOptionsAsync(search, page, pageSize));
    }

    [HttpGet("lookup/veterinarians")]
    [Authorize(Policy = "VeterinaryRequests.Manage")]
    public async Task<ActionResult<
        PaginatedResult<VeterinaryRequestVeterinarianOptionDto>>>
        GetVeterinarianOptions(
            [FromQuery] Guid? veterinaryClinicId = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
    {
        return Ok(await _veterinaryRequestService
            .GetVeterinarianOptionsAsync(
                veterinaryClinicId,
                search,
                page,
                pageSize));
    }

    [HttpGet("lookup/customers")]
    [Authorize(Policy = "VeterinaryRequests.Manage")]
    public async Task<ActionResult<
        PaginatedResult<VeterinaryRequestCustomerOptionDto>>>
        GetCustomerOptions(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
    {
        return Ok(await _veterinaryRequestService
            .GetCustomerOptionsAsync(search, page, pageSize));
    }

    [HttpGet("lookup/pets")]
    [Authorize(Policy = "VeterinaryRequests.Manage")]
    public async Task<ActionResult<
        PaginatedResult<VeterinaryRequestPetOptionDto>>>
        GetPetOptions(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
    {
        return Ok(await _veterinaryRequestService
            .GetPetOptionsAsync(search, page, pageSize));
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirst(
                    ClaimTypes.NameIdentifier)
                ?.Value
            ?? User.FindFirst("sub")
                ?.Value
            ?? User.FindFirst("userId")
                ?.Value;

        return Guid.TryParse(
            userIdValue,
            out var userId)
            ? userId
            : null;
    }
}
