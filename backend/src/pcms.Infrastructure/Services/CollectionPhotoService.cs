using System.Buffers.Binary;
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
        bool canManageCollections,
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

        var collection = await RequireCollectionAccessAsync(
            collectionId,
            uploadedByUserId,
            canManageCollections,
            requireMutableWorkflow: true);

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

        var fileBytes = new byte[(int)fileSize];

        try
        {
            await fileStream.ReadExactlyAsync(fileBytes.AsMemory());
        }
        catch (EndOfStreamException)
        {
            throw new ArgumentException(
                "La fotografía está incompleta.");
        }

        if (await fileStream.ReadAsync(new byte[1]) != 0 ||
            !MatchesImageFormat(fileBytes, normalizedContentType))
        {
            throw new ArgumentException(
                "El contenido de la fotografía no coincide con el formato JPEG, PNG o WebP indicado.");
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

                UploadedByUserNameSnapshot =
                    uploadedByUser.FirstName + " " + uploadedByUser.LastName,

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

        try
        {
            await using (var outputStream =
                new FileStream(
                    absolutePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
            {
                await outputStream.WriteAsync(fileBytes);
            }

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
            Guid collectionId,
            Guid actorUserId,
            bool canManageCollections)
    {
        _ = await RequireCollectionAccessAsync(
            collectionId,
            actorUserId,
            canManageCollections,
            requireMutableWorkflow: false);

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

                    UploadedByUserName = photo.UploadedByUserNameSnapshot,

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
            Guid id,
            Guid actorUserId,
            bool canManageCollections)
    {
        var collectionId = await _context.CollectionPhotos
            .AsNoTracking()
            .Where(photo => photo.Id == id && photo.IsActive)
            .Select(photo => (Guid?)photo.CollectionId)
            .FirstOrDefaultAsync();

        if (!collectionId.HasValue)
        {
            return null;
        }

        _ = await RequireCollectionAccessAsync(
            collectionId.Value,
            actorUserId,
            canManageCollections,
            requireMutableWorkflow: false);

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

                    UploadedByUserName = photo.UploadedByUserNameSnapshot,

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
        Guid id,
        Guid actorUserId,
        bool canManageCollections)
    {
        var photo =
            await _context.CollectionPhotos
                .AsNoTracking()
                .Where(photo =>
                    photo.Id == id &&
                    photo.IsActive)
                .Select(photo => new
                {
                    photo.CollectionId,
                    photo.StoragePath,
                    photo.ContentType
                })
                .FirstOrDefaultAsync();

        if (photo == null)
        {
            return null;
        }

        _ = await RequireCollectionAccessAsync(
            photo.CollectionId,
            actorUserId,
            canManageCollections,
            requireMutableWorkflow: false);

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
        Guid id,
        Guid actorUserId,
        bool canManageCollections)
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

        _ = await RequireCollectionAccessAsync(
            photo.CollectionId,
            actorUserId,
            canManageCollections,
            requireMutableWorkflow: true);

        photo.IsActive =
            false;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RestoreAsync(
        Guid id,
        Guid actorUserId,
        bool canManageCollections)
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

        _ = await RequireCollectionAccessAsync(
            photo.CollectionId,
            actorUserId,
            canManageCollections,
            requireMutableWorkflow: true);

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

    private async Task<Collection> RequireCollectionAccessAsync(
        Guid collectionId,
        Guid actorUserId,
        bool canManageCollections,
        bool requireMutableWorkflow)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException();
        }

        var actorIsActive = await _context.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == actorUserId && user.IsActive);

        if (!actorIsActive)
        {
            throw new UnauthorizedAccessException();
        }

        var collection = await _context.Collections
            .FirstOrDefaultAsync(current =>
                current.Id == collectionId && current.IsActive)
            ?? throw new InvalidOperationException(
                "La recolección no existe o está inactiva.");

        if (!canManageCollections &&
            collection.AssignedDriverId != actorUserId)
        {
            throw new UnauthorizedAccessException();
        }

        if (requireMutableWorkflow &&
            collection.Status is CollectionStatus.Received or
                CollectionStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "No se pueden modificar fotografías de una recolección recibida o cancelada.");
        }

        return collection;
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

    private static bool MatchesImageFormat(
        ReadOnlySpan<byte> bytes,
        string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => HasJpegStructure(bytes),
            "image/png" => HasPngStructure(bytes),
            "image/webp" => HasWebPHeader(bytes),
            _ => false
        };
    }

    private static bool HasJpegStructure(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 16 ||
            !bytes[..3].SequenceEqual(new byte[] { 0xff, 0xd8, 0xff }) ||
            !bytes[^2..].SequenceEqual(new byte[] { 0xff, 0xd9 }))
        {
            return false;
        }

        var offset = 2;
        var hasFrame = false;

        while (offset < bytes.Length - 2)
        {
            if (bytes[offset++] != 0xff)
            {
                return false;
            }

            while (offset < bytes.Length - 2 &&
                   bytes[offset] == 0xff)
            {
                offset++;
            }

            if (offset >= bytes.Length - 2)
            {
                return false;
            }

            var marker = bytes[offset++];
            if (marker is 0x00 or 0xd9)
            {
                return false;
            }

            if (marker == 0x01 ||
                marker is >= 0xd0 and <= 0xd7)
            {
                continue;
            }

            if (offset + 2 > bytes.Length - 2)
            {
                return false;
            }

            var segmentLength =
                BinaryPrimitives.ReadUInt16BigEndian(
                    bytes[offset..(offset + 2)]);

            if (segmentLength < 2 ||
                offset + segmentLength > bytes.Length - 2)
            {
                return false;
            }

            if (marker >= 0xc0 && marker <= 0xcf &&
                marker is not (0xc4 or 0xc8 or 0xcc))
            {
                if (segmentLength < 8 ||
                    BinaryPrimitives.ReadUInt16BigEndian(
                        bytes[(offset + 3)..(offset + 5)]) == 0 ||
                    BinaryPrimitives.ReadUInt16BigEndian(
                        bytes[(offset + 5)..(offset + 7)]) == 0)
                {
                    return false;
                }

                hasFrame = true;
            }

            if (marker == 0xda)
            {
                return hasFrame &&
                    segmentLength >= 6 &&
                    offset + segmentLength < bytes.Length - 2;
            }

            offset += segmentLength;
        }

        return false;
    }

    private static bool HasPngStructure(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 45 ||
            !bytes[..8].SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4e, 0x47,
                    0x0d, 0x0a, 0x1a, 0x0a }))
        {
            return false;
        }

        var offset = 8;
        var firstChunk = true;
        var hasImageData = false;
        byte? zlibMethod = null;
        byte? zlibFlags = null;

        while (offset + 12 <= bytes.Length)
        {
            var length =
                BinaryPrimitives.ReadUInt32BigEndian(
                    bytes[offset..(offset + 4)]);
            var nextOffset = (long)offset + 12 + length;

            if (nextOffset > bytes.Length)
            {
                return false;
            }

            var chunkType = bytes.Slice(offset + 4, 4);
            var dataEnd = offset + 8 + (int)length;
            var expectedCrc =
                BinaryPrimitives.ReadUInt32BigEndian(
                    bytes[dataEnd..(dataEnd + 4)]);

            if (ComputePngCrc(
                    bytes.Slice(offset + 4, 4 + (int)length)) !=
                expectedCrc)
            {
                return false;
            }

            if (firstChunk)
            {
                if (!chunkType.SequenceEqual("IHDR"u8) ||
                    length != 13 ||
                    BinaryPrimitives.ReadUInt32BigEndian(
                        bytes[(offset + 8)..(offset + 12)]) == 0 ||
                    BinaryPrimitives.ReadUInt32BigEndian(
                        bytes[(offset + 12)..(offset + 16)]) == 0)
                {
                    return false;
                }

                firstChunk = false;
            }
            else if (chunkType.SequenceEqual("IDAT"u8) && length > 0)
            {
                hasImageData = true;

                var imageData = bytes.Slice(offset + 8, (int)length);
                foreach (var value in imageData)
                {
                    if (zlibMethod == null)
                    {
                        zlibMethod = value;
                    }
                    else if (zlibFlags == null)
                    {
                        zlibFlags = value;
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (chunkType.SequenceEqual("IEND"u8))
            {
                return length == 0 &&
                    hasImageData &&
                    zlibMethod.HasValue &&
                    zlibFlags.HasValue &&
                    (zlibMethod.Value & 0x0f) == 8 &&
                    (zlibMethod.Value >> 4) <= 7 &&
                    ((zlibMethod.Value << 8) |
                        zlibFlags.Value) % 31 == 0 &&
                    nextOffset == bytes.Length;
            }

            offset = (int)nextOffset;
        }

        return false;
    }

    private static uint ComputePngCrc(ReadOnlySpan<byte> bytes)
    {
        var crc = uint.MaxValue;

        foreach (var value in bytes)
        {
            crc ^= value;

            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) == 0
                    ? crc >> 1
                    : (crc >> 1) ^ 0xedb88320u;
            }
        }

        return ~crc;
    }

    private static bool HasWebPHeader(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 20 ||
            !bytes[..4].SequenceEqual("RIFF"u8) ||
            !bytes[8..12].SequenceEqual("WEBP"u8) ||
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..8]) !=
                (uint)(bytes.Length - 8) ||
            (!bytes[12..16].SequenceEqual("VP8 "u8) &&
             !bytes[12..16].SequenceEqual("VP8L"u8) &&
             !bytes[12..16].SequenceEqual("VP8X"u8)))
        {
            return false;
        }

        return HasWebPImageChunks(bytes[12..], allowAnimation: true);
    }

    private static bool HasWebPImageChunks(
        ReadOnlySpan<byte> chunks,
        bool allowAnimation)
    {
        var offset = 0;
        var hasImageData = false;

        while (offset + 8 <= chunks.Length)
        {
            var length =
                BinaryPrimitives.ReadUInt32LittleEndian(
                    chunks[(offset + 4)..(offset + 8)]);
            var nextOffset = (long)offset + 8 +
                length + (length & 1);

            if (nextOffset > chunks.Length)
            {
                return false;
            }

            var chunkType = chunks.Slice(offset, 4);
            var data = chunks.Slice(offset + 8, (int)length);

            if (chunkType.SequenceEqual("VP8 "u8))
            {
                if (data.Length <= 10 ||
                    !data[3..6].SequenceEqual(
                        new byte[] { 0x9d, 0x01, 0x2a }) ||
                    (BinaryPrimitives.ReadUInt16LittleEndian(data[6..8]) &
                        0x3fff) == 0 ||
                    (BinaryPrimitives.ReadUInt16LittleEndian(data[8..10]) &
                        0x3fff) == 0)
                {
                    return false;
                }

                hasImageData = true;
            }
            else if (chunkType.SequenceEqual("VP8L"u8))
            {
                if (data.Length <= 5 || data[0] != 0x2f ||
                    (data[4] & 0xe0) != 0)
                {
                    return false;
                }

                hasImageData = true;
            }
            else if (chunkType.SequenceEqual("VP8X"u8) &&
                     data.Length != 10)
            {
                return false;
            }
            else if (chunkType.SequenceEqual("ANMF"u8))
            {
                if (!allowAnimation || data.Length < 24 ||
                    !HasWebPImageChunks(
                        data[16..],
                        allowAnimation: false))
                {
                    return false;
                }

                hasImageData = true;
            }

            offset = (int)nextOffset;
        }

        return offset == chunks.Length && hasImageData;
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

            UploadedByUserName = photo.UploadedByUserNameSnapshot,

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
