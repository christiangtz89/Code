using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Collections.DTOs;
using pcms.Application.Collections.Interfaces;
using pcms.Application.Receptions.Exceptions;
using pcms.Domain.Enums;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CollectionsController : ControllerBase
{
    private readonly ICollectionService _collectionService;

    public CollectionsController(
        ICollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    [HttpPost]
    public async Task<ActionResult<CollectionDto>> Create(
        CreateCollectionDto dto)
    {
        var collectedByUserId =
            GetCurrentUserId();

        if (!collectedByUserId.HasValue)
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
            var collection =
                await _collectionService.CreateAsync(
                    dto,
                    collectedByUserId.Value);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = collection.Id
                },
                collection);
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
    public async Task<ActionResult<PagedCollectionsDto>>
        GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] CollectionStatus? status = null,
            [FromQuery] CollectionLocationType? locationType = null)
    {
        try
        {
            var collections =
                await _collectionService.GetAllAsync(
                    page,
                    pageSize,
                    status,
                    locationType);

            return Ok(collections);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CollectionDto>>
        GetById(
            Guid id)
    {
        var collection =
            await _collectionService
                .GetByIdAsync(id);

        if (collection == null)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "La recolección no fue encontrada."
            });
        }

        return Ok(collection);
    }

    [HttpGet("qr/{qrCode}")]
    public async Task<ActionResult<CollectionDto>>
        GetByQrCode(
            string qrCode)
    {
        var collection =
            await _collectionService
                .GetByQrCodeAsync(qrCode);

        if (collection == null)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "No se encontró una recolección con ese código QR."
            });
        }

        return Ok(collection);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CollectionDto>>
        Update(
            Guid id,
            UpdateCollectionDto dto)
    {
        try
        {
            var collection =
                await _collectionService.UpdateAsync(
                    id,
                    dto);

            if (collection == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "La recolección no fue encontrada."
                });
            }

            return Ok(collection);
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
    public async Task<ActionResult<CollectionDto>>
        ChangeStatus(
            Guid id,
            ChangeCollectionStatusDto dto)
    {
        try
        {
            var collection =
                await _collectionService
                    .ChangeStatusAsync(
                        id,
                        dto);

            if (collection == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "La recolección no fue encontrada."
                });
            }

            return Ok(collection);
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

    [HttpPost("{id:guid}/convert-to-reception")]
    public async Task<ActionResult<CollectionDto>>
        ConvertToReception(
            Guid id,
            ConvertCollectionToReceptionDto dto)
    {
        var receivedByUserId =
            GetCurrentUserId();

        if (!receivedByUserId.HasValue)
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
            var collection =
                await _collectionService
                    .ConvertToReceptionAsync(
                        id,
                        dto,
                        receivedByUserId.Value);

            if (collection == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "La recolección no fue encontrada."
                });
            }

            return Ok(collection);
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

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<CollectionDto>>>
        Search(
            [FromQuery] string search,
            [FromQuery] CollectionStatus? status = null,
            [FromQuery] CollectionLocationType? locationType = null)
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

        try
        {
            var collections =
                await _collectionService.SearchAsync(
                    search,
                    status,
                    locationType);

            return Ok(collections);
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

    private Guid? GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        return Guid.TryParse(
            userIdValue,
            out var userId)
            ? userId
            : null;
    }
}