using System.Data;
using Microsoft.EntityFrameworkCore;
using pcms.Application.Payments.DTOs;
using pcms.Application.Payments.Interfaces;
using pcms.Application.CremationPricing.Interfaces;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly ICremationPricingService _cremationPricingService;

    public PaymentService(
        AppDbContext context,
        ICremationPricingService cremationPricingService)
    {
        _context = context;
        _cremationPricingService = cremationPricingService;
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
        .Include(c => c.CremationPackage)
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

            CremationPackageId =
                cremation.CremationPackageId,

            PackageName =
                cremation.PackageName,

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

    public async Task<PaymentAccountDto>
        CreateCollectionAccountAsync(
            CreateCollectionPaymentAccountDto dto)
    {
        if (dto.CollectionId == Guid.Empty ||
            dto.CremationPriceId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Debe seleccionar una recolección y un precio de servicio válidos.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var collection = await _context.Collections
            .Include(current => current.Pet)
                .ThenInclude(pet => pet.Customer)
            .Include(current => current.Reception)
            .FirstOrDefaultAsync(current =>
                current.Id == dto.CollectionId &&
                current.IsActive);

        if (collection == null)
        {
            throw new InvalidOperationException(
                "No se encontró una recolección activa.");
        }

        if (collection.Status != CollectionStatus.Collected ||
            !collection.CollectedByUserId.HasValue ||
            !collection.CollectedAt.HasValue ||
            collection.Reception != null)
        {
            throw new InvalidOperationException(
                "La cuenta de pago solo puede prepararse para una recolección con custodia confirmada y sin recepción.");
        }

        if (await _context.PaymentAccounts.AnyAsync(account =>
                account.CollectionId == collection.Id))
        {
            throw new InvalidOperationException(
                "La recolección ya tiene una cuenta de pago.");
        }

        var selectedPrice = await _context.CremationPrices
            .AsNoTracking()
            .Include(price => price.CremationPackage)
            .FirstOrDefaultAsync(price =>
                price.Id == dto.CremationPriceId &&
                price.IsActive &&
                price.CremationPackage.IsActive);

        if (selectedPrice == null)
        {
            throw new InvalidOperationException(
                "El precio de servicio seleccionado no existe o está inactivo.");
        }

        var expectedCremationType =
            selectedPrice.CremationPackage.PackageType ==
                CremationPackageType.AshesReturn
                ? CremationType.Individual
                : CremationType.Communal;

        if (selectedPrice.CremationType != expectedCremationType)
        {
            throw new InvalidOperationException(
                "El precio seleccionado no corresponde al tipo de cremación del paquete.");
        }

        var weightKg =
            collection.ApproximateWeightKg ??
            collection.Pet.WeightKg;

        var quote = await _cremationPricingService.GetQuoteAsync(
            selectedPrice.CremationPackageId,
            weightKg,
            selectedPrice.CremationType);

        if (quote.MinimumWeightKg != selectedPrice.MinimumWeightKg ||
            quote.MaximumWeightKg != selectedPrice.MaximumWeightKg ||
            quote.Price != selectedPrice.Price)
        {
            throw new InvalidOperationException(
                "El precio seleccionado no corresponde al peso de la recolección.");
        }

        if (quote.RequiredCollectionPaymentAmount is not decimal required ||
            required <= 0m)
        {
            throw new InvalidOperationException(
                "El precio del servicio no tiene configurado el pago requerido para recolección.");
        }

        var account = new PaymentAccount
        {
            Id = Guid.NewGuid(),
            CollectionId = collection.Id,
            CremationPriceId = selectedPrice.Id,
            CremationPackageId = selectedPrice.CremationPackageId,
            PackageName = selectedPrice.CremationPackage.Name,
            ServiceTotal = quote.Price,
            RequiredCollectionPaymentAmount = required,
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentAccounts.Add(account);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

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
                    PetName = c.Reception.PetNameSnapshot,
                    CustomerName = c.Reception.CustomerNameSnapshot,
                    PackageName = c.PackageName
                })
            .ToListAsync();
    }

    public async Task<IEnumerable<PaymentCollectionOptionDto>>
        GetAvailableCollectionOptionsAsync()
    {
        return await (
            from collection in _context.Collections.AsNoTracking()
            let weightKg = collection.ApproximateWeightKg ??
                collection.Pet.WeightKg
            from price in _context.CremationPrices.AsNoTracking()
            where collection.IsActive &&
                collection.Status == CollectionStatus.Collected &&
                collection.Reception == null &&
                !_context.PaymentAccounts.Any(account =>
                    account.CollectionId == collection.Id) &&
                price.IsActive &&
                price.CremationPackage.IsActive &&
                price.RequiredCollectionPaymentAmount.HasValue &&
                price.RequiredCollectionPaymentAmount.Value > 0m &&
                price.RequiredCollectionPaymentAmount.Value <= price.Price &&
                ((price.CremationPackage.PackageType ==
                        CremationPackageType.AshesReturn &&
                    price.CremationType == CremationType.Individual) ||
                 (price.CremationPackage.PackageType ==
                        CremationPackageType.NoAshes &&
                    price.CremationType == CremationType.Communal)) &&
                weightKg >= price.MinimumWeightKg &&
                weightKg <= price.MaximumWeightKg
            orderby collection.CreatedAt descending,
                price.CremationPackage.DisplayOrder,
                price.CremationPackage.Name
            select new PaymentCollectionOptionDto
            {
                CollectionId = collection.Id,
                CremationPriceId = price.Id,
                QrCode = collection.QrCode,
                PetName = collection.Pet.Name,
                CustomerName =
                    collection.Pet.Customer.FirstName + " " +
                    collection.Pet.Customer.LastName,
                PackageName = price.CremationPackage.Name,
                WeightKg = weightKg,
                ServiceTotal = price.Price,
                RequiredCollectionPaymentAmount =
                    price.RequiredCollectionPaymentAmount ?? 0m
            }).ToListAsync();
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
            .Include(pa => pa.Collection)
            .Include(pa => pa.Payments)
            .FirstOrDefaultAsync(pa =>
                pa.Id == paymentAccountId);

        if (account is null)
        {
            throw new KeyNotFoundException(
                "No se encontró la cuenta de pago.");
        }

        var serviceIsActive =
            account.Cremation?.IsActive == true ||
            (account.Collection?.IsActive == true &&
             account.Collection.Status != CollectionStatus.Cancelled);

        if (!serviceIsActive)
        {
            throw new InvalidOperationException(
                "No se pueden registrar pagos para un servicio inactivo o cancelado.");
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
                (pa.Cremation != null &&
                 (EF.Functions.ILike(
                      pa.Cremation.Reception.QrCode,
                      pattern) ||
                  EF.Functions.ILike(
                      pa.Cremation.Reception.PetNameSnapshot,
                      pattern) ||
                  EF.Functions.ILike(
                      pa.Cremation.Reception.CustomerNameSnapshot,
                      pattern))) ||
                (pa.Collection != null &&
                 (EF.Functions.ILike(
                      pa.Collection.QrCode,
                      pattern) ||
                  EF.Functions.ILike(
                      pa.Collection.Pet.Name,
                      pattern) ||
                  EF.Functions.ILike(
                      pa.Collection.Pet.Customer.FirstName + " " +
                      pa.Collection.Pet.Customer.LastName,
                      pattern))) ||
                (pa.PackageName != null &&
                 EF.Functions.ILike(pa.PackageName, pattern)));

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
                .ThenInclude(c => c!.Reception)
                    .ThenInclude(r => r.Pet)
                        .ThenInclude(p => p.Customer)
            .Include(pa => pa.Collection)
                .ThenInclude(c => c!.Pet)
                    .ThenInclude(p => p.Customer)
            .Include(pa => pa.Collection)
                .ThenInclude(c => c!.Reception)
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
        var collection = account.Collection;
        var reception = cremation?.Reception ?? collection?.Reception;
        var pet = reception?.Pet ?? collection?.Pet
            ?? throw new InvalidOperationException(
                "La cuenta de pago no tiene un contexto de servicio válido.");

        var amountPaid = account.Payments.Sum(
            payment => payment.Amount);

        var balance = Math.Max(
            0m,
            account.ServiceTotal - amountPaid);

        return new PaymentAccountDto
        {
            Id = account.Id,
            CremationId = account.CremationId,
            CollectionId = account.CollectionId,
            ReceptionId = reception?.Id,
            QrCode = reception?.QrCode ?? collection!.QrCode,
            PetId = pet.Id,
            PetName = reception?.PetNameSnapshot ?? pet.Name,
            CustomerId = pet.CustomerId,
            CustomerName = reception?.CustomerNameSnapshot ??
                BuildUserName(
                    pet.Customer.FirstName,
                    pet.Customer.LastName),
            PackageName = account.PackageName ??
                cremation?.PackageName ??
                string.Empty,
            IsCremationActive = cremation?.IsActive ??
                collection?.IsActive == true,
            ServiceTotal = account.ServiceTotal,
            RequiredCollectionPaymentAmount =
                account.RequiredCollectionPaymentAmount,
            IsCollectionPaymentSatisfied =
                !account.RequiredCollectionPaymentAmount.HasValue ||
                amountPaid >=
                    account.RequiredCollectionPaymentAmount.Value,
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
