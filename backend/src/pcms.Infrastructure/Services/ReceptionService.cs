using Microsoft.EntityFrameworkCore;
using pcms.Application.Receptions.DTOs;
using pcms.Application.Receptions.Interfaces;
using pcms.Domain.Entities;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class ReceptionService : IReceptionService
{
    private readonly AppDbContext _context;

    public ReceptionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ReceptionDto> CreateAsync(
    CreateReceptionDto dto,
    Guid receivedByUserId)
{
    if (dto.PetId == Guid.Empty)
    {
        throw new ArgumentException(
            "Debe seleccionar una mascota válida.");
    }

    if (receivedByUserId == Guid.Empty)
    {
        throw new ArgumentException(
            "No se pudo identificar al usuario que realiza la recepción.");
    }

    if (dto.VerifiedWeightKg <= 0)
    {
        throw new ArgumentException(
            "El peso verificado debe ser mayor que cero.");
    }

    if (dto.HasPersonalBelongings &&
        string.IsNullOrWhiteSpace(
            dto.PersonalBelongingsDescription))
    {
        throw new ArgumentException(
            "Debe describir los objetos personales recibidos.");
    }

    var pet = await _context.Pets
        .Include(p => p.Customer)
        .Include(p => p.Reception)
        .FirstOrDefaultAsync(p =>
            p.Id == dto.PetId &&
            p.IsActive);

    if (pet == null)
    {
        throw new ArgumentException(
            "La mascota no existe o está inactiva.");
    }

    if (pet.Reception != null)
    {
        throw new InvalidOperationException(
            "La mascota ya tiene una recepción registrada.");
    }

    var receivedByUser = await _context.Users
        .FirstOrDefaultAsync(u =>
            u.Id == receivedByUserId &&
            u.IsActive);

    if (receivedByUser == null)
    {
        throw new ArgumentException(
            "El usuario que realiza la recepción no existe o está inactivo.");
    }

    string qrCode;

    do
    {
        qrCode =
            $"REC-{Guid.NewGuid():N}"
                .ToUpperInvariant();
    }
    while (await _context.Receptions
        .AnyAsync(r => r.QrCode == qrCode));

    if (dto.ReferringVeterinarianId.HasValue &&
    !dto.VeterinaryClinicId.HasValue)
{
    throw new ArgumentException(
        "A veterinary clinic must be selected when a referring veterinarian is provided.");
}

if (!string.IsNullOrWhiteSpace(dto.ReferralNotes) &&
    !dto.VeterinaryClinicId.HasValue)
{
    throw new ArgumentException(
        "A veterinary clinic must be selected when referral notes are provided.");
}

VeterinaryClinic? veterinaryClinic = null;
Veterinarian? referringVeterinarian = null;

if (dto.VeterinaryClinicId.HasValue)
{
    veterinaryClinic = await _context.VeterinaryClinics
        .FirstOrDefaultAsync(v =>
            v.Id == dto.VeterinaryClinicId.Value &&
            v.IsActive);

    if (veterinaryClinic == null)
    {
        throw new InvalidOperationException(
            "Active veterinary clinic not found.");
    }
}

if (dto.ReferringVeterinarianId.HasValue)
{
    referringVeterinarian = await _context.Veterinarians
        .FirstOrDefaultAsync(v =>
            v.Id == dto.ReferringVeterinarianId.Value &&
            v.IsActive);

    if (referringVeterinarian == null)
    {
        throw new InvalidOperationException(
            "Active referring veterinarian not found.");
    }

    if (referringVeterinarian.VeterinaryClinicId !=
        dto.VeterinaryClinicId)
    {
        throw new InvalidOperationException(
            "The referring veterinarian does not belong to the selected veterinary clinic.");
    }
}

    var currentTime = DateTime.UtcNow;

    var reception = new Reception
    {
        Id = Guid.NewGuid(),
        PetId = pet.Id,
        ReceivedByUserId = receivedByUser.Id,
        VeterinaryClinicId = dto.VeterinaryClinicId,
        ReferringVeterinarianId = dto.ReferringVeterinarianId,
        ReferralNotes = string.IsNullOrWhiteSpace(dto.ReferralNotes)
            ? null
            : dto.ReferralNotes.Trim(),
        ReceivedAt = currentTime,
        QrCode = qrCode,
        VerifiedWeightKg = dto.VerifiedWeightKg,
        HasPersonalBelongings =
            dto.HasPersonalBelongings,
        PersonalBelongingsDescription =
            dto.HasPersonalBelongings
                ? dto.PersonalBelongingsDescription?.Trim()
                : null,
        Notes = string.IsNullOrWhiteSpace(dto.Notes)
            ? null
            : dto.Notes.Trim(),
        IsActive = true,
        CreatedAt = currentTime
    };

    _context.Receptions.Add(reception);

    await _context.SaveChangesAsync();

    return new ReceptionDto
    {
        Id = reception.Id,
        PetId = reception.PetId,
        PetName = pet.Name,
        CustomerId = pet.CustomerId,
        CustomerName =
            pet.Customer.FirstName + " " +
            pet.Customer.LastName,
        ReceivedByUserId =
            reception.ReceivedByUserId,
        ReceivedByUserName =
            receivedByUser.FirstName + " " +
            receivedByUser.LastName,
        VeterinaryClinicId = reception.VeterinaryClinicId,

        VeterinaryClinicName = veterinaryClinic?.Name,

        ReferringVeterinarianId =
            reception.ReferringVeterinarianId,

        ReferringVeterinarianName =
            referringVeterinarian == null
            ? null
            : referringVeterinarian.FirstName + " " +
            referringVeterinarian.LastName,

        ReceivedAt = reception.ReceivedAt,
        QrCode = reception.QrCode,
        VerifiedWeightKg =
            reception.VerifiedWeightKg,
        HasPersonalBelongings =
            reception.HasPersonalBelongings,
        PersonalBelongingsDescription =
            reception.PersonalBelongingsDescription,
        ReferralNotes = reception.ReferralNotes,
        Notes = reception.Notes,
        IsActive = reception.IsActive,
        CreatedAt = reception.CreatedAt
    };
}

    public async Task<PagedReceptionsDto> GetAllAsync(
    int page,
    int pageSize)
{
    if (page < 1)
    {
        throw new ArgumentOutOfRangeException(
            nameof(page),
            "La página debe ser mayor que cero.");
    }

    if (pageSize < 1 || pageSize > 100)
    {
        throw new ArgumentOutOfRangeException(
            nameof(pageSize),
            "El tamaño de página debe estar entre 1 y 100.");
    }

    var query = _context.Receptions
        .AsNoTracking()
        .Where(r => r.IsActive);

    var totalItems = await query.CountAsync();

    var items = await query
        .OrderByDescending(r => r.ReceivedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(r => new ReceptionDto
        {
            Id = r.Id,

            PetId = r.PetId,
            PetName = r.Pet.Name,

            CustomerId = r.Pet.CustomerId,
            CustomerName =
                r.Pet.Customer.FirstName + " " +
                r.Pet.Customer.LastName,

            ReceivedByUserId =
                r.ReceivedByUserId,
            ReceivedByUserName =
                r.ReceivedByUser.FirstName + " " +
                r.ReceivedByUser.LastName,

            VeterinaryClinicId = r.VeterinaryClinicId,

            VeterinaryClinicName = r.VeterinaryClinic != null
                ? r.VeterinaryClinic.Name
                : null,

            ReferringVeterinarianId =
                r.ReferringVeterinarianId,

            ReferringVeterinarianName =
                r.ReferringVeterinarian != null
                    ? r.ReferringVeterinarian.FirstName + " " +
                    r.ReferringVeterinarian.LastName
                    : null,

            ReceivedAt = r.ReceivedAt,
            QrCode = r.QrCode,
            VerifiedWeightKg =
                r.VerifiedWeightKg,

            HasPersonalBelongings =
                r.HasPersonalBelongings,

            PersonalBelongingsDescription =
                r.PersonalBelongingsDescription,

            
            ReferralNotes = r.ReferralNotes,
            Notes = r.Notes,

            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt
        })
        .ToListAsync();

    return new PagedReceptionsDto
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalItems = totalItems,
        TotalPages = (int)Math.Ceiling(
            (double)totalItems / pageSize)
    };
}

    public async Task<ReceptionDto?> GetByIdAsync(Guid id)
{
    return await _context.Receptions
        .AsNoTracking()
        .Where(r =>
            r.Id == id &&
            r.IsActive)
        .Select(r => new ReceptionDto
        {
            Id = r.Id,

            PetId = r.PetId,
            PetName = r.Pet.Name,

            CustomerId = r.Pet.CustomerId,
            CustomerName =
                r.Pet.Customer.FirstName + " " +
                r.Pet.Customer.LastName,

            ReceivedByUserId =
                r.ReceivedByUserId,
            ReceivedByUserName =
                r.ReceivedByUser.FirstName + " " +
                r.ReceivedByUser.LastName,

            VeterinaryClinicId = r.VeterinaryClinicId,

            VeterinaryClinicName = r.VeterinaryClinic != null
                ? r.VeterinaryClinic.Name
                : null,

            ReferringVeterinarianId = r.ReferringVeterinarianId,

            ReferringVeterinarianName =
                r.ReferringVeterinarian != null
                    ? r.ReferringVeterinarian.FirstName + " " +
                    r.ReferringVeterinarian.LastName
                    : null,    

            ReceivedAt = r.ReceivedAt,
            QrCode = r.QrCode,
            VerifiedWeightKg =
                r.VerifiedWeightKg,

            HasPersonalBelongings =
                r.HasPersonalBelongings,
            PersonalBelongingsDescription =
                r.PersonalBelongingsDescription,

            ReferralNotes = r.ReferralNotes,
            Notes = r.Notes,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt
        })
        .FirstOrDefaultAsync();
}

   public async Task<ReceptionDto?> GetByQrCodeAsync(
    string qrCode)
{
    if (string.IsNullOrWhiteSpace(qrCode))
    {
        return null;
    }

    var normalizedQrCode = qrCode
        .Trim()
        .ToUpperInvariant();

    return await _context.Receptions
        .AsNoTracking()
        .Where(r =>
            r.QrCode == normalizedQrCode &&
            r.IsActive)
        .Select(r => new ReceptionDto
        {
            Id = r.Id,

            PetId = r.PetId,
            PetName = r.Pet.Name,

            CustomerId = r.Pet.CustomerId,
            CustomerName =
                r.Pet.Customer.FirstName + " " +
                r.Pet.Customer.LastName,

            ReceivedByUserId =
                r.ReceivedByUserId,
            ReceivedByUserName =
                r.ReceivedByUser.FirstName + " " +
                r.ReceivedByUser.LastName,

            VeterinaryClinicId = r.VeterinaryClinicId,

            VeterinaryClinicName = r.VeterinaryClinic != null
                ? r.VeterinaryClinic.Name
                : null,

            ReferringVeterinarianId =
                r.ReferringVeterinarianId,

            ReferringVeterinarianName =
                r.ReferringVeterinarian != null
                    ? r.ReferringVeterinarian.FirstName + " " +
                     r.ReferringVeterinarian.LastName
                    : null,


            ReceivedAt = r.ReceivedAt,
            QrCode = r.QrCode,
            VerifiedWeightKg =
                r.VerifiedWeightKg,

            HasPersonalBelongings =
                r.HasPersonalBelongings,
            PersonalBelongingsDescription =
                r.PersonalBelongingsDescription,

            ReferralNotes = r.ReferralNotes,
            Notes = r.Notes,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt
        })
        .FirstOrDefaultAsync();
}


public async Task<ReceptionDto?> UpdateAsync(
    Guid id,
    UpdateReceptionDto dto)
{
    if (dto.VerifiedWeightKg <= 0)
    {
        throw new ArgumentException(
            "El peso verificado debe ser mayor que cero.");
    }

    if (dto.HasPersonalBelongings &&
        string.IsNullOrWhiteSpace(
            dto.PersonalBelongingsDescription))
    {
        throw new ArgumentException(
            "Debe describir los objetos personales recibidos.");
    }

    var reception = await _context.Receptions
        .Include(r => r.Pet)
            .ThenInclude(p => p.Customer)
        .Include(r => r.ReceivedByUser)
        .FirstOrDefaultAsync(r =>
            r.Id == id &&
            r.IsActive);

    if (reception == null)
    {
        return null;
    }

    if (dto.ReferringVeterinarianId.HasValue &&
        !dto.VeterinaryClinicId.HasValue)
    {
        throw new ArgumentException(
            "A veterinary clinic must be selected when a referring veterinarian is provided.");
    }

    if (!string.IsNullOrWhiteSpace(dto.ReferralNotes) &&
        !dto.VeterinaryClinicId.HasValue)
    {
        throw new ArgumentException(
            "A veterinary clinic must be selected when referral notes are provided.");
    }

    VeterinaryClinic? veterinaryClinic = null;
    Veterinarian? referringVeterinarian = null;

    if (dto.VeterinaryClinicId.HasValue)
    {
        veterinaryClinic = await _context.VeterinaryClinics
            .FirstOrDefaultAsync(v =>
                v.Id == dto.VeterinaryClinicId.Value &&
                v.IsActive);

        if (veterinaryClinic == null)
        {
            throw new InvalidOperationException(
                "Active veterinary clinic not found.");
        }
    }

    if (dto.ReferringVeterinarianId.HasValue)
    {
        referringVeterinarian = await _context.Veterinarians
            .FirstOrDefaultAsync(v =>
                v.Id == dto.ReferringVeterinarianId.Value &&
                v.IsActive);

        if (referringVeterinarian == null)
        {
            throw new InvalidOperationException(
                "Active referring veterinarian not found.");
        }

        if (referringVeterinarian.VeterinaryClinicId !=
            dto.VeterinaryClinicId!.Value)
        {
            throw new InvalidOperationException(
                "The referring veterinarian does not belong to the selected veterinary clinic.");
        }
    }

    reception.VeterinaryClinicId =
        dto.VeterinaryClinicId;

    reception.ReferringVeterinarianId =
        dto.ReferringVeterinarianId;

    reception.ReferralNotes =
        string.IsNullOrWhiteSpace(dto.ReferralNotes)
            ? null
            : dto.ReferralNotes.Trim();

    reception.VerifiedWeightKg =
        dto.VerifiedWeightKg;

    reception.HasPersonalBelongings =
        dto.HasPersonalBelongings;

    reception.PersonalBelongingsDescription =
        dto.HasPersonalBelongings
            ? dto.PersonalBelongingsDescription?.Trim()
            : null;

    reception.Notes =
        string.IsNullOrWhiteSpace(dto.Notes)
            ? null
            : dto.Notes.Trim();

    await _context.SaveChangesAsync();

    return new ReceptionDto
    {
        Id = reception.Id,

        PetId = reception.PetId,
        PetName = reception.Pet.Name,

        CustomerId = reception.Pet.CustomerId,
        CustomerName =
            reception.Pet.Customer.FirstName + " " +
            reception.Pet.Customer.LastName,

        ReceivedByUserId =
            reception.ReceivedByUserId,

        ReceivedByUserName =
            reception.ReceivedByUser.FirstName + " " +
            reception.ReceivedByUser.LastName,

        VeterinaryClinicId =
            reception.VeterinaryClinicId,

        VeterinaryClinicName =
            veterinaryClinic?.Name,

        ReferringVeterinarianId =
            reception.ReferringVeterinarianId,

        ReferringVeterinarianName =
            referringVeterinarian == null
                ? null
                : referringVeterinarian.FirstName + " " +
                  referringVeterinarian.LastName,

        ReceivedAt = reception.ReceivedAt,
        QrCode = reception.QrCode,

        VerifiedWeightKg =
            reception.VerifiedWeightKg,

        HasPersonalBelongings =
            reception.HasPersonalBelongings,

        PersonalBelongingsDescription =
            reception.PersonalBelongingsDescription,

        ReferralNotes =
            reception.ReferralNotes,

        Notes = reception.Notes,
        IsActive = reception.IsActive,
        CreatedAt = reception.CreatedAt
    };
}

    public async Task<bool> DeactivateAsync(Guid id)
{
    var reception = await _context.Receptions
        .FirstOrDefaultAsync(r =>
            r.Id == id &&
            r.IsActive);

    if (reception == null)
    {
        return false;
    }

    reception.IsActive = false;

    await _context.SaveChangesAsync();

    return true;
}

    public async Task<bool> RestoreAsync(Guid id)
{
    var reception = await _context.Receptions
        .FirstOrDefaultAsync(r =>
            r.Id == id &&
            !r.IsActive);

    if (reception == null)
    {
        return false;
    }

    reception.IsActive = true;

    await _context.SaveChangesAsync();

    return true;
}

    public async Task<IEnumerable<ReceptionDto>> SearchAsync(
    string search)
{
    if (string.IsNullOrWhiteSpace(search))
    {
        return Array.Empty<ReceptionDto>();
    }

    var normalizedSearch = search
        .Trim()
        .ToLower();

    return await _context.Receptions
        .AsNoTracking()
        .Where(r =>
            r.IsActive &&
            (
                r.QrCode.ToLower()
                    .Contains(normalizedSearch) ||

                r.Pet.Name.ToLower()
                    .Contains(normalizedSearch) ||

                r.Pet.Customer.FirstName.ToLower()
                    .Contains(normalizedSearch) ||

                r.Pet.Customer.LastName.ToLower()
                    .Contains(normalizedSearch) ||

                r.ReceivedByUser.FirstName.ToLower()
                    .Contains(normalizedSearch) ||

                r.ReceivedByUser.LastName.ToLower()
                    .Contains(normalizedSearch)
            ))
        .OrderByDescending(r => r.ReceivedAt)
        .Select(r => new ReceptionDto
        {
            Id = r.Id,

            PetId = r.PetId,
            PetName = r.Pet.Name,

            CustomerId = r.Pet.CustomerId,
            CustomerName =
                r.Pet.Customer.FirstName + " " +
                r.Pet.Customer.LastName,

            ReceivedByUserId =
                r.ReceivedByUserId,
            ReceivedByUserName =
                r.ReceivedByUser.FirstName + " " +
                r.ReceivedByUser.LastName,

            VeterinaryClinicId = r.VeterinaryClinicId,

            VeterinaryClinicName = r.VeterinaryClinic != null
                ? r.VeterinaryClinic.Name
                : null,

            ReferringVeterinarianId =
                r.ReferringVeterinarianId,

            ReferringVeterinarianName =
                r.ReferringVeterinarian != null
                    ? r.ReferringVeterinarian.FirstName + " " +
                     r.ReferringVeterinarian.LastName
                    : null,

            ReceivedAt = r.ReceivedAt,
            QrCode = r.QrCode,
            VerifiedWeightKg =
                r.VerifiedWeightKg,

            HasPersonalBelongings =
                r.HasPersonalBelongings,
            PersonalBelongingsDescription =
                r.PersonalBelongingsDescription,

            ReferralNotes = r.ReferralNotes,
            Notes = r.Notes,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt
        })
        .ToListAsync();
}
}