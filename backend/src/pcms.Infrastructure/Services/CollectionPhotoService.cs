using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using pcms.Application.Collections.Photos.DTOs;
using pcms.Application.Collections.Photos.Interfaces;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class CollectionPhotoService
    : ICollectionPhotoService
{
    private const long MaximumFileSize =
        10 * 1024 * 1024;

    private static readonly HashSet<string>
        AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    private readonly AppDbContext _context;
    private readonly IHostEnvironment _environment;

    public CollectionPhotoService(
        AppDbContext context,
        IHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<CollectionPhotoDto> UploadAsync(
        Guid collectionId,
        Guid uploadedByUserId,
        CollectionPhotoType photoType,
        string originalFileName,
        string contentType,
        long fileSize,
        Stream fileStream,
        string? notes)
    {
        if (collectionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Debe seleccionar una recolección válida.");
        }

        if (uploadedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "No se pudo identificar al usuario que sube la fotografía.");
        }

        if (!Enum.IsDefined(photoType))
        {
            throw new ArgumentException(
                "El tipo de fotografía no es válido.");
        }

        if (string.IsNullOrWhiteSpace(
                originalFileName))
        {
            throw new ArgumentException(
                "El nombre del archivo es obligatorio.");
        }

        var normalizedContentType =
            contentType.Trim().ToLowerInvariant();

        if (!AllowedContentTypes.Contains(
                normalizedContentType))
        {
            throw new ArgumentException(
                "Solo se permiten fotografías JPEG, PNG o WebP.");
        }

        if (fileSize <= 0)
        {
            throw new ArgumentException(
                "La fotografía está vacía.");
        }

        if (fileSize > MaximumFileSize)
        {
            throw new ArgumentException(
                "La fotografía no puede exceder 10 MB.");
        }

        if (!fileStream.CanRead)
        {
            throw new ArgumentException(
                "No fue posible leer la fotografía.");
        }

        var collection =
            await _context.Collections
                .FirstOrDefaultAsync(c =>
                    c.Id == collectionId &&
                    c.IsActive);

        if (collection == null)
        {
            throw new InvalidOperationException(
                "La recolección no existe o está inactiva.");
        }

        var uploadedByUser =
            await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == uploadedByUserId &&
                    u.IsActive);

        if (uploadedByUser == null)
        {
            throw new InvalidOperationException(
                "El usuario que sube la fotografía no existe o está inactivo.");
        }

        var extension =
            GetExtension(
                normalizedContentType);

        var storedFileName =
            $"{Guid.NewGuid():N}{extension}";

        var relativeDirectory =
    "collections";

        var absoluteDirectory =
            Path.Combine(
                _environment.ContentRootPath,
                "private-storage",
                relativeDirectory);

        Directory.CreateDirectory(
            absoluteDirectory);

        var absolutePath =
            Path.Combine(
                absoluteDirectory,
                storedFileName);

        var relativeStoragePath =
            Path.Combine(
                    relativeDirectory,
                    storedFileName)
                .Replace(
                    Path.DirectorySeparatorChar,
                    '/');

        var now =
            DateTime.UtcNow;

        var photo =
            new CollectionPhoto
            {
                Id = Guid.NewGuid(),

                CollectionId =
                    collection.Id,

                UploadedByUserId =
                    uploadedByUser.Id,

                PhotoType =
                    photoType,

                OriginalFileName =
                    Path.GetFileName(
                        originalFileName.Trim()),

                StoredFileName =
                    storedFileName,

                StoragePath =
                    relativeStoragePath,

                ContentType =
                    normalizedContentType,

                Notes =
                    string.IsNullOrWhiteSpace(notes)
                        ? null
                        : notes.Trim(),

                IsActive = true,

                UploadedAt = now
            };

        await using (
            var outputStream =
                new FileStream(
                    absolutePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
        {
            await fileStream.CopyToAsync(
                outputStream);
        }

        try
        {
            if (photoType ==
                CollectionPhotoType.PetIdentification)
            {
                var previousIdentificationPhotos =
                    await _context.CollectionPhotos
                        .Where(existing =>
                            existing.CollectionId ==
                                collection.Id &&
                            existing.PhotoType ==
                                CollectionPhotoType
                                    .PetIdentification &&
                            existing.IsActive)
                        .ToListAsync();

                foreach (
                    var previousPhoto
                    in previousIdentificationPhotos)
                {
                    previousPhoto.IsActive =
                        false;
                }
            }

            _context.CollectionPhotos.Add(
                photo);

            await _context.SaveChangesAsync();
        }
        catch
        {
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            throw;
        }

        return MapToDto(
            photo,
            uploadedByUser);
    }

    public async Task<
        IEnumerable<CollectionPhotoDto>>
        GetByCollectionIdAsync(
            Guid collectionId)
    {
        return await _context.CollectionPhotos
            .AsNoTracking()
            .Where(photo =>
                photo.CollectionId ==
                    collectionId &&
                photo.IsActive)
            .OrderBy(photo =>
                photo.PhotoType)
            .ThenByDescending(photo =>
                photo.UploadedAt)
            .Select(photo =>
                new CollectionPhotoDto
                {
                    Id =
                        photo.Id,

                    CollectionId =
                        photo.CollectionId,

                    UploadedByUserId =
                        photo.UploadedByUserId,

                    UploadedByUserName =
                        photo.UploadedByUser
                            .FirstName +
                        " " +
                        photo.UploadedByUser
                            .LastName,

                    PhotoType =
                        photo.PhotoType,

                    OriginalFileName =
                        photo.OriginalFileName,

                    ContentType =
                        photo.ContentType,

                    FileUrl =
                        ToFileUrl(
    photo.Id),

                    Notes =
                        photo.Notes,

                    IsActive =
                        photo.IsActive,

                    UploadedAt =
                        photo.UploadedAt
                })
            .ToListAsync();
    }

    public async Task<CollectionPhotoDto?>
        GetByIdAsync(
            Guid id)
    {
        return await _context.CollectionPhotos
            .AsNoTracking()
            .Where(photo =>
                photo.Id == id &&
                photo.IsActive)
            .Select(photo =>
                new CollectionPhotoDto
                {
                    Id =
                        photo.Id,

                    CollectionId =
                        photo.CollectionId,

                    UploadedByUserId =
                        photo.UploadedByUserId,

                    UploadedByUserName =
                        photo.UploadedByUser
                            .FirstName +
                        " " +
                        photo.UploadedByUser
                            .LastName,

                    PhotoType =
                        photo.PhotoType,

                    OriginalFileName =
                        photo.OriginalFileName,

                    ContentType =
                        photo.ContentType,

                    FileUrl =
                        ToFileUrl(
    photo.Id),

                    Notes =
                        photo.Notes,

                    IsActive =
                        photo.IsActive,

                    UploadedAt =
                        photo.UploadedAt
                })
            .FirstOrDefaultAsync();
    }

    public async Task<CollectionPhotoFileDto?>
    GetFileAsync(
        Guid id)
    {
        var photo =
            await _context.CollectionPhotos
                .AsNoTracking()
                .Where(photo =>
                    photo.Id == id &&
                    photo.IsActive)
                .Select(photo => new
                {
                    photo.StoragePath,
                    photo.ContentType
                })
                .FirstOrDefaultAsync();

        if (photo == null)
        {
            return null;
        }

        var privateStorageRoot =
            Path.GetFullPath(
                Path.Combine(
                    _environment.ContentRootPath,
                    "private-storage"));

        var relativeStoragePath =
            photo.StoragePath
                .Replace(
                    '/',
                    Path.DirectorySeparatorChar);

        var absolutePath =
            Path.GetFullPath(
                Path.Combine(
                    privateStorageRoot,
                    relativeStoragePath));

        var expectedPrefix =
            privateStorageRoot.TrimEnd(
                Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        if (!absolutePath.StartsWith(
                expectedPrefix,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "La ruta de almacenamiento de la fotografía no es válida.");
        }

        if (!File.Exists(absolutePath))
        {
            throw new InvalidOperationException(
                "El archivo de la fotografía no fue encontrado.");
        }

        var fileStream =
            new FileStream(
                absolutePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

        return new CollectionPhotoFileDto
        {
            Content =
                fileStream,

            ContentType =
                photo.ContentType
        };
    }

    public async Task<bool> DeactivateAsync(
        Guid id)
    {
        var photo =
            await _context.CollectionPhotos
                .FirstOrDefaultAsync(photo =>
                    photo.Id == id &&
                    photo.IsActive);

        if (photo == null)
        {
            return false;
        }

        photo.IsActive =
            false;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RestoreAsync(
        Guid id)
    {
        var photo =
            await _context.CollectionPhotos
                .FirstOrDefaultAsync(photo =>
                    photo.Id == id &&
                    !photo.IsActive);

        if (photo == null)
        {
            return false;
        }

        if (photo.PhotoType ==
            CollectionPhotoType.PetIdentification)
        {
            var currentIdentificationPhotos =
                await _context.CollectionPhotos
                    .Where(existing =>
                        existing.CollectionId ==
                            photo.CollectionId &&
                        existing.Id !=
                            photo.Id &&
                        existing.PhotoType ==
                            CollectionPhotoType
                                .PetIdentification &&
                        existing.IsActive)
                    .ToListAsync();

            foreach (
                var currentPhoto
                in currentIdentificationPhotos)
            {
                currentPhoto.IsActive =
                    false;
            }
        }

        photo.IsActive =
            true;

        await _context.SaveChangesAsync();

        return true;
    }

    private static string GetExtension(
        string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",

            _ => throw new ArgumentException(
                "El tipo de archivo no es válido.")
        };
    }

    private static string ToFileUrl(
    Guid photoId)
    {
        return $"/api/CollectionPhotos/{photoId}/file";
    }

    private static CollectionPhotoDto MapToDto(
        CollectionPhoto photo,
        User uploadedByUser)
    {
        return new CollectionPhotoDto
        {
            Id =
                photo.Id,

            CollectionId =
                photo.CollectionId,

            UploadedByUserId =
                photo.UploadedByUserId,

            UploadedByUserName =
                uploadedByUser.FirstName +
                " " +
                uploadedByUser.LastName,

            PhotoType =
                photo.PhotoType,

            OriginalFileName =
                photo.OriginalFileName,

            ContentType =
                photo.ContentType,

            FileUrl =
                ToFileUrl(
    photo.Id),

            Notes =
                photo.Notes,

            IsActive =
                photo.IsActive,

            UploadedAt =
                photo.UploadedAt
        };
    }
}