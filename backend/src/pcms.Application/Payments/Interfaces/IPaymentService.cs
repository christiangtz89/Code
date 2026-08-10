using pcms.Application.Payments.DTOs;
using pcms.Domain.Enums;

namespace pcms.Application.Payments.Interfaces;

public interface IPaymentService
{
    Task<PaymentAccountDto> CreateAccountAsync(
        CreatePaymentAccountDto dto);

    Task<IEnumerable<PaymentCremationOptionDto>>
        GetAvailableCremationOptionsAsync();

    Task<PagedPaymentAccountsDto> GetAllAsync(
        int page,
        int pageSize,
        PaymentStatus? status);

    Task<PaymentAccountDto?> GetByIdAsync(
        Guid id);

    Task<PaymentAccountDto?> GetByCremationIdAsync(
        Guid cremationId);

    Task<PaymentAccountDto?> UpdateAccountAsync(
        Guid id,
        UpdatePaymentAccountDto dto);

    Task<PaymentDto> AddPaymentAsync(
        Guid paymentAccountId,
        CreatePaymentDto dto,
        Guid recordedByUserId);

    Task<IEnumerable<PaymentDto>> GetPaymentHistoryAsync(
        Guid paymentAccountId);

    Task<IEnumerable<PaymentAccountDto>> SearchAsync(
        string search,
        PaymentStatus? status);
}