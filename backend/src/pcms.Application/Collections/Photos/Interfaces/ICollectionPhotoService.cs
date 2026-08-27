using pcms.Application.Collections.Photos.DTOs;
using pcms.Domain.Enums;

namespace pcms.Application.Collections.Photos.Interfaces;

public interface ICollectionPhotoService
{
    Task<CollectionPhotoDto> UploadAsync(
        Guid collectionId,
        Guid uploadedByUserId,
        CollectionPhotoType photoType,
        string originalFileName,
        string contentType,
        long fileSize,
        Stream fileStream,
        string? notes);

    Task<IEnumerable<CollectionPhotoDto>>
        GetByCollectionIdAsync(
            Guid collectionId);

    Task<CollectionPhotoDto?> GetByIdAsync(
        Guid id);

    Task<CollectionPhotoFileDto?> GetFileAsync(
    Guid id);

    Task<bool> DeactivateAsync(
        Guid id);

    Task<bool> RestoreAsync(
        Guid id);
}