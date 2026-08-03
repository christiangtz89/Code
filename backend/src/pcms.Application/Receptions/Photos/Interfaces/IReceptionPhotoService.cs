using pcms.Application.Receptions.Photos.DTOs;
using pcms.Domain.Enums;

namespace pcms.Application.Receptions.Photos.Interfaces;

public interface IReceptionPhotoService
{
    Task<ReceptionPhotoDto> UploadAsync(
        Guid receptionId,
        Guid uploadedByUserId,
        ReceptionPhotoType photoType,
        string originalFileName,
        string contentType,
        long fileSize,
        Stream fileStream,
        string? notes);

    Task<IEnumerable<ReceptionPhotoDto>>
        GetByReceptionIdAsync(Guid receptionId);

    Task<ReceptionPhotoDto?> GetByIdAsync(Guid id);

    Task<bool> DeactivateAsync(Guid id);

    Task<bool> RestoreAsync(Guid id);
}