using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Collections.Photos.DTOs;
using pcms.Application.Collections.Photos.Interfaces;
using pcms.Domain.Enums;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CollectionPhotosController : ControllerBase
{
    private readonly ICollectionPhotoService
        _collectionPhotoService;

    public CollectionPhotosController(
        ICollectionPhotoService collectionPhotoService)
    {
        _collectionPhotoService =
            collectionPhotoService;
    }

    [HttpPost("collection/{collectionId:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<
    ActionResult<CollectionPhotoDto>>
    Upload(
        Guid collectionId,
        IFormFile file,
        [FromForm] CollectionPhotoType photoType,
        [FromForm] string? notes)
    {
        var uploadedByUserId =
            GetCurrentUserId();

        if (!uploadedByUserId.HasValue)
        {
            return Unauthorized(new
            {
                success = false,
                message =
                    "No se pudo identificar al usuario autenticado."
            });
        }

        if (file == null ||
            file.Length == 0)
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "Debe seleccionar una fotografía."
            });
        }

        try
        {
            await using var fileStream =
                file.OpenReadStream();

            var photo =
                await _collectionPhotoService
                    .UploadAsync(
                        collectionId,
                        uploadedByUserId.Value,
                        photoType,
                        file.FileName,
                        file.ContentType,
                        file.Length,
                        fileStream,
                        notes);

            return CreatedAtAction(
                nameof(GetById),
                new { id = photo.Id },
                photo);
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

    [HttpGet("collection/{collectionId:guid}")]
    public async Task<
        ActionResult<IEnumerable<CollectionPhotoDto>>>
        GetByCollectionId(
            Guid collectionId)
    {
        var photos =
            await _collectionPhotoService
                .GetByCollectionIdAsync(
                    collectionId);

        return Ok(photos);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CollectionPhotoDto>>
        GetById(
            Guid id)
    {
        var photo =
            await _collectionPhotoService
                .GetByIdAsync(id);

        if (photo == null)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "La fotografía no fue encontrada."
            });
        }

        return Ok(photo);
    }

    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> GetFile(
        Guid id)
    {
        try
        {
            var photoFile =
                await _collectionPhotoService
                    .GetFileAsync(id);

            if (photoFile == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "La fotografía no fue encontrada."
                });
            }

            return File(
                photoFile.Content,
                photoFile.ContentType,
                enableRangeProcessing: true);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(
        Guid id)
    {
        var success =
            await _collectionPhotoService
                .DeactivateAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "No se encontró una fotografía activa."
            });
        }

        return NoContent();
    }

    [HttpPatch("{id:guid}/restore")]
    public async Task<IActionResult> Restore(
        Guid id)
    {
        var success =
            await _collectionPhotoService
                .RestoreAsync(id);

        if (!success)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "No se encontró una fotografía inactiva."
            });
        }

        return NoContent();
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