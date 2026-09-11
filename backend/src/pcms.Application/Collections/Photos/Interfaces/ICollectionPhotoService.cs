using pcms.Application.Collections.Photos.DTOs;
using pcms.Domain.Enums;

namespace pcms.Application.Collections.Photos.Interfaces;

public interface ICollectionPhotoService
{
    Task<CollectionPhotoDto> UploadAsync(
        Guid collectionId,
        Guid uploadedByUserId,
        bool canManageCollections,
        CollectionPhotoType photoType,
        string originalFileName,
        string contentType,
        long fileSize,
        Stream fileStream,
        string? notes);

    Task<IEnumerable<CollectionPhotoDto>>
        GetByCollectionIdAsync(
            Guid collectionId,
            Guid actorUserId,
            bool canManageCollections);

    Task<CollectionPhotoDto?> GetByIdAsync(
        Guid id,
        Guid actorUserId,
        bool canManageCollections);

    Task<CollectionPhotoFileDto?> GetFileAsync(
        Guid id,
        Guid actorUserId,
        bool canManageCollections);

    Task<bool> DeactivateAsync(
        Guid id,
        Guid actorUserId,
        bool canManageCollections);

    Task<bool> RestoreAsync(
        Guid id,
        Guid actorUserId,
        bool canManageCollections);
}
