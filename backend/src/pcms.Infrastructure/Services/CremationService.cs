using Microsoft.EntityFrameworkCore;
using pcms.Application.Cremations.DTOs;
using pcms.Application.Cremations.Interfaces;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;
using pcms.Application.CremationPricing.Interfaces;

namespace pcms.Infrastructure.Services;

public class CremationService : ICremationService
{
    private readonly AppDbContext _context;
    private readonly ICremationPricingService _cremationPricingService;

    public CremationService(
        AppDbContext context,
        ICremationPricingService cremationPricingService)
    {
        _context = context;
        _cremationPricingService = cremationPricingService;
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

    public async Task<CremationDto> CreateAsync(
    CreateCremationDto dto)
    {
        if (dto.ReceptionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Debe seleccionar una recepción válida.");
        }

        if (dto.CremationPackageId == Guid.Empty)
        {
            throw new ArgumentException(
                "Debe seleccionar un paquete o servicio válido.");
        }

        var reception = await _context.Receptions
            .AsNoTracking()
            .Include(r => r.Pet)
                .ThenInclude(p => p.Customer)
            .FirstOrDefaultAsync(r =>
                r.Id == dto.ReceptionId &&
                r.IsActive);

        if (reception == null)
        {
            throw new InvalidOperationException(
                "No se encontró una recepción activa.");
        }

        if (reception.VerifiedWeightKg <= 0)
        {
            throw new InvalidOperationException(
                "La recepción no tiene un peso verificado válido.");
        }

        var cremationExists = await _context.Cremations
            .AnyAsync(c =>
                c.ReceptionId == dto.ReceptionId);

        if (cremationExists)
        {
            throw new InvalidOperationException(
                "La recepción ya tiene una cremación registrada.");
        }

        var package = await _context.CremationPackages
    .AsNoTracking()
    .FirstOrDefaultAsync(p =>
        p.Id == dto.CremationPackageId &&
        p.IsActive);

        if (package == null)
        {
            throw new InvalidOperationException(
                "No se encontró un paquete o servicio de cremación activo.");
        }

        var cremationType =
        package.PackageType switch
        {
            CremationPackageType.AshesReturn =>
                CremationType.Individual,

            CremationPackageType.NoAshes =>
                CremationType.Communal,

            _ => throw new InvalidOperationException(
                "El tipo de paquete de cremación no es válido.")
        };

        Urn? urn = null;

        if (package.IncludesUrn)
        {
            if (!dto.UrnId.HasValue)
            {
                throw new ArgumentException(
                    "Debe seleccionar una urna para este paquete.");
            }

            urn = await _context.CremationPackageUrns
                .AsNoTracking()
                .Where(option =>
                    option.CremationPackageId == package.Id &&
                    option.UrnId == dto.UrnId.Value &&
                    option.IsActive &&
                    option.Urn.IsActive)
                .Select(option => option.Urn)
                .FirstOrDefaultAsync();

            if (urn == null)
            {
                throw new InvalidOperationException(
                    "La urna seleccionada no está permitida para este paquete o está inactiva.");
            }
        }
        else if (dto.UrnId.HasValue)
        {
            throw new ArgumentException(
                "El paquete seleccionado no incluye urna.");
        }

        var accessoryDescription =
            package.IncludesPawPrint
                ? string.IsNullOrWhiteSpace(
                    dto.AccessoryDescription)
                    ? package.AccessoryDescription
                    : dto.AccessoryDescription.Trim()
                : null;

        var quote =
    await _cremationPricingService.GetQuoteAsync(
        package.Id,
        reception.VerifiedWeightKg,
        cremationType);

        PaymentAccount? collectionPaymentAccount = null;
        var quotedPrice = quote.Price;

        if (reception.CollectionId.HasValue)
        {
            collectionPaymentAccount = await _context.PaymentAccounts
                .Include(account => account.Payments)
                .FirstOrDefaultAsync(account =>
                    account.CollectionId == reception.CollectionId.Value);

            if (collectionPaymentAccount != null &&
                collectionPaymentAccount.CremationPackageId != package.Id)
            {
                throw new InvalidOperationException(
                    "El paquete seleccionado no corresponde al servicio asociado al pago de la recolección.");
            }

            if (collectionPaymentAccount != null &&
                collectionPaymentAccount.ServiceTotal <= 0m)
            {
                throw new InvalidOperationException(
                    "La cuenta de pago no tiene un precio histórico válido.");
            }

            if (collectionPaymentAccount != null)
            {
                quotedPrice = collectionPaymentAccount.ServiceTotal;
            }

            if (collectionPaymentAccount != null &&
                collectionPaymentAccount.Payments.Sum(payment =>
                    payment.Amount) > quotedPrice)
            {
                throw new InvalidOperationException(
                    "Los pagos registrados exceden el precio cotizado para la cremación.");
            }
        }

        ValidateScheduledAt(dto.ScheduledAt);

        if (dto.ScheduledAt.HasValue &&
            dto.ScheduledAt.Value < reception.ReceivedAt)
        {
            throw new ArgumentException(
                "La fecha programada no puede ser anterior a la fecha de recepción.");
        }

        User? assignedUser = null;

        if (dto.AssignedToUserId.HasValue)
        {
            assignedUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Id == dto.AssignedToUserId.Value &&
                    u.IsActive);

            if (assignedUser == null)
            {
                throw new InvalidOperationException(
                    "No se encontró un usuario activo para asignar la cremación.");
            }
        }

        var currentTime = DateTime.UtcNow;

        var cremation = new Cremation
        {
            Id = Guid.NewGuid(),
            ReceptionId = reception.Id,
            AssignedToUserId = assignedUser?.Id,
            CremationPackageId = package.Id,
            UrnId = urn?.Id,

            CremationType = cremationType,

            Status = dto.ScheduledAt.HasValue
                ? CremationStatus.Scheduled
                : CremationStatus.Pending,

            PackageName = package.Name,

            IncludesUrn = package.IncludesUrn,

            UrnDescription = urn?.Name,

            IncludesPawPrint = package.IncludesPawPrint,
            AccessoryDescription = accessoryDescription,
            IncludesCertificate = package.IncludesCertificate,
            QuotedPrice =
    quotedPrice,

            QuotedWeightKg =
    quote.WeightKg,

            QuotedMinimumWeightKg =
    quote.MinimumWeightKg,

            QuotedMaximumWeightKg =
    quote.MaximumWeightKg,
            ScheduledAt = dto.ScheduledAt,

            SpecialInstructions =
                string.IsNullOrWhiteSpace(dto.SpecialInstructions)
                    ? null
                    : dto.SpecialInstructions.Trim(),

            Notes = string.IsNullOrWhiteSpace(dto.Notes)
                ? null
                : dto.Notes.Trim(),

            IsActive = true,
            CreatedAt = currentTime
        };

        _context.Cremations.Add(cremation);

        if (collectionPaymentAccount != null)
        {
            collectionPaymentAccount.CremationId = cremation.Id;
            collectionPaymentAccount.UpdatedAt = currentTime;
        }

        await _context.SaveChangesAsync();

        return new CremationDto
        {
            Id = cremation.Id,
            ReceptionId = cremation.ReceptionId,
            QrCode = reception.QrCode,

            PetId = reception.PetId,
            PetName = reception.PetNameSnapshot,

            CustomerId = reception.Pet.CustomerId,
            CustomerName = reception.CustomerNameSnapshot,

            AssignedToUserId =
                cremation.AssignedToUserId,

            AssignedToUserName = assignedUser == null
                ? null
                : assignedUser.FirstName + " " +
                  assignedUser.LastName,

            CremationType = cremation.CremationType,
            Status = cremation.Status,
            CremationPackageId = package.Id,
            CremationPackageName = package.Name,
            PackageName = cremation.PackageName,

            UrnId = urn?.Id,
            UrnName = urn?.Name,
            IncludesUrn = cremation.IncludesUrn,
            UrnDescription = cremation.UrnDescription,
            IncludesPawPrint = cremation.IncludesPawPrint,
            AccessoryDescription =
    cremation.AccessoryDescription,
            IncludesCertificate =
                cremation.IncludesCertificate,
            QuotedPrice =
    cremation.QuotedPrice,

            QuotedWeightKg =
    cremation.QuotedWeightKg,

            QuotedMinimumWeightKg =
    cremation.QuotedMinimumWeightKg,

            QuotedMaximumWeightKg =
    cremation.QuotedMaximumWeightKg,

            ScheduledAt = cremation.ScheduledAt,
            StartedAt = cremation.StartedAt,
            CompletedAt = cremation.CompletedAt,
            ReadyForDeliveryAt =
                cremation.ReadyForDeliveryAt,
            DeliveredAt = cremation.DeliveredAt,

            SpecialInstructions =
                cremation.SpecialInstructions,

            Notes = cremation.Notes,
            IsActive = cremation.IsActive,
            CreatedAt = cremation.CreatedAt
        };
    }

    public async Task<PagedCremationsDto> GetAllAsync(
    int page,
    int pageSize,
    bool isActive)
    {
        var query = _context.Cremations
        .AsNoTracking()
        .Where(c => c.IsActive == isActive);

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CremationDto
            {
                Id = c.Id,
                ReceptionId = c.ReceptionId,
                QrCode = c.Reception.QrCode,

                PetId = c.Reception.PetId,
                PetName = c.Reception.PetNameSnapshot,

                CustomerId =
                    c.Reception.Pet.CustomerId,

                CustomerName = c.Reception.CustomerNameSnapshot,

                AssignedToUserId =
                    c.AssignedToUserId,

                AssignedToUserName =
                    c.AssignedToUser == null
                        ? null
                        : c.AssignedToUser.FirstName + " " +
                          c.AssignedToUser.LastName,

                CremationType = c.CremationType,
                Status = c.Status,
                PackageName = c.PackageName,

                CremationPackageId =
    c.CremationPackageId,

                CremationPackageName =
    c.CremationPackage != null
        ? c.CremationPackage.Name
        : c.PackageName,

                UrnId =
    c.UrnId,

                UrnName =
    c.Urn != null
        ? c.Urn.Name
        : c.UrnDescription,

                AccessoryDescription =
    c.AccessoryDescription,

                IncludesUrn = c.IncludesUrn,
                UrnDescription = c.UrnDescription,

                IncludesPawPrint =
                    c.IncludesPawPrint,

                IncludesCertificate =
                    c.IncludesCertificate,

                QuotedPrice =
    c.QuotedPrice,

                QuotedWeightKg =
    c.QuotedWeightKg,

                QuotedMinimumWeightKg =
    c.QuotedMinimumWeightKg,

                QuotedMaximumWeightKg =
    c.QuotedMaximumWeightKg,

                ScheduledAt = c.ScheduledAt,
                StartedAt = c.StartedAt,
                CompletedAt = c.CompletedAt,

                ReadyForDeliveryAt =
                    c.ReadyForDeliveryAt,

                DeliveredAt = c.DeliveredAt,

                SpecialInstructions =
                    c.SpecialInstructions,

                Notes = c.Notes,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return new PagedCremationsDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(
                (double)totalItems / pageSize)
        };
    }

    public async Task<CremationDto?> GetByIdAsync(Guid id)
    {
        return await _context.Cremations
            .AsNoTracking()
            .Where(c =>
                c.Id == id &&
                c.IsActive)
            .Select(c => new CremationDto
            {
                Id = c.Id,
                ReceptionId = c.ReceptionId,
                QrCode = c.Reception.QrCode,

                PetId = c.Reception.PetId,
                PetName = c.Reception.PetNameSnapshot,

                CustomerId =
                    c.Reception.Pet.CustomerId,

                CustomerName = c.Reception.CustomerNameSnapshot,

                AssignedToUserId =
                    c.AssignedToUserId,

                AssignedToUserName =
                    c.AssignedToUser == null
                        ? null
                        : c.AssignedToUser.FirstName + " " +
                          c.AssignedToUser.LastName,

                CremationType = c.CremationType,
                Status = c.Status,
                PackageName = c.PackageName,

                CremationPackageId =
    c.CremationPackageId,

                CremationPackageName =
    c.CremationPackage != null
        ? c.CremationPackage.Name
        : c.PackageName,

                UrnId =
    c.UrnId,

                UrnName =
    c.Urn != null
        ? c.Urn.Name
        : c.UrnDescription,

                AccessoryDescription =
    c.AccessoryDescription,

                IncludesUrn = c.IncludesUrn,
                UrnDescription = c.UrnDescription,

                IncludesPawPrint =
                    c.IncludesPawPrint,

                IncludesCertificate =
                    c.IncludesCertificate,

                QuotedPrice = c.QuotedPrice,
                QuotedWeightKg = c.QuotedWeightKg,
                QuotedMinimumWeightKg = c.QuotedMinimumWeightKg,
                QuotedMaximumWeightKg = c.QuotedMaximumWeightKg,

                ScheduledAt = c.ScheduledAt,
                StartedAt = c.StartedAt,
                CompletedAt = c.CompletedAt,

                ReadyForDeliveryAt =
                    c.ReadyForDeliveryAt,

                DeliveredAt = c.DeliveredAt,

                SpecialInstructions =
                    c.SpecialInstructions,

                Notes = c.Notes,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<CremationDto?> GetByReceptionIdAsync(
    Guid receptionId)
    {
        return await _context.Cremations
            .AsNoTracking()
            .Where(c =>
                c.ReceptionId == receptionId &&
                c.IsActive)
            .Select(c => new CremationDto
            {
                Id = c.Id,
                ReceptionId = c.ReceptionId,
                QrCode = c.Reception.QrCode,

                PetId = c.Reception.PetId,
                PetName = c.Reception.PetNameSnapshot,

                CustomerId =
                    c.Reception.Pet.CustomerId,

                CustomerName = c.Reception.CustomerNameSnapshot,

                AssignedToUserId =
                    c.AssignedToUserId,

                AssignedToUserName =
                    c.AssignedToUser == null
                        ? null
                        : c.AssignedToUser.FirstName + " " +
                          c.AssignedToUser.LastName,

                CremationType = c.CremationType,
                Status = c.Status,
                PackageName = c.PackageName,

                CremationPackageId =
    c.CremationPackageId,

                CremationPackageName =
    c.CremationPackage != null
        ? c.CremationPackage.Name
        : c.PackageName,

                UrnId =
    c.UrnId,

                UrnName =
    c.Urn != null
        ? c.Urn.Name
        : c.UrnDescription,

                AccessoryDescription =
    c.AccessoryDescription,

                IncludesUrn = c.IncludesUrn,
                UrnDescription = c.UrnDescription,

                IncludesPawPrint =
                    c.IncludesPawPrint,

                IncludesCertificate =
                    c.IncludesCertificate,

                QuotedPrice =
    c.QuotedPrice,

                QuotedWeightKg =
    c.QuotedWeightKg,

                QuotedMinimumWeightKg =
    c.QuotedMinimumWeightKg,

                QuotedMaximumWeightKg =
    c.QuotedMaximumWeightKg,

                ScheduledAt = c.ScheduledAt,
                StartedAt = c.StartedAt,
                CompletedAt = c.CompletedAt,

                ReadyForDeliveryAt =
                    c.ReadyForDeliveryAt,

                DeliveredAt = c.DeliveredAt,

                SpecialInstructions =
                    c.SpecialInstructions,

                Notes = c.Notes,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<CremationDto>> GetByStatusAsync(
    CremationStatus status)
    {
        if (!Enum.IsDefined(
                typeof(CremationStatus),
                status))
        {
            throw new ArgumentException(
                "El estado de cremación no es válido.");
        }

        return await _context.Cremations
            .AsNoTracking()
            .Where(c =>
                c.IsActive &&
                c.Status == status)
            .OrderBy(c => c.ScheduledAt ?? c.CreatedAt)
            .Select(c => new CremationDto
            {
                Id = c.Id,
                ReceptionId = c.ReceptionId,
                QrCode = c.Reception.QrCode,

                PetId = c.Reception.PetId,
                PetName = c.Reception.PetNameSnapshot,

                CustomerId =
                    c.Reception.Pet.CustomerId,

                CustomerName = c.Reception.CustomerNameSnapshot,

                AssignedToUserId =
                    c.AssignedToUserId,

                AssignedToUserName =
                    c.AssignedToUser == null
                        ? null
                        : c.AssignedToUser.FirstName + " " +
                          c.AssignedToUser.LastName,

                CremationType = c.CremationType,
                Status = c.Status,
                PackageName = c.PackageName,

                CremationPackageId =
    c.CremationPackageId,

                CremationPackageName =
    c.CremationPackage != null
        ? c.CremationPackage.Name
        : c.PackageName,

                UrnId =
    c.UrnId,

                UrnName =
    c.Urn != null
        ? c.Urn.Name
        : c.UrnDescription,

                AccessoryDescription =
    c.AccessoryDescription,

                IncludesUrn = c.IncludesUrn,
                UrnDescription = c.UrnDescription,

                IncludesPawPrint =
                    c.IncludesPawPrint,

                IncludesCertificate =
                    c.IncludesCertificate,

                QuotedPrice = c.QuotedPrice,
                QuotedWeightKg = c.QuotedWeightKg,
                QuotedMinimumWeightKg = c.QuotedMinimumWeightKg,
                QuotedMaximumWeightKg = c.QuotedMaximumWeightKg,

                ScheduledAt = c.ScheduledAt,
                StartedAt = c.StartedAt,
                CompletedAt = c.CompletedAt,

                ReadyForDeliveryAt =
                    c.ReadyForDeliveryAt,

                DeliveredAt = c.DeliveredAt,

                SpecialInstructions =
                    c.SpecialInstructions,

                Notes = c.Notes,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CremationDto?> UpdateAsync(
    Guid id,
    UpdateCremationDto dto)
    {
        if (dto.CremationPackageId == Guid.Empty)
        {
            throw new ArgumentException(
                "Debe seleccionar un paquete o servicio válido.");
        }

        var cremation = await _context.Cremations
            .Include(c => c.Reception)
                .ThenInclude(r => r.Pet)
                    .ThenInclude(p => p.Customer)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.IsActive);

        if (cremation == null)
        {
            return null;
        }

        if (cremation.Reception.VerifiedWeightKg <= 0)
        {
            throw new InvalidOperationException(
                "La recepción no tiene un peso verificado válido.");
        }

        var packageChanged =
    cremation.CremationPackageId !=
    dto.CremationPackageId;

        var urnChanged =
            cremation.UrnId != dto.UrnId;

        if (urnChanged && await _context.CremationUrnReservations.AnyAsync(x => x.CremationId == id && x.Status == pcms.Domain.Enums.UrnReservationStatus.Active))
        {
            throw new InvalidOperationException("Debe liberar la reserva activa antes de cambiar la urna.");
        }

        if (urnChanged && await _context.CremationUrnReservations.AnyAsync(x => x.CremationId == id && x.Status == pcms.Domain.Enums.UrnReservationStatus.Fulfilled))
        {
            throw new InvalidOperationException("No se puede cambiar la urna después de registrar su entrega.");
        }

        var canInitializeLegacyQuote =
            cremation.CremationPackageId is null &&
            cremation.QuotedPrice is null &&
            !await _context.PaymentAccounts
                .AsNoTracking()
                .AnyAsync(account =>
                    account.CremationId == cremation.Id);

        if (cremation.Status is
            CremationStatus.InProgress or
            CremationStatus.Cooling or
            CremationStatus.ProcessingRemains or
            CremationStatus.Completed or
            CremationStatus.ReadyForDelivery or
            CremationStatus.Delivered)
        {
            if (cremation.CremationPackageId !=
                dto.CremationPackageId ||
                cremation.UrnId != dto.UrnId)
            {
                if (!canInitializeLegacyQuote)
                {
                    throw new InvalidOperationException(
                        "No se puede cambiar el paquete o la urna después de iniciar la cremación.");
                }
            }
        }

        var package = await _context.CremationPackages
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Id == dto.CremationPackageId);

        if (package == null)
        {
            throw new InvalidOperationException(
                "No se encontró el paquete o servicio de cremación.");
        }

        if (packageChanged && !package.IsActive)
        {
            throw new InvalidOperationException(
                "No se puede seleccionar un paquete o servicio de cremación inactivo.");
        }

        var cremationType = packageChanged
            ? package.PackageType switch
            {
                CremationPackageType.AshesReturn =>
                    CremationType.Individual,

                CremationPackageType.NoAshes =>
                    CremationType.Communal,

                _ => throw new InvalidOperationException(
                    "El tipo de paquete de cremación no es válido.")
            }
            : cremation.CremationType;

        var includesUrn = packageChanged
            ? package.IncludesUrn
            : cremation.IncludesUrn;

        Urn? urn = null;

        if (includesUrn)
        {
            if (!dto.UrnId.HasValue)
            {
                throw new ArgumentException(
                    "Debe seleccionar una urna para este paquete.");
            }

            if (packageChanged || urnChanged)
            {
                urn = await _context.CremationPackageUrns
                    .AsNoTracking()
                    .Where(option =>
                        option.CremationPackageId == package.Id &&
                        option.UrnId == dto.UrnId.Value &&
                        option.IsActive &&
                        option.Urn.IsActive)
                    .Select(option => option.Urn)
                    .FirstOrDefaultAsync();
            }
            else
            {
                urn = await _context.Urns
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u =>
                        u.Id == dto.UrnId.Value);
            }

            if (urn == null)
            {
                throw new InvalidOperationException(
                    packageChanged || urnChanged
                        ? "La urna seleccionada no está permitida para este paquete o está inactiva."
                        : "No se encontró la urna asociada a la cremación.");
            }
        }
        else if (dto.UrnId.HasValue)
        {
            throw new ArgumentException(
                "El paquete seleccionado no incluye urna.");
        }

        var accessoryDescription =
            cremation.AccessoryDescription;

        if (packageChanged)
        {
            accessoryDescription =
                package.IncludesPawPrint
                    ? string.IsNullOrWhiteSpace(
                        dto.AccessoryDescription)
                        ? package.AccessoryDescription
                        : dto.AccessoryDescription.Trim()
                    : null;
        }
        else if (cremation.IncludesPawPrint &&
                 !string.IsNullOrWhiteSpace(
                     dto.AccessoryDescription))
        {
            accessoryDescription =
                dto.AccessoryDescription.Trim();
        }

        if (packageChanged)
        {
            var updatedQuote =
                await _cremationPricingService.GetQuoteAsync(
                    package.Id,
                    cremation.Reception.VerifiedWeightKg,
                    cremationType);

            var paymentAccount =
                await _context.PaymentAccounts
                    .Include(account => account.Payments)
                    .FirstOrDefaultAsync(account =>
                        account.CremationId ==
                        cremation.Id);

            if (paymentAccount is not null)
            {
                var amountPaid =
                    paymentAccount.Payments.Sum(
                        payment => payment.Amount);

                if (updatedQuote.Price < amountPaid)
                {
                    throw new InvalidOperationException(
                        $"El nuevo precio cotizado ({updatedQuote.Price:C2}) " +
                        $"no puede ser menor que el monto ya pagado ({amountPaid:C2}).");
                }

                paymentAccount.ServiceTotal =
                    updatedQuote.Price;

                paymentAccount.UpdatedAt =
                    DateTime.UtcNow;
            }

            cremation.QuotedPrice =
                updatedQuote.Price;

            cremation.QuotedWeightKg =
                updatedQuote.WeightKg;

            cremation.QuotedMinimumWeightKg =
                updatedQuote.MinimumWeightKg;

            cremation.QuotedMaximumWeightKg =
                updatedQuote.MaximumWeightKg;
        }

        ValidateScheduledAt(dto.ScheduledAt);

        if (dto.ScheduledAt.HasValue &&
            dto.ScheduledAt.Value <
            cremation.Reception.ReceivedAt)
        {
            throw new ArgumentException(
                "La fecha programada no puede ser anterior a la fecha de recepción.");
        }

        User? assignedUser = null;

        if (dto.AssignedToUserId.HasValue)
        {
            assignedUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Id == dto.AssignedToUserId.Value &&
                    u.IsActive);

            if (assignedUser == null)
            {
                throw new InvalidOperationException(
                    "No se encontró un usuario activo para asignar la cremación.");
            }
        }

        cremation.AssignedToUserId =
    assignedUser?.Id;

        cremation.CremationPackageId =
            package.Id;

        cremation.UrnId =
            urn?.Id;

        if (packageChanged)
        {
            cremation.CremationType =
                cremationType;

            cremation.PackageName =
                package.Name;

            cremation.IncludesUrn =
                package.IncludesUrn;

            cremation.IncludesPawPrint =
                package.IncludesPawPrint;

            cremation.IncludesCertificate =
                package.IncludesCertificate;
        }

        cremation.AccessoryDescription =
            accessoryDescription;

        if (packageChanged || urnChanged)
        {
            cremation.UrnDescription =
                urn?.Name;
        }

        cremation.ScheduledAt =
            dto.ScheduledAt;

        cremation.SpecialInstructions =
            string.IsNullOrWhiteSpace(
                dto.SpecialInstructions)
                ? null
                : dto.SpecialInstructions.Trim();

        cremation.Notes =
            string.IsNullOrWhiteSpace(dto.Notes)
                ? null
                : dto.Notes.Trim();

        // Only Pending and Scheduled records change status
        // automatically when their schedule is edited.
        if (cremation.Status == CremationStatus.Pending ||
            cremation.Status == CremationStatus.Scheduled)
        {
            cremation.Status =
                dto.ScheduledAt.HasValue
                    ? CremationStatus.Scheduled
                    : CremationStatus.Pending;
        }

        await _context.SaveChangesAsync();

        return new CremationDto
        {
            Id = cremation.Id,
            ReceptionId = cremation.ReceptionId,
            QrCode = cremation.Reception.QrCode,

            PetId = cremation.Reception.PetId,
            PetName = cremation.Reception.PetNameSnapshot,

            CustomerId =
                cremation.Reception.Pet.CustomerId,

            CustomerName = cremation.Reception.CustomerNameSnapshot,

            AssignedToUserId =
                cremation.AssignedToUserId,

            AssignedToUserName =
                assignedUser == null
                    ? null
                    : assignedUser.FirstName + " " +
                      assignedUser.LastName,

            CremationType = cremation.CremationType,
            Status = cremation.Status,

            PackageName = cremation.PackageName,

            CremationPackageId = package.Id,
            CremationPackageName = package.Name,

            UrnId = urn?.Id,
            UrnName = urn?.Name,

            IncludesUrn = cremation.IncludesUrn,
            UrnDescription = cremation.UrnDescription,

            IncludesPawPrint = cremation.IncludesPawPrint,

            AccessoryDescription =
    cremation.AccessoryDescription,

            IncludesCertificate =
    cremation.IncludesCertificate,

            QuotedPrice =
    cremation.QuotedPrice,

            QuotedWeightKg =
    cremation.QuotedWeightKg,

            QuotedMinimumWeightKg =
    cremation.QuotedMinimumWeightKg,

            QuotedMaximumWeightKg =
    cremation.QuotedMaximumWeightKg,

            ScheduledAt =
                cremation.ScheduledAt,

            StartedAt =
                cremation.StartedAt,

            CompletedAt =
                cremation.CompletedAt,

            ReadyForDeliveryAt =
                cremation.ReadyForDeliveryAt,

            DeliveredAt =
                cremation.DeliveredAt,

            SpecialInstructions =
                cremation.SpecialInstructions,

            Notes = cremation.Notes,
            IsActive = cremation.IsActive,
            CreatedAt = cremation.CreatedAt
        };
    }

    public async Task<CremationDto?> ChangeStatusAsync(
    Guid id,
    ChangeCremationStatusDto dto)
    {
        if (!Enum.IsDefined(
                typeof(CremationStatus),
                dto.Status))
        {
            throw new ArgumentException(
                "El estado de cremación no es válido.");
        }

        var cremation = await _context.Cremations
            .Include(c => c.Reception)
                .ThenInclude(r => r.Pet)
                    .ThenInclude(p => p.Customer)
            .Include(c => c.AssignedToUser)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.IsActive);

        if (cremation == null)
        {
            return null;
        }

        if (cremation.Status == dto.Status)
        {
            throw new InvalidOperationException(
                "La cremación ya tiene el estado seleccionado.");
        }

        var validTransition = cremation.Status switch
        {
            CremationStatus.Pending =>
                dto.Status is CremationStatus.Scheduled
                    or CremationStatus.Cancelled,

            CremationStatus.Scheduled =>
                dto.Status is CremationStatus.InProgress
                    or CremationStatus.Cancelled,

            CremationStatus.InProgress =>
                dto.Status == CremationStatus.Cooling,

            CremationStatus.Cooling =>
                dto.Status ==
                    CremationStatus.ProcessingRemains,

            CremationStatus.ProcessingRemains =>
                dto.Status == CremationStatus.Completed,

            CremationStatus.Completed =>
                dto.Status ==
                    CremationStatus.ReadyForDelivery,

            CremationStatus.ReadyForDelivery =>
                dto.Status == CremationStatus.Delivered,

            CremationStatus.Delivered => false,

            CremationStatus.Cancelled => false,

            _ => false
        };

        if (!validTransition)
        {
            throw new InvalidOperationException(
                $"No se permite cambiar el estado de " +
                $"{cremation.Status} a {dto.Status}.");
        }

        if (dto.Status == CremationStatus.Scheduled &&
            !cremation.ScheduledAt.HasValue)
        {
            throw new InvalidOperationException(
                "Debe establecer una fecha programada antes de marcar la cremación como programada.");
        }

        if (dto.Status == CremationStatus.InProgress &&
            !cremation.AssignedToUserId.HasValue)
        {
            throw new InvalidOperationException(
                "Debe asignar un usuario antes de iniciar la cremación.");
        }

        var currentTime = DateTime.UtcNow;

        cremation.Status = dto.Status;

        switch (dto.Status)
        {
            case CremationStatus.InProgress:
                cremation.StartedAt ??= currentTime;
                break;

            case CremationStatus.Completed:
                cremation.CompletedAt ??= currentTime;
                break;

            case CremationStatus.ReadyForDelivery:
                cremation.ReadyForDeliveryAt ??= currentTime;
                break;

            case CremationStatus.Delivered:
                cremation.DeliveredAt ??= currentTime;
                break;
        }

        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            cremation.Notes = dto.Notes.Trim();
        }

        await _context.SaveChangesAsync();

        return new CremationDto
        {
            Id = cremation.Id,
            ReceptionId = cremation.ReceptionId,
            QrCode = cremation.Reception.QrCode,

            PetId = cremation.Reception.PetId,
            PetName = cremation.Reception.PetNameSnapshot,

            CustomerId =
                cremation.Reception.Pet.CustomerId,

            CustomerName = cremation.Reception.CustomerNameSnapshot,

            AssignedToUserId =
                cremation.AssignedToUserId,

            AssignedToUserName =
                cremation.AssignedToUser == null
                    ? null
                    : cremation.AssignedToUser.FirstName + " " +
                      cremation.AssignedToUser.LastName,

            CremationType =
                cremation.CremationType,

            Status = cremation.Status,
            PackageName = cremation.PackageName,

            CremationPackageId =
    cremation.CremationPackageId,

            CremationPackageName =
    cremation.CremationPackage != null
        ? cremation.CremationPackage.Name
        : cremation.PackageName,

            UrnId =
    cremation.UrnId,

            UrnName =
    cremation.Urn != null
        ? cremation.Urn.Name
        : cremation.UrnDescription,

            AccessoryDescription =
    cremation.AccessoryDescription,

            IncludesUrn =
                cremation.IncludesUrn,

            UrnDescription =
                cremation.UrnDescription,

            IncludesPawPrint =
                cremation.IncludesPawPrint,

            IncludesCertificate =
                cremation.IncludesCertificate,

            QuotedPrice =
    cremation.QuotedPrice,

            QuotedWeightKg =
    cremation.QuotedWeightKg,

            QuotedMinimumWeightKg =
    cremation.QuotedMinimumWeightKg,

            QuotedMaximumWeightKg =
    cremation.QuotedMaximumWeightKg,

            ScheduledAt =
                cremation.ScheduledAt,

            StartedAt =
                cremation.StartedAt,

            CompletedAt =
                cremation.CompletedAt,

            ReadyForDeliveryAt =
                cremation.ReadyForDeliveryAt,

            DeliveredAt =
                cremation.DeliveredAt,

            SpecialInstructions =
                cremation.SpecialInstructions,

            Notes = cremation.Notes,
            IsActive = cremation.IsActive,
            CreatedAt = cremation.CreatedAt
        };
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var cremation = await _context.Cremations
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.IsActive);

        if (cremation == null)
        {
            return false;
        }

        cremation.IsActive = false;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RestoreAsync(Guid id)
    {
        var cremation = await _context.Cremations
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                !c.IsActive);

        if (cremation == null)
        {
            return false;
        }

        var receptionIsActive = await _context.Receptions
            .AnyAsync(r =>
                r.Id == cremation.ReceptionId &&
                r.IsActive);

        if (!receptionIsActive)
        {
            throw new InvalidOperationException(
                "No se puede restaurar la cremación porque su recepción está inactiva.");
        }

        cremation.IsActive = true;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<CremationDto>> SearchAsync(
    string search,
    bool isActive)
    {
        var normalizedSearch = search.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return Array.Empty<CremationDto>();
        }

        return await _context.Cremations
            .AsNoTracking()
            .Where(c =>
        c.IsActive == isActive &&
                (
                    c.Reception.QrCode
                        .ToLower()
                        .Contains(normalizedSearch) ||

                    c.Reception.PetNameSnapshot
                        .ToLower()
                        .Contains(normalizedSearch) ||

                    c.Reception.CustomerNameSnapshot
                        .ToLower()
                        .Contains(normalizedSearch) ||

                    c.PackageName
                        .ToLower()
                        .Contains(normalizedSearch) ||

                    (c.UrnDescription != null &&
                        c.UrnDescription
                            .ToLower()
                            .Contains(normalizedSearch)) ||

                    (c.AssignedToUser != null &&
                        (
                            c.AssignedToUser.FirstName
                                .ToLower()
                                .Contains(normalizedSearch) ||

                            c.AssignedToUser.LastName
                                .ToLower()
                                .Contains(normalizedSearch)
                        ))
                ))
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CremationDto
            {
                Id = c.Id,
                ReceptionId = c.ReceptionId,
                QrCode = c.Reception.QrCode,

                PetId = c.Reception.PetId,
                PetName = c.Reception.PetNameSnapshot,

                CustomerId =
                    c.Reception.Pet.CustomerId,

                CustomerName = c.Reception.CustomerNameSnapshot,

                AssignedToUserId =
                    c.AssignedToUserId,

                AssignedToUserName =
                    c.AssignedToUser == null
                        ? null
                        : c.AssignedToUser.FirstName + " " +
                          c.AssignedToUser.LastName,

                CremationType = c.CremationType,
                Status = c.Status,
                PackageName = c.PackageName,

                CremationPackageId =
    c.CremationPackageId,

                CremationPackageName =
    c.CremationPackage != null
        ? c.CremationPackage.Name
        : c.PackageName,

                UrnId =
    c.UrnId,

                UrnName =
    c.Urn != null
        ? c.Urn.Name
        : c.UrnDescription,

                AccessoryDescription =
    c.AccessoryDescription,

                IncludesUrn = c.IncludesUrn,
                UrnDescription = c.UrnDescription,

                IncludesPawPrint =
                    c.IncludesPawPrint,

                IncludesCertificate =
                    c.IncludesCertificate,

                QuotedPrice =
    c.QuotedPrice,

                QuotedWeightKg =
    c.QuotedWeightKg,

                QuotedMinimumWeightKg =
    c.QuotedMinimumWeightKg,

                QuotedMaximumWeightKg =
    c.QuotedMaximumWeightKg,

                ScheduledAt = c.ScheduledAt,
                StartedAt = c.StartedAt,
                CompletedAt = c.CompletedAt,

                ReadyForDeliveryAt =
                    c.ReadyForDeliveryAt,

                DeliveredAt = c.DeliveredAt,

                SpecialInstructions =
                    c.SpecialInstructions,

                Notes = c.Notes,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<
    IEnumerable<CremationReceptionOptionDto>>
    GetAvailableReceptionOptionsAsync()
    {
        return await _context.Receptions
            .AsNoTracking()
            .Where(r =>
                r.IsActive &&
                !_context.Cremations.Any(c =>
                    c.ReceptionId == r.Id))
            .OrderByDescending(r => r.ReceivedAt)
            .Select(r =>
                new CremationReceptionOptionDto
                {
                    Id = r.Id,
                    QrCode = r.QrCode,
                    PetName = r.PetNameSnapshot,

                    CustomerName = r.CustomerNameSnapshot
                })
            .ToListAsync();
    }

    public async Task<IEnumerable<CremationUserOptionDto>>
    GetActiveUserOptionsAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u =>
                new CremationUserOptionDto
                {
                    Id = u.Id,
                    Name = u.FirstName + " " + u.LastName
                })
            .ToListAsync();
    }

    private static void ValidateScheduledAt(
    DateTime? scheduledAt)
    {
        if (!scheduledAt.HasValue)
        {
            return;
        }

        var value = scheduledAt.Value;

        if ((value.Minute != 0 &&
             value.Minute != 30) ||
            value.Second != 0 ||
            value.Millisecond != 0)
        {
            throw new ArgumentException(
                "La hora programada debe estar en intervalos de 30 minutos.");
        }
    }
}
