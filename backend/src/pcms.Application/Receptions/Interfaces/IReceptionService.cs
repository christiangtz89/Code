using pcms.Application.Receptions.DTOs;

namespace pcms.Application.Receptions.Interfaces;

public interface IReceptionService
{
    Task<ReceptionDto> CreateAsync(
        CreateReceptionDto dto,
        Guid receivedByUserId);

    Task<PagedReceptionsDto> GetAllAsync(
        int page,
        int pageSize);

    Task<ReceptionDto?> GetByIdAsync(Guid id);

    Task<ReceptionDto?> GetByQrCodeAsync(string qrCode);

    Task<ReceptionDto?> UpdateAsync(
        Guid id,
        UpdateReceptionDto dto);

    Task<bool> DeactivateAsync(Guid id);

    Task<bool> RestoreAsync(Guid id);

    Task<IEnumerable<ReceptionDto>> SearchAsync(string search);
}