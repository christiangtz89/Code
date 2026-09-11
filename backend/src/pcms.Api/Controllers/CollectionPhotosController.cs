using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Auth;
using pcms.Application.Collections.Photos.DTOs;
using pcms.Application.Collections.Photos.Interfaces;
using pcms.Domain.Enums;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Collections.View")]
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
                        CanManageCollections(),
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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("collection/{collectionId:guid}")]
    public async Task<
        ActionResult<IEnumerable<CollectionPhotoDto>>>
        GetByCollectionId(
            Guid collectionId)
    {
        var actorUserId = GetCurrentUserId();
        if (!actorUserId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var photos = await _collectionPhotoService
                .GetByCollectionIdAsync(
                    collectionId,
                    actorUserId.Value,
                    CanManageCollections());

            return Ok(photos);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CollectionPhotoDto>>
        GetById(
            Guid id)
    {
        var actorUserId = GetCurrentUserId();
        if (!actorUserId.HasValue)
        {
            return Unauthorized();
        }

        CollectionPhotoDto? photo;
        try
        {
            photo = await _collectionPhotoService.GetByIdAsync(
                id,
                actorUserId.Value,
                CanManageCollections());
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }

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
        var actorUserId = GetCurrentUserId();
        if (!actorUserId.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            var photoFile =
                await _collectionPhotoService
                    .GetFileAsync(
                        id,
                        actorUserId.Value,
                        CanManageCollections());

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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(
        Guid id)
    {
        var actorUserId = GetCurrentUserId();
        if (!actorUserId.HasValue)
        {
            return Unauthorized();
        }

        bool success;
        try
        {
            success = await _collectionPhotoService.DeactivateAsync(
                id,
                actorUserId.Value,
                CanManageCollections());
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }

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
        var actorUserId = GetCurrentUserId();
        if (!actorUserId.HasValue)
        {
            return Unauthorized();
        }

        bool success;
        try
        {
            success = await _collectionPhotoService.RestoreAsync(
                id,
                actorUserId.Value,
                CanManageCollections());
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }

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

    private bool CanManageCollections()
    {
        return User.HasClaim("pcms_owner", "true") ||
            PermissionImplications.Satisfies(
                User.FindAll("permission").Select(claim => claim.Value),
                PermissionCodes.CollectionsManage);
    }
}
