using System.Data;
using Microsoft.EntityFrameworkCore;
using pcms.Application.Payments.DTOs;
using pcms.Application.Payments.Interfaces;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;

    public PaymentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentAccountDto> CreateAccountAsync(
        CreatePaymentAccountDto dto)
    {
        if (dto.CremationId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "La cremación es requerida.");
        }

        var cremation =
    await _context.Cremations
        .AsNoTracking()
        .FirstOrDefaultAsync(c =>
            c.Id == dto.CremationId &&
            c.IsActive);

        if (cremation == null)
        {
            throw new InvalidOperationException(
                "No se encontró una cremación activa.");
        }

        if (cremation.QuotedPrice is not decimal quotedPrice ||
    quotedPrice <= 0)
        {
            throw new InvalidOperationException(
                "La cremación no tiene una cotización histórica válida.");
        }

        var accountExists = await _context.PaymentAccounts
            .AsNoTracking()
            .AnyAsync(pa =>
                pa.CremationId == dto.CremationId);

        if (accountExists)
        {
            throw new InvalidOperationException(
                "La cremación ya tiene una cuenta de pago.");
        }

        var account = new PaymentAccount
        {
            Id = Guid.NewGuid(),
            CremationId = dto.CremationId,

            ServiceTotal =
    quotedPrice,
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentAccounts.Add(account);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(account.Id)
            ?? throw new InvalidOperationException(
                "No fue posible recuperar la cuenta de pago creada.");
    }

    public async Task<IEnumerable<PaymentCremationOptionDto>>
    GetAvailableCremationOptionsAsync()
    {
        return await _context.Cremations
            .AsNoTracking()
            .Where(c =>
                c.IsActive &&
                c.QuotedPrice.HasValue &&
                c.QuotedPrice.Value > 0m &&
                !_context.PaymentAccounts.Any(pa =>
                    pa.CremationId == c.Id))
            .OrderByDescending(c => c.CreatedAt)
            .Select(c =>
                new PaymentCremationOptionDto
                {
                    Id = c.Id,
                    QrCode = c.Reception.QrCode,
                    PetName = c.Reception.Pet.Name,
                    CustomerName =
                        c.Reception.Pet.Customer.SecondLastName == null
                            ? c.Reception.Pet.Customer.FirstName + " " +
                              c.Reception.Pet.Customer.LastName
                            : c.Reception.Pet.Customer.FirstName + " " +
                              c.Reception.Pet.Customer.LastName + " " +
                              c.Reception.Pet.Customer.SecondLastName,
                    PackageName = c.PackageName
                })
            .ToListAsync();
    }

    public async Task<PagedPaymentAccountsDto> GetAllAsync(
        int page,
        int pageSize,
        PaymentStatus? status)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;
        pageSize = Math.Min(pageSize, 100);

        var query = _context.PaymentAccounts
            .AsNoTracking()
            .AsQueryable();

        query = ApplyStatusFilter(query, status);

        var totalItems = await query.CountAsync();

        var accountIds = await query
            .OrderByDescending(pa => pa.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(pa => pa.Id)
            .ToListAsync();

        var items = await GetAccountDtosByIdsAsync(
            accountIds);

        return new PagedPaymentAccountsDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    totalItems / (double)pageSize)
        };
    }

    public async Task<PaymentAccountDto?> GetByIdAsync(
        Guid id)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var accounts = await GetAccountDtosByIdsAsync(
            new[] { id });

        return accounts.FirstOrDefault();
    }

    public async Task<PaymentAccountDto?> GetByCremationIdAsync(
        Guid cremationId)
    {
        if (cremationId == Guid.Empty)
        {
            return null;
        }

        var id = await _context.PaymentAccounts
            .AsNoTracking()
            .Where(pa =>
                pa.CremationId == cremationId)
            .Select(pa => (Guid?)pa.Id)
            .FirstOrDefaultAsync();

        if (id is null)
        {
            return null;
        }

        return await GetByIdAsync(id.Value);
    }

    public async Task<PaymentDto> AddPaymentAsync(
        Guid paymentAccountId,
        CreatePaymentDto dto,
        Guid recordedByUserId)
    {
        if (dto.Amount < 0.01m)
        {
            throw new InvalidOperationException(
                "El monto del pago debe ser mayor que cero.");
        }

        if (decimal.Round(dto.Amount, 2) != dto.Amount)
        {
            throw new InvalidOperationException(
                "El monto del pago no puede tener más de dos decimales.");
        }

        if (dto.Amount > 9999999999.99m)
        {
            throw new InvalidOperationException(
                "El monto del pago excede el máximo permitido.");
        }

        if (!Enum.IsDefined(
                typeof(PaymentMethod),
                dto.Method))
        {
            throw new InvalidOperationException(
                "El método de pago no es válido.");
        }

        if (dto.PaidAt == default)
        {
            throw new InvalidOperationException(
                "La fecha del pago es requerida.");
        }

        var paidAtUtc =
    NormalizeToUtc(dto.PaidAt);

        if (paidAtUtc > DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "La fecha del pago no puede estar en el futuro.");
        }

        if (recordedByUserId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "No fue posible identificar al usuario que registra el pago.");
        }

        var userIsActive = await _context.Users
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id == recordedByUserId &&
                user.IsActive);

        if (!userIsActive)
        {
            throw new InvalidOperationException(
                "El usuario que registra el pago no está activo.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var account = await _context.PaymentAccounts
            .Include(pa => pa.Cremation)
            .Include(pa => pa.Payments)
            .FirstOrDefaultAsync(pa =>
                pa.Id == paymentAccountId);

        if (account is null)
        {
            throw new KeyNotFoundException(
                "No se encontró la cuenta de pago.");
        }

        if (!account.Cremation.IsActive)
        {
            throw new InvalidOperationException(
                "No se pueden registrar pagos para una cremación inactiva.");
        }

        var amountPaid = account.Payments.Sum(
            payment => payment.Amount);

        var balance = account.ServiceTotal - amountPaid;

        if (balance <= 0)
        {
            throw new InvalidOperationException(
                "La cuenta ya se encuentra pagada en su totalidad.");
        }

        if (dto.Amount > balance)
        {
            throw new InvalidOperationException(
                $"El pago excede el saldo pendiente ({balance:C2}).");
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentAccountId = account.Id,
            RecordedByUserId = recordedByUserId,
            Amount = dto.Amount,
            Method = dto.Method,
            PaidAt = paidAtUtc,
            Reference = NormalizeOptional(dto.Reference),
            Notes = NormalizeOptional(dto.Notes),
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);

        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetPaymentDtoAsync(payment.Id)
            ?? throw new InvalidOperationException(
                "No fue posible recuperar el pago registrado.");
    }

    public async Task<IEnumerable<PaymentDto>>
        GetPaymentHistoryAsync(
            Guid paymentAccountId)
    {
        var accountExists = await _context.PaymentAccounts
            .AsNoTracking()
            .AnyAsync(pa =>
                pa.Id == paymentAccountId);

        if (!accountExists)
        {
            throw new KeyNotFoundException(
                "No se encontró la cuenta de pago.");
        }

        var payments = await _context.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.PaymentAccountId ==
                paymentAccountId)
            .Include(payment =>
                payment.RecordedByUser)
            .OrderByDescending(payment =>
                payment.PaidAt)
            .ThenByDescending(payment =>
                payment.CreatedAt)
            .ToListAsync();

        return payments.Select(MapPayment);
    }

    public async Task<IEnumerable<PaymentAccountDto>>
        SearchAsync(
            string search,
            PaymentStatus? status)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return Array.Empty<PaymentAccountDto>();
        }

        var term = search.Trim();
        var pattern = $"%{term}%";

        var query = _context.PaymentAccounts
            .AsNoTracking()
            .Where(pa =>
                EF.Functions.ILike(
                    pa.Cremation.Reception.QrCode,
                    pattern) ||
                EF.Functions.ILike(
                    pa.Cremation.Reception.Pet.Name,
                    pattern) ||
                EF.Functions.ILike(
                    pa.Cremation.Reception.Pet.Customer
                        .FirstName,
                    pattern) ||
                EF.Functions.ILike(
                    pa.Cremation.Reception.Pet.Customer
                        .LastName,
                    pattern) ||
                (
                    pa.Cremation.Reception.Pet.Customer
                        .SecondLastName != null &&
                    EF.Functions.ILike(
                        pa.Cremation.Reception.Pet.Customer
                            .SecondLastName!,
                        pattern)
                ) ||
                EF.Functions.ILike(
                    pa.Cremation.PackageName,
                    pattern));

        query = ApplyStatusFilter(
            query,
            status);

        var accountIds = await query
            .OrderByDescending(pa => pa.CreatedAt)
            .Select(pa => pa.Id)
            .ToListAsync();

        return await GetAccountDtosByIdsAsync(
            accountIds);
    }

    private static IQueryable<PaymentAccount>
        ApplyStatusFilter(
            IQueryable<PaymentAccount> query,
            PaymentStatus? status)
    {
        if (status is null)
        {
            return query;
        }

        return status.Value switch
        {
            PaymentStatus.Pending =>
                query.Where(pa =>
                    !pa.Payments.Any()),

            PaymentStatus.PartiallyPaid =>
                query.Where(pa =>
                    (
                        pa.Payments
                            .Select(payment =>
                                (decimal?)payment.Amount)
                            .Sum() ?? 0m
                    ) > 0m &&
                    (
                        pa.Payments
                            .Select(payment =>
                                (decimal?)payment.Amount)
                            .Sum() ?? 0m
                    ) < pa.ServiceTotal),

            PaymentStatus.Paid =>
                query.Where(pa =>
                    (
                        pa.Payments
                            .Select(payment =>
                                (decimal?)payment.Amount)
                            .Sum() ?? 0m
                    ) >= pa.ServiceTotal),

            _ => query
        };
    }

    private async Task<List<PaymentAccountDto>>
        GetAccountDtosByIdsAsync(
            IEnumerable<Guid> accountIds)
    {
        var orderedIds = accountIds.ToList();

        if (orderedIds.Count == 0)
        {
            return new List<PaymentAccountDto>();
        }

        var accounts = await _context.PaymentAccounts
            .AsNoTracking()
            .Where(pa =>
                orderedIds.Contains(pa.Id))
            .Include(pa => pa.Cremation)
                .ThenInclude(c => c.Reception)
                    .ThenInclude(r => r.Pet)
                        .ThenInclude(p => p.Customer)
            .Include(pa => pa.Payments)
                .ThenInclude(payment =>
                    payment.RecordedByUser)
            .ToListAsync();

        var accountDictionary = accounts.ToDictionary(
            account => account.Id);

        return orderedIds
            .Where(accountDictionary.ContainsKey)
            .Select(id =>
                MapAccount(accountDictionary[id]))
            .ToList();
    }

    private async Task<PaymentDto?> GetPaymentDtoAsync(
        Guid paymentId)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .Include(p => p.RecordedByUser)
            .FirstOrDefaultAsync(p =>
                p.Id == paymentId);

        return payment is null
            ? null
            : MapPayment(payment);
    }

    private static PaymentAccountDto MapAccount(
        PaymentAccount account)
    {
        var cremation = account.Cremation;
        var reception = cremation.Reception;
        var pet = reception.Pet;
        var customer = pet.Customer;

        var amountPaid = account.Payments.Sum(
            payment => payment.Amount);

        var balance = Math.Max(
            0m,
            account.ServiceTotal - amountPaid);

        return new PaymentAccountDto
        {
            Id = account.Id,
            CremationId = account.CremationId,
            ReceptionId = cremation.ReceptionId,
            QrCode = reception.QrCode,
            PetId = reception.Pet.Id,
            PetName = pet.Name,
            CustomerId = pet.CustomerId,
            CustomerName = BuildCustomerName(
                customer.FirstName,
                customer.LastName,
                customer.SecondLastName),
            PackageName = cremation.PackageName,
            IsCremationActive = cremation.IsActive,
            ServiceTotal = account.ServiceTotal,
            AmountPaid = amountPaid,
            Balance = balance,
            Status = GetPaymentStatus(
                account.ServiceTotal,
                amountPaid),
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt,
            Payments = account.Payments
                .OrderByDescending(payment =>
                    payment.PaidAt)
                .ThenByDescending(payment =>
                    payment.CreatedAt)
                .Select(MapPayment)
                .ToList()
        };
    }

    private static PaymentDto MapPayment(
        Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            PaymentAccountId =
                payment.PaymentAccountId,
            RecordedByUserId =
                payment.RecordedByUserId,
            RecordedByUserName =
                BuildUserName(
                    payment.RecordedByUser.FirstName,
                    payment.RecordedByUser.LastName),
            Amount = payment.Amount,
            Method = payment.Method,
            PaidAt = payment.PaidAt,
            Reference = payment.Reference,
            Notes = payment.Notes,
            CreatedAt = payment.CreatedAt
        };
    }

    private static PaymentStatus GetPaymentStatus(
        decimal serviceTotal,
        decimal amountPaid)
    {
        if (amountPaid <= 0)
        {
            return PaymentStatus.Pending;
        }

        if (amountPaid >= serviceTotal)
        {
            return PaymentStatus.Paid;
        }

        return PaymentStatus.PartiallyPaid;
    }

    private static string BuildCustomerName(
        string firstName,
        string lastName,
        string? secondLastName)
    {
        return string.Join(
            " ",
            new[]
            {
                firstName,
                lastName,
                secondLastName
            }
            .Where(value =>
                !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim()));
    }

    private static string BuildUserName(
        string firstName,
        string lastName)
    {
        return string.Join(
            " ",
            new[]
            {
                firstName,
                lastName
            }
            .Where(value =>
                !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()));
    }

    private static string? NormalizeOptional(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static DateTime NormalizeToUtc(
        DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,

            DateTimeKind.Local =>
                value.ToUniversalTime(),

            _ => DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc)
        };
    }
}
