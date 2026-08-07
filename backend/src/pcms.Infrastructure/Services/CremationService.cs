using Microsoft.EntityFrameworkCore;
using pcms.Application.Cremations.DTOs;
using pcms.Application.Cremations.Interfaces;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class CremationService : ICremationService
{
    private readonly AppDbContext _context;

    public CremationService(AppDbContext context)
    {
        _context = context;
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
        if (!Enum.IsDefined(
                typeof(CremationType),
                dto.CremationType))
        {
            throw new ArgumentException(
                "El tipo de cremación no es válido.");
        }

        var packageName = dto.PackageName.Trim();

        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new ArgumentException(
                "El nombre del paquete es obligatorio.");
        }

        if (dto.IncludesUrn &&
            string.IsNullOrWhiteSpace(dto.UrnDescription))
        {
            throw new ArgumentException(
                "Debe proporcionar una descripción de la urna.");
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

        var cremationExists = await _context.Cremations
            .AnyAsync(c =>
                c.ReceptionId == dto.ReceptionId);

        if (cremationExists)
        {
            throw new InvalidOperationException(
                "La recepción ya tiene una cremación registrada.");
        }

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
            CremationType = dto.CremationType,

            Status = dto.ScheduledAt.HasValue
                ? CremationStatus.Scheduled
                : CremationStatus.Pending,

            PackageName = packageName,

            IncludesUrn = dto.IncludesUrn,

            UrnDescription = dto.IncludesUrn
                ? dto.UrnDescription?.Trim()
                : null,

            IncludesPawPrint = dto.IncludesPawPrint,
            IncludesCertificate = dto.IncludesCertificate,
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

        await _context.SaveChangesAsync();

        return new CremationDto
        {
            Id = cremation.Id,
            ReceptionId = cremation.ReceptionId,
            QrCode = reception.QrCode,

            PetId = reception.PetId,
            PetName = reception.Pet.Name,

            CustomerId = reception.Pet.CustomerId,
            CustomerName = BuildCustomerName(
        reception.Pet.Customer.FirstName,
        reception.Pet.Customer.LastName,
        reception.Pet.Customer.SecondLastName),

            AssignedToUserId =
                cremation.AssignedToUserId,

            AssignedToUserName = assignedUser == null
                ? null
                : assignedUser.FirstName + " " +
                  assignedUser.LastName,

            CremationType = cremation.CremationType,
            Status = cremation.Status,
            PackageName = cremation.PackageName,

            IncludesUrn = cremation.IncludesUrn,
            UrnDescription = cremation.UrnDescription,
            IncludesPawPrint = cremation.IncludesPawPrint,
            IncludesCertificate =
                cremation.IncludesCertificate,

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
                PetName = c.Reception.Pet.Name,

                CustomerId =
                    c.Reception.Pet.CustomerId,

                CustomerName =
        c.Reception.Pet.Customer.SecondLastName == null
            ? c.Reception.Pet.Customer.FirstName + " " +
              c.Reception.Pet.Customer.LastName
            : c.Reception.Pet.Customer.FirstName + " " +
              c.Reception.Pet.Customer.LastName + " " +
              c.Reception.Pet.Customer.SecondLastName,

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

                IncludesUrn = c.IncludesUrn,
                UrnDescription = c.UrnDescription,

                IncludesPawPrint =
                    c.IncludesPawPrint,

                IncludesCertificate =
                    c.IncludesCertificate,

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
                PetName = c.Reception.Pet.Name,

                CustomerId =
                    c.Reception.Pet.CustomerId,

                CustomerName =
        c.Reception.Pet.Customer.SecondLastName == null
            ? c.Reception.Pet.Customer.FirstName + " " +
              c.Reception.Pet.Customer.LastName
            : c.Reception.Pet.Customer.FirstName + " " +
              c.Reception.Pet.Customer.LastName + " " +
              c.Reception.Pet.Customer.SecondLastName,

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

                IncludesUrn = c.IncludesUrn,
                UrnDescription = c.UrnDescription,

                IncludesPawPrint =
                    c.IncludesPawPrint,

                IncludesCertificate =
                    c.IncludesCertificate,

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
                PetName = c.Reception.Pet.Name,

                CustomerId =
                    c.Reception.Pet.CustomerId,

                CustomerName =
        c.Reception.Pet.Customer.SecondLastName == null
            ? c.Reception.Pet.Customer.FirstName + " " +
              c.Reception.Pet.Customer.LastName
            : c.Reception.Pet.Customer.FirstName + " " +
              c.Reception.Pet.Customer.LastName + " " +
              c.Reception.Pet.Customer.SecondLastName,

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

                IncludesUrn = c.IncludesUrn,
                UrnDescription = c.UrnDescription,

                IncludesPawPrint =
                    c.IncludesPawPrint,

                IncludesCertificate =
                    c.IncludesCertificate,

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
                PetName = c.Reception.Pet.Name,

                CustomerId =
                    c.Reception.Pet.CustomerId,

                CustomerName =
        c.Reception.Pet.Customer.SecondLastName == null
            ? c.Reception.Pet.Customer.FirstName + " " +
              c.Reception.Pet.Customer.LastName
            : c.Reception.Pet.Customer.FirstName + " " +
              c.Reception.Pet.Customer.LastName + " " +
              c.Reception.Pet.Customer.SecondLastName,

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

                IncludesUrn = c.IncludesUrn,
                UrnDescription = c.UrnDescription,

                IncludesPawPrint =
                    c.IncludesPawPrint,

                IncludesCertificate =
                    c.IncludesCertificate,

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
        if (!Enum.IsDefined(
                typeof(CremationType),
                dto.CremationType))
        {
            throw new ArgumentException(
                "El tipo de cremación no es válido.");
        }

        var packageName = dto.PackageName.Trim();

        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new ArgumentException(
                "El nombre del paquete es obligatorio.");
        }

        if (dto.IncludesUrn &&
            string.IsNullOrWhiteSpace(dto.UrnDescription))
        {
            throw new ArgumentException(
                "Debe proporcionar una descripción de la urna.");
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
            dto.AssignedToUserId;

        cremation.CremationType =
            dto.CremationType;

        cremation.PackageName =
            packageName;

        cremation.IncludesUrn =
            dto.IncludesUrn;

        cremation.UrnDescription =
            dto.IncludesUrn
                ? dto.UrnDescription?.Trim()
                : null;

        cremation.IncludesPawPrint =
            dto.IncludesPawPrint;

        cremation.IncludesCertificate =
            dto.IncludesCertificate;

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
            PetName = cremation.Reception.Pet.Name,

            CustomerId =
                cremation.Reception.Pet.CustomerId,

            CustomerName = BuildCustomerName(
        cremation.Reception.Pet.Customer.FirstName,
        cremation.Reception.Pet.Customer.LastName,
        cremation.Reception.Pet.Customer.SecondLastName),

            AssignedToUserId =
                cremation.AssignedToUserId,

            AssignedToUserName =
                assignedUser == null
                    ? null
                    : assignedUser.FirstName + " " +
                      assignedUser.LastName,

            CremationType =
                cremation.CremationType,

            Status = cremation.Status,
            PackageName = cremation.PackageName,

            IncludesUrn =
                cremation.IncludesUrn,

            UrnDescription =
                cremation.UrnDescription,

            IncludesPawPrint =
                cremation.IncludesPawPrint,

            IncludesCertificate =
                cremation.IncludesCertificate,

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
            PetName = cremation.Reception.Pet.Name,

            CustomerId =
                cremation.Reception.Pet.CustomerId,

            CustomerName = BuildCustomerName(
        cremation.Reception.Pet.Customer.FirstName,
        cremation.Reception.Pet.Customer.LastName,
        cremation.Reception.Pet.Customer.SecondLastName),

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

            IncludesUrn =
                cremation.IncludesUrn,

            UrnDescription =
                cremation.UrnDescription,

            IncludesPawPrint =
                cremation.IncludesPawPrint,

            IncludesCertificate =
                cremation.IncludesCertificate,

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

                    c.Reception.Pet.Name
                        .ToLower()
                        .Contains(normalizedSearch) ||

                    c.Reception.Pet.Customer.FirstName
                        .ToLower()
                        .Contains(normalizedSearch) ||

                    c.Reception.Pet.Customer.LastName
                        .ToLower()
                        .Contains(normalizedSearch) ||

                    (c.Reception.Pet.Customer.SecondLastName != null &&
        c.Reception.Pet.Customer.SecondLastName
            .ToLower()
            .Contains(normalizedSearch)) ||

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
                PetName = c.Reception.Pet.Name,

                CustomerId =
                    c.Reception.Pet.CustomerId,

                CustomerName =
        c.Reception.Pet.Customer.SecondLastName == null
            ? c.Reception.Pet.Customer.FirstName + " " +
              c.Reception.Pet.Customer.LastName
            : c.Reception.Pet.Customer.FirstName + " " +
              c.Reception.Pet.Customer.LastName + " " +
              c.Reception.Pet.Customer.SecondLastName,

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

                IncludesUrn = c.IncludesUrn,
                UrnDescription = c.UrnDescription,

                IncludesPawPrint =
                    c.IncludesPawPrint,

                IncludesCertificate =
                    c.IncludesCertificate,

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
                    PetName = r.Pet.Name,

                    CustomerName =
                        r.Pet.Customer.SecondLastName == null
                            ? r.Pet.Customer.FirstName + " " +
                              r.Pet.Customer.LastName
                            : r.Pet.Customer.FirstName + " " +
                              r.Pet.Customer.LastName + " " +
                              r.Pet.Customer.SecondLastName
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
}