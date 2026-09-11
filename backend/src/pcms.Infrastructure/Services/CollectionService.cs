using System.Data;
using Microsoft.EntityFrameworkCore;
using pcms.Application.Auth;
using pcms.Application.Collections.DTOs;
using pcms.Application.Collections.Interfaces;
using pcms.Application.Receptions.Exceptions;
using pcms.Domain.Entities;
using pcms.Domain.Enums;
using pcms.Infrastructure.Persistence;

namespace pcms.Infrastructure.Services;

public class CollectionService : ICollectionService
{

        private const decimal WeightCorrectionTolerance = 0.10m;
    private readonly AppDbContext _context;

    public CollectionService(
        AppDbContext context)
    {
        _context = context;
    }

    public async Task<CollectionDto> CreateAsync(
        CreateCollectionDto dto,
        Guid createdByUserId)
    {
        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "No se pudo identificar al usuario que realiza la recolección.");
        }

        ValidateCommonCollectionData(
            dto.LocationType,
            dto.PickupAddress,
            dto.ApproximateWeightKg,
            dto.HasPersonalBelongings,
            dto.PersonalBelongingsDescription);

        _ = await GetActiveUserAsync(
                createdByUserId,
                "El usuario que registra la recolección no existe o está inactivo.");

        await using var transaction =
            await _context.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable);

        var currentTime =
            DateTime.UtcNow;

        Customer customer;
        Pet pet;

        /*
         * PATH C
         * Existing Pet
         */
        if (dto.ExistingPetId.HasValue)
        {
            pet = CustomerPetWorkflowRules.RequireEligiblePet(
                await _context.Pets
                .Include(p => p.Customer)
                .Include(p => p.Reception)
                .FirstOrDefaultAsync(p =>
                    p.Id == dto.ExistingPetId.Value));

            if (pet.Reception != null)
            {
                throw new InvalidOperationException(
                    "La mascota seleccionada ya tiene una recepción registrada.");
            }

            customer =
                pet.Customer;

            if (dto.ExistingCustomerId.HasValue &&
                pet.CustomerId !=
                    dto.ExistingCustomerId.Value)
            {
                throw new InvalidOperationException(
                    "La mascota seleccionada no pertenece al cliente seleccionado.");
            }
        }
        else
        {
            /*
             * PATH B
             * Existing Customer + New Pet
             */
            if (dto.ExistingCustomerId.HasValue)
            {
                customer =
                    await _context.Customers
                        .FirstOrDefaultAsync(c =>
                            c.Id ==
                                dto.ExistingCustomerId.Value &&
                            c.IsActive)
                    ?? throw new InvalidOperationException(
                        "No se encontró un cliente activo válido.");
            }
            /*
             * PATH A
             * New Customer + New Pet
             */
            else
            {
                ValidateNewCustomerData(dto);

                customer = new Customer
                {
                    Id = Guid.NewGuid(),

                    FirstName =
                        dto.OwnerFirstName!.Trim(),

                    LastName =
                        dto.OwnerLastName!.Trim(),

                    SecondLastName =
                        NormalizeOptional(
                            dto.OwnerSecondLastName),

                    Phone =
                        dto.OwnerPhone!.Trim(),

                    Email =
                        dto.OwnerEmail!.Trim(),

                    IsActive = true,

                    CreatedAt =
                        currentTime
                };

                _context.Customers.Add(
                    customer);
            }

            ValidateNewPetData(dto);

            pet = new Pet
            {
                Id = Guid.NewGuid(),

                CustomerId =
                    customer.Id,

                Name =
                    dto.PetName!.Trim(),

                Species =
                    dto.Species!.Trim(),

                Breed =
                    dto.Breed!.Trim(),

                Sex =
                    dto.Sex!.Trim(),

                Color =
                    dto.Color!.Trim(),

                WeightKg =
                    dto.ApproximateWeightKg!.Value,

                AgeYears =
                    dto.AgeYears,

                DateOfDeath =
                    DateOnly.FromDateTime(dto.DateOfDeath!.Value),

                IsActive = true,

                CreatedAt =
                    currentTime
            };

            _context.Pets.Add(pet);
        }

        /*
         * Only one currently-collected custody
         * event can exist for a Pet.
         */
        var hasActiveCollection =
            await _context.Collections
                .AnyAsync(c =>
                    c.PetId == pet.Id &&
                    c.IsActive &&
                    c.Status != CollectionStatus.Received &&
                    c.Status != CollectionStatus.Cancelled);

        if (hasActiveCollection)
        {
            throw new InvalidOperationException(
                "La mascota ya tiene una recolección activa.");
        }

        var location =
            await ValidateLocationAsync(
                dto.LocationType,
                dto.VeterinaryClinicId,
                dto.ReferringVeterinarianId);

        var qrCode =
            await GenerateUniqueQrCodeAsync();

        var pickupContactName =
            NormalizeOptional(
                dto.PickupContactName);

        var pickupContactPhone =
            NormalizeOptional(
                dto.PickupContactPhone);

        /*
         * For a customer-home collection,
         * use the customer as the default
         * pickup contact when no override
         * was entered.
         */
        if (dto.LocationType ==
            CollectionLocationType.CustomerHome)
        {
            pickupContactName ??=
                GetCustomerFullName(customer);

            pickupContactPhone ??=
                customer.Phone;
        }

        var approximateWeightKg =
            dto.ApproximateWeightKg ??
            pet.WeightKg;

        var collection =
            new Collection
            {
                Id = Guid.NewGuid(),

                PetId =
                    pet.Id,

                CollectedByUserId = null,

                LocationType =
                    dto.LocationType,

                VeterinaryClinicId =
                    location.Clinic?.Id,

                ReferringVeterinarianId =
                    location.Veterinarian?.Id,

                Status =
                    CollectionStatus.Pending,

                QrCode =
                    qrCode,

                PickupAddress =
                    dto.PickupAddress.Trim(),

                PickupContactName =
                    pickupContactName,

                PickupContactPhone =
                    pickupContactPhone,

                ApproximateWeightKg =
                    approximateWeightKg,

                HasPersonalBelongings =
                    dto.HasPersonalBelongings,

                PersonalBelongingsDescription =
                    dto.HasPersonalBelongings
                        ? NormalizeOptional(
                            dto.PersonalBelongingsDescription)
                        : null,

                Notes =
                    NormalizeOptional(
                        dto.Notes),

                CollectedAt = null,

                ReceivedAt = null,

                CancelledAt = null,

                IsActive = true,

                CreatedAt =
                    currentTime
            };

        _context.Collections.Add(
            collection);

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return await GetByIdAsync(
            collection.Id)
            ?? throw new InvalidOperationException(
                "No fue posible recuperar la recolección creada.");
    }

    public async Task<PagedCollectionsDto> GetAllAsync(
        int page,
        int pageSize,
        CollectionStatus? status,
        CollectionLocationType? locationType)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "La página debe ser mayor que cero.");
        }

        if (pageSize < 1 ||
            pageSize > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "El tamaño de página debe estar entre 1 y 100.");
        }

        ValidateOptionalEnums(
            status,
            locationType);

        var query =
            _context.Collections
                .AsNoTracking()
                .Where(c => c.IsActive);

        if (status.HasValue)
        {
            query =
                query.Where(c =>
                    c.Status ==
                        status.Value);
        }

        if (locationType.HasValue)
        {
            query =
                query.Where(c =>
                    c.LocationType ==
                        locationType.Value);
        }

        var totalItems =
            await query.CountAsync();

        var items =
            await ProjectToDto(query)
                .OrderByDescending(c =>
                    c.CollectedAt)
                .Skip(
                    (page - 1) *
                    pageSize)
                .Take(pageSize)
                .ToListAsync();

        return new PagedCollectionsDto
        {
            Items = items,

            Page = page,

            PageSize = pageSize,

            TotalItems =
                totalItems,

            TotalPages =
                (int)Math.Ceiling(
                    (double)totalItems /
                    pageSize)
        };
    }

    public async Task<CollectionDto?>
        GetByIdAsync(
            Guid id)
    {
        return await ProjectToDto(
                _context.Collections
                    .AsNoTracking()
                    .Where(c =>
                        c.Id == id &&
                        c.IsActive))
            .FirstOrDefaultAsync();
    }

    public async Task<CollectionDto?>
        GetByQrCodeAsync(
            string qrCode)
    {
        if (string.IsNullOrWhiteSpace(
            qrCode))
        {
            return null;
        }

        var normalizedQrCode =
            qrCode
                .Trim()
                .ToUpperInvariant();

        return await ProjectToDto(
                _context.Collections
                    .AsNoTracking()
                    .Where(c =>
                        c.QrCode ==
                            normalizedQrCode &&
                        c.IsActive))
            .FirstOrDefaultAsync();
    }

    public async Task<CollectionDto?>
        UpdateAsync(
            Guid id,
            UpdateCollectionDto dto)
    {
        ValidateCommonCollectionData(
            dto.LocationType,
            dto.PickupAddress,
            dto.ApproximateWeightKg,
            dto.HasPersonalBelongings,
            dto.PersonalBelongingsDescription);

        var collection =
            await _context.Collections
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.IsActive);

        if (collection == null)
        {
            return null;
        }

        if (!IsPreReceptionState(collection.Status))
        {
            throw new InvalidOperationException(
                "Una recolección recibida o cancelada ya no puede editarse.");
        }

        var location =
            await ValidateLocationAsync(
                dto.LocationType,
                dto.VeterinaryClinicId,
                dto.ReferringVeterinarianId);

        collection.LocationType =
            dto.LocationType;

        collection.VeterinaryClinicId =
            location.Clinic?.Id;

        collection.ReferringVeterinarianId =
            location.Veterinarian?.Id;

        collection.PickupAddress =
            dto.PickupAddress.Trim();

        collection.PickupContactName =
            NormalizeOptional(
                dto.PickupContactName);

        collection.PickupContactPhone =
            NormalizeOptional(
                dto.PickupContactPhone);

        collection.ApproximateWeightKg =
            dto.ApproximateWeightKg;

        collection.HasPersonalBelongings =
            dto.HasPersonalBelongings;

        collection.PersonalBelongingsDescription =
            dto.HasPersonalBelongings
                ? NormalizeOptional(
                    dto.PersonalBelongingsDescription)
                : null;

        collection.Notes =
            NormalizeOptional(
                dto.Notes);

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<CollectionDto?>
        ChangeStatusAsync(
            Guid id,
            ChangeCollectionStatusDto dto,
            Guid actorUserId)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "No se pudo identificar al usuario que cancela la recolección.");
        }

        _ = await GetActiveUserAsync(
            actorUserId,
            "El usuario que cancela la recolección no existe o está inactivo.");

        if (!Enum.IsDefined(
            typeof(CollectionStatus),
            dto.Status))
        {
            throw new ArgumentException(
                "El estado de la recolección no es válido.");
        }

        var collection =
            await _context.Collections
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.IsActive);

        if (collection == null)
        {
            return null;
        }

        if (dto.Status ==
            CollectionStatus.Received)
        {
            throw new InvalidOperationException(
                "Utilice la conversión a recepción para marcar una recolección como recibida.");
        }

        if (!IsPreReceptionState(collection.Status))
        {
            throw new InvalidOperationException(
                "La recolección ya se encuentra en un estado terminal.");
        }

        if (dto.Status !=
            CollectionStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "La única transición manual permitida es cancelar la recolección.");
        }

        collection.Status =
            CollectionStatus.Cancelled;

        collection.CancelledAt =
            DateTime.UtcNow;

        var currentAssignment =
            await _context.CollectionAssignmentHistory
                .FirstOrDefaultAsync(history =>
                    history.CollectionId == collection.Id &&
                    history.EndedAt == null);

        if (currentAssignment != null)
        {
            currentAssignment.EndedByUserId = actorUserId;
            currentAssignment.EndedAt = collection.CancelledAt;
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<IEnumerable<CollectionDriverOptionDto>>
        GetActiveDriverOptionsAsync()
    {
        return await EligibleDriversQuery()
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new CollectionDriverOptionDto
            {
                Id = user.Id,
                Name = user.FirstName + " " + user.LastName
            })
            .ToListAsync();
    }

    public async Task<CollectionDto?> AssignAsync(
        Guid id,
        AssignCollectionDto dto,
        Guid assignedByUserId)
    {
        if (dto.DriverUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Debe seleccionar un conductor.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var assignedByUser = await GetActiveUserAsync(
            assignedByUserId,
            "El usuario que asigna la recolección no existe o está inactivo.");

        var driver = await EligibleDriversQuery()
            .FirstOrDefaultAsync(user => user.Id == dto.DriverUserId)
            ?? throw new InvalidOperationException(
                "El conductor seleccionado no existe, está inactivo o no tiene acceso a recolecciones.");

        var collection = await _context.Collections
            .FirstOrDefaultAsync(current =>
                current.Id == id &&
                current.IsActive);

        if (collection == null)
        {
            return null;
        }

        if (collection.Status is CollectionStatus.Collected or
            CollectionStatus.Received or CollectionStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "La asignación no puede cambiar después de confirmar la custodia, recibir o cancelar la recolección.");
        }

        if (collection.AssignedDriverId == driver.Id &&
            collection.Status is CollectionStatus.Assigned or
                CollectionStatus.Accepted)
        {
            await transaction.CommitAsync();
            return await GetByIdAsync(id);
        }

        var now = DateTime.UtcNow;
        var currentAssignment =
            await _context.CollectionAssignmentHistory
                .FirstOrDefaultAsync(history =>
                    history.CollectionId == collection.Id &&
                    history.EndedAt == null);

        if (currentAssignment != null)
        {
            currentAssignment.EndedByUserId = assignedByUser.Id;
            currentAssignment.EndedAt = now;
        }

        collection.AssignedDriverId = driver.Id;
        collection.AssignedByUserId = assignedByUser.Id;
        collection.AssignedAt = now;
        collection.AcceptedByUserId = null;
        collection.AcceptedAt = null;
        collection.Status = CollectionStatus.Assigned;

        _context.CollectionAssignmentHistory.Add(
            new CollectionAssignmentHistory
            {
                Id = Guid.NewGuid(),
                CollectionId = collection.Id,
                AssignedDriverId = driver.Id,
                AssignedByUserId = assignedByUser.Id,
                AssignedAt = now
            });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(id);
    }

    public async Task<CollectionDto?> AcceptAsync(
        Guid id,
        Guid actorUserId)
    {
        var actor = await GetActiveUserAsync(
            actorUserId,
            "El usuario que acepta la recolección no existe o está inactivo.");

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var collection = await _context.Collections
            .FirstOrDefaultAsync(current =>
                current.Id == id &&
                current.IsActive);

        if (collection == null)
        {
            return null;
        }

        if (collection.AssignedDriverId != actor.Id)
        {
            throw new UnauthorizedAccessException(
                "Solo el conductor asignado puede aceptar esta recolección.");
        }

        if (collection.Status == CollectionStatus.Accepted &&
            collection.AcceptedByUserId == actor.Id)
        {
            return await GetByIdAsync(id);
        }

        if (collection.Status != CollectionStatus.Assigned)
        {
            throw new InvalidOperationException(
                "Solo una recolección asignada puede aceptarse.");
        }

        var assignment =
            await _context.CollectionAssignmentHistory
                .FirstOrDefaultAsync(history =>
                    history.CollectionId == collection.Id &&
                    history.AssignedDriverId == actor.Id &&
                    history.EndedAt == null)
            ?? throw new InvalidOperationException(
                "La asignación actual no tiene un registro de historial válido.");

        var now = DateTime.UtcNow;
        collection.AcceptedByUserId = actor.Id;
        collection.AcceptedAt = now;
        collection.Status = CollectionStatus.Accepted;
        assignment.AcceptedByUserId = actor.Id;
        assignment.AcceptedAt = now;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(id);
    }

    public async Task<CollectionDto?> ConfirmCustodyAsync(
        Guid id,
        Guid actorUserId)
    {
        var actor = await GetActiveUserAsync(
            actorUserId,
            "El usuario que confirma la custodia no existe o está inactivo.");

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var collection = await _context.Collections
            .FirstOrDefaultAsync(current =>
                current.Id == id &&
                current.IsActive);

        if (collection == null)
        {
            return null;
        }

        if (collection.AssignedDriverId != actor.Id)
        {
            throw new UnauthorizedAccessException(
                "Solo el conductor asignado puede confirmar la custodia.");
        }

        if (collection.Status != CollectionStatus.Accepted ||
            collection.AcceptedByUserId != actor.Id ||
            !collection.AcceptedAt.HasValue)
        {
            throw new InvalidOperationException(
                "La asignación debe estar aceptada antes de confirmar la custodia.");
        }

        if (!await HasRequiredEvidenceAsync(collection.Id))
        {
            throw new InvalidOperationException(
                "Debe existir al menos una fotografía activa de identificación o evidencia de recolección antes de confirmar la custodia.");
        }

        var now = DateTime.UtcNow;
        collection.CollectedByUserId = actor.Id;
        collection.CollectedAt = now;
        collection.Status = CollectionStatus.Collected;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(id);
    }

    public async Task<CollectionDto?>
        ConvertToReceptionAsync(
            Guid id,
            ConvertCollectionToReceptionDto dto,
            Guid receivedByUserId)
    {
        if (receivedByUserId ==
            Guid.Empty)
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

        var receivedByUser =
            await GetActiveUserAsync(
                receivedByUserId,
                "El usuario que realiza la recepción no existe o está inactivo.");

        await using var transaction =
            await _context.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable);

        var collection =
            await _context.Collections
                .Include(c => c.Pet)
                    .ThenInclude(p =>
                        p.Customer)
                .Include(c => c.Pet)
                    .ThenInclude(p =>
                        p.Reception)
                .Include(c =>
                    c.Reception)
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.IsActive);

        if (collection == null)
        {
            return null;
        }

        CustomerPetWorkflowRules.RequireEligiblePet(collection.Pet);

        if (collection.Status !=
            CollectionStatus.Collected)
        {
            throw new InvalidOperationException(
                "Solo una recolección en estado Recolectada puede convertirse en recepción.");
        }

        if (!collection.CollectedByUserId.HasValue ||
            !collection.CollectedAt.HasValue)
        {
            throw new InvalidOperationException(
                "La custodia física de la mascota no ha sido confirmada.");
        }

        if (!await HasRequiredEvidenceAsync(collection.Id))
        {
            throw new InvalidOperationException(
                "La recolección debe conservar al menos una fotografía activa de identificación o evidencia antes de crear la recepción.");
        }

        var paymentAccount = await _context.PaymentAccounts
            .Include(account => account.Payments)
            .FirstOrDefaultAsync(account =>
                account.CollectionId == collection.Id);

        if (paymentAccount == null)
        {
            throw new InvalidOperationException(
                "Debe registrarse la cuenta y el pago requerido de la recolección antes de crear la recepción.");
        }

        if (paymentAccount.RequiredCollectionPaymentAmount
                is not decimal requiredPaymentAmount ||
            requiredPaymentAmount <= 0m)
        {
            throw new InvalidOperationException(
                "La cuenta no tiene configurado un pago requerido válido para la recolección.");
        }

        var amountPaid = paymentAccount.Payments.Sum(
            payment => payment.Amount);

        if (amountPaid < requiredPaymentAmount)
        {
            throw new InvalidOperationException(
                $"El pago requerido para recibir la mascota es de " +
                $"{requiredPaymentAmount:0.00} MXN; se han registrado " +
                $"{amountPaid:0.00} MXN.");
        }

        if (collection.Reception != null)
        {
            throw new InvalidOperationException(
                "La recolección ya tiene una recepción registrada.");
        }

        if (collection.Pet.Reception != null)
        {
            throw new InvalidOperationException(
                "La mascota ya tiene una recepción registrada.");
        }

                if (collection.ApproximateWeightKg.HasValue)
        {
            var originalWeightKg =
                collection.ApproximateWeightKg.Value;

            var weightChanged =
                Math.Abs(
                    dto.VerifiedWeightKg -
                    originalWeightKg) >= 0.01m;

            if (weightChanged)
            {
                var minimumAllowedWeightKg =
                    originalWeightKg *
                    (1m - WeightCorrectionTolerance);

                var maximumAllowedWeightKg =
                    originalWeightKg *
                    (1m + WeightCorrectionTolerance);

                if (dto.VerifiedWeightKg <
                        minimumAllowedWeightKg ||
                    dto.VerifiedWeightKg >
                        maximumAllowedWeightKg)
                {
                    throw new InvalidOperationException(
                        $"Verifica que sea la mascota correcta. " +
                        $"El peso ingresado " +
                        $"({dto.VerifiedWeightKg:F2} kg) " +
                        $"está fuera de la tolerancia permitida " +
                        $"de ±10% respecto al peso registrado " +
                        $"({originalWeightKg:F2} kg). " +
                        $"El rango permitido es de " +
                        $"{minimumAllowedWeightKg:F2} a " +
                        $"{maximumAllowedWeightKg:F2} kg.");
                }

                var pricingConfiguration =
                    await _context
                        .CremationPricingConfigurations
                        .AsNoTracking()
                        .OrderBy(configuration =>
                            configuration.CreatedAt)
                        .FirstOrDefaultAsync();

                if (pricingConfiguration is null)
                {
                    throw new InvalidOperationException(
                        "No existe una configuración activa de rangos de peso.");
                }

                var currentRange =
                    GetWeightRange(
                        originalWeightKg,
                        pricingConfiguration.WeightInterval);

                var newRange =
                    GetWeightRange(
                        dto.VerifiedWeightKg,
                        pricingConfiguration.WeightInterval);

                var rangeDifference =
                    Math.Abs(
                        newRange.Index -
                        currentRange.Index);

                if (rangeDifference > 1)
                {
                    throw new InvalidOperationException(
                        "Verifica que sea la mascota correcta. " +
                        "El nuevo peso provocaría un cambio de dos o más " +
                        "rangos de precio. No se realizó ningún cambio.");
                }

                if (rangeDifference == 1 &&
                    !dto.ConfirmWeightRangeChange)
                {
                    throw new
                        WeightRangeChangeConfirmationRequiredException(
                            originalWeightKg,
                            dto.VerifiedWeightKg,
                            currentRange.MinimumWeightKg,
                            currentRange.MaximumWeightKg,
                            newRange.MinimumWeightKg,
                            newRange.MaximumWeightKg,
                            null,
                            null);
                }
            }
        }

        /*
         * The collection QR must become
         * the Reception QR unchanged.
         */
        var qrAlreadyUsed =
            await _context.Receptions
                .AnyAsync(r =>
                    r.QrCode ==
                        collection.QrCode);

        if (qrAlreadyUsed)
        {
            throw new InvalidOperationException(
                "El código QR de la recolección ya está asociado a otra recepción.");
        }

        var currentTime =
            DateTime.UtcNow;
        var identitySnapshot =
            CustomerPetWorkflowRules.CaptureReceptionIdentity(collection.Pet);

        var reception =
            new Reception
            {
                Id = Guid.NewGuid(),

                PetId =
                    collection.PetId,

                PetNameSnapshot =
                    identitySnapshot.PetName,

                CustomerNameSnapshot =
                    identitySnapshot.CustomerName,

                ReceivedByUserId =
                    receivedByUser.Id,

                CollectionId =
                    collection.Id,

                VeterinaryClinicId =
                    collection.VeterinaryClinicId,

                ReferringVeterinarianId =
                    collection.ReferringVeterinarianId,

                ReceivedAt =
                    currentTime,

                /*
                 * CRITICAL:
                 * no second QR generation.
                 */
                QrCode =
                    collection.QrCode,

                VerifiedWeightKg =
                    dto.VerifiedWeightKg,

                HasPersonalBelongings =
                    dto.HasPersonalBelongings,

                PersonalBelongingsDescription =
                    dto.HasPersonalBelongings
                        ? NormalizeOptional(
                            dto.PersonalBelongingsDescription)
                        : null,

                ReferralNotes =
                    NormalizeOptional(
                        dto.ReferralNotes),

                Notes =
                    NormalizeOptional(
                        dto.Notes),

                IsActive = true,

                CreatedAt =
                    currentTime
            };

        _context.Receptions.Add(
            reception);

        collection.Status =
            CollectionStatus.Received;

        collection.ReceivedAt =
            currentTime;

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return await GetByIdAsync(id);
    }

    public async Task<IEnumerable<CollectionDto>>
        SearchAsync(
            string search,
            CollectionStatus? status,
            CollectionLocationType? locationType)
    {
        if (string.IsNullOrWhiteSpace(
            search))
        {
            return Array.Empty<CollectionDto>();
        }

        ValidateOptionalEnums(
            status,
            locationType);

        var normalizedSearch =
            search
                .Trim()
                .ToLower();

        var query =
            _context.Collections
                .AsNoTracking()
                .Where(c =>
                    c.IsActive);

        if (status.HasValue)
        {
            query =
                query.Where(c =>
                    c.Status ==
                        status.Value);
        }

        if (locationType.HasValue)
        {
            query =
                query.Where(c =>
                    c.LocationType ==
                        locationType.Value);
        }

        query =
            query.Where(c =>
                c.QrCode
                    .ToLower()
                    .Contains(
                        normalizedSearch) ||

                c.Pet.Name
                    .ToLower()
                    .Contains(
                        normalizedSearch) ||

                c.Pet.Species
                    .ToLower()
                    .Contains(
                        normalizedSearch) ||

                c.Pet.Customer.FirstName
                    .ToLower()
                    .Contains(
                        normalizedSearch) ||

                c.Pet.Customer.LastName
                    .ToLower()
                    .Contains(
                        normalizedSearch) ||

                (
                    c.Pet.Customer.SecondLastName != null &&
                    c.Pet.Customer.SecondLastName
                        .ToLower()
                        .Contains(
                            normalizedSearch)
                ) ||

                c.Pet.Customer.Phone
                    .ToLower()
                    .Contains(
                        normalizedSearch) ||

                c.PickupAddress
                    .ToLower()
                    .Contains(
                        normalizedSearch) ||

                (
                    c.PickupContactName != null &&
                    c.PickupContactName
                        .ToLower()
                        .Contains(
                            normalizedSearch)
                ) ||

                (
                    c.PickupContactPhone != null &&
                    c.PickupContactPhone
                        .ToLower()
                        .Contains(
                            normalizedSearch)
                ) ||

                (
                    c.VeterinaryClinic != null &&
                    c.VeterinaryClinic.Name
                        .ToLower()
                        .Contains(
                            normalizedSearch)
                ) ||

                (
                    c.ReferringVeterinarian != null &&
                    (
                        c.ReferringVeterinarian.FirstName
                            .ToLower()
                            .Contains(
                                normalizedSearch) ||

                        c.ReferringVeterinarian.LastName
                            .ToLower()
                            .Contains(
                                normalizedSearch) ||

                        (
                            c.ReferringVeterinarian.SecondLastName != null &&
                            c.ReferringVeterinarian.SecondLastName
                                .ToLower()
                                .Contains(
                                    normalizedSearch)
                        )
                    )
                ));

        return await ProjectToDto(query)
            .OrderByDescending(c =>
                c.CollectedAt)
            .ToListAsync();
    }

    private async Task<User>
        GetActiveUserAsync(
            Guid userId,
            string errorMessage)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Id == userId &&
                u.IsActive)
            ?? throw new InvalidOperationException(
                errorMessage);
    }

    private IQueryable<User> EligibleDriversQuery()
    {
        return _context.Users
            .AsNoTracking()
            .Where(user =>
                user.IsActive &&
                (user.IsOwner ||
                 user.UserRoles.Any(userRole =>
                     userRole.Role.IsActive &&
                     userRole.Role.RolePermissions.Any(rolePermission =>
                         rolePermission.Permission.Code ==
                             PermissionCodes.CollectionsView ||
                         rolePermission.Permission.Code ==
                             PermissionCodes.CollectionsManage))));
    }

    private Task<bool> HasRequiredEvidenceAsync(Guid collectionId)
    {
        return _context.CollectionPhotos.AnyAsync(photo =>
            photo.CollectionId == collectionId &&
            photo.IsActive &&
            (photo.PhotoType == CollectionPhotoType.PetIdentification ||
             photo.PhotoType == CollectionPhotoType.PickupEvidence));
    }

    private async Task<
        (
            VeterinaryClinic? Clinic,
            Veterinarian? Veterinarian
        )>
        ValidateLocationAsync(
            CollectionLocationType locationType,
            Guid? veterinaryClinicId,
            Guid? veterinarianId)
    {
        if (!Enum.IsDefined(
            typeof(CollectionLocationType),
            locationType))
        {
            throw new ArgumentException(
                "El tipo de ubicación de recolección no es válido.");
        }

        /*
         * Customer home cannot have a
         * veterinary pickup source.
         */
        if (locationType ==
            CollectionLocationType.CustomerHome)
        {
            if (veterinaryClinicId.HasValue ||
                veterinarianId.HasValue)
            {
                throw new ArgumentException(
                    "Una recolección en domicilio del cliente no puede tener una veterinaria o veterinario como ubicación de recolección.");
            }

            return (null, null);
        }

        /*
         * Veterinary-location collection
         * requires a clinic or veterinarian.
         */
        if (!veterinaryClinicId.HasValue &&
            !veterinarianId.HasValue)
        {
            throw new ArgumentException(
                "Debe seleccionar una veterinaria o un veterinario para una recolección veterinaria.");
        }

        VeterinaryClinic? clinic = null;
        Veterinarian? veterinarian = null;

        if (veterinaryClinicId.HasValue)
        {
            clinic =
                await _context.VeterinaryClinics
                    .FirstOrDefaultAsync(c =>
                        c.Id ==
                            veterinaryClinicId.Value &&
                        c.IsActive);

            if (clinic == null)
            {
                throw new InvalidOperationException(
                    "La veterinaria seleccionada no existe o está inactiva.");
            }
        }

        if (veterinarianId.HasValue)
        {
            veterinarian =
                await _context.Veterinarians
                    .FirstOrDefaultAsync(v =>
                        v.Id ==
                            veterinarianId.Value &&
                        v.IsActive);

            if (veterinarian == null)
            {
                throw new InvalidOperationException(
                    "El veterinario seleccionado no existe o está inactivo.");
            }

            /*
             * Clinic-associated veterinarian:
             * matching clinic is required.
             */
            if (veterinarian
                .VeterinaryClinicId
                .HasValue)
            {
                if (!veterinaryClinicId.HasValue)
                {
                    throw new ArgumentException(
                        "El veterinario seleccionado pertenece a una veterinaria. Debe seleccionar también esa veterinaria.");
                }

                if (veterinarian
                    .VeterinaryClinicId
                    .Value !=
                    veterinaryClinicId.Value)
                {
                    throw new ArgumentException(
                        "El veterinario seleccionado no pertenece a la veterinaria indicada.");
                }
            }
            /*
             * Independent veterinarian:
             * cannot be paired with a clinic.
             */
            else if (veterinaryClinicId.HasValue)
            {
                throw new ArgumentException(
                    "Un veterinario independiente no puede asociarse con una veterinaria en esta recolección.");
            }
        }

        return (
            clinic,
            veterinarian);
    }

    private async Task<string>
        GenerateUniqueQrCodeAsync()
    {
        string qrCode;

        do
        {
            qrCode =
                $"PCMS-{Guid.NewGuid():N}"
                    .ToUpperInvariant();
        }
        while (
            await _context.Collections
                .AnyAsync(c =>
                    c.QrCode ==
                        qrCode) ||

            await _context.Receptions
                .AnyAsync(r =>
                    r.QrCode ==
                        qrCode)
        );

        return qrCode;
    }

    private static void
        ValidateCommonCollectionData(
            CollectionLocationType locationType,
            string pickupAddress,
            decimal? approximateWeightKg,
            bool hasPersonalBelongings,
            string? personalBelongingsDescription)
    {
        if (!Enum.IsDefined(
            typeof(CollectionLocationType),
            locationType))
        {
            throw new ArgumentException(
                "El tipo de ubicación de recolección no es válido.");
        }

        if (string.IsNullOrWhiteSpace(
            pickupAddress))
        {
            throw new ArgumentException(
                "La dirección de recolección es obligatoria.");
        }

        if (approximateWeightKg.HasValue &&
            approximateWeightKg.Value <= 0)
        {
            throw new ArgumentException(
                "El peso aproximado debe ser mayor que cero.");
        }

        if (hasPersonalBelongings &&
            string.IsNullOrWhiteSpace(
                personalBelongingsDescription))
        {
            throw new ArgumentException(
                "Debe describir los objetos personales recibidos.");
        }
    }

    private static void
        ValidateNewCustomerData(
            CreateCollectionDto dto)
    {
        if (string.IsNullOrWhiteSpace(
            dto.OwnerFirstName))
        {
            throw new ArgumentException(
                "El nombre del propietario es obligatorio para crear un nuevo cliente.");
        }

        if (string.IsNullOrWhiteSpace(
            dto.OwnerLastName))
        {
            throw new ArgumentException(
                "El apellido paterno del propietario es obligatorio para crear un nuevo cliente.");
        }

        if (string.IsNullOrWhiteSpace(
            dto.OwnerPhone))
        {
            throw new ArgumentException(
                "El teléfono del propietario es obligatorio para crear un nuevo cliente.");
        }

        if (string.IsNullOrWhiteSpace(
            dto.OwnerEmail))
        {
            throw new ArgumentException(
                "El correo electrónico del propietario es obligatorio para crear un nuevo cliente.");
        }
    }

    private static void
        ValidateNewPetData(
            CreateCollectionDto dto)
    {
        if (string.IsNullOrWhiteSpace(
            dto.PetName))
        {
            throw new ArgumentException(
                "El nombre de la mascota es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(
            dto.Species))
        {
            throw new ArgumentException(
                "La especie de la mascota es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(
            dto.Breed))
        {
            throw new ArgumentException(
                "La raza de la mascota es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(
            dto.Sex))
        {
            throw new ArgumentException(
                "El sexo de la mascota es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(
            dto.Color))
        {
            throw new ArgumentException(
                "El color de la mascota es obligatorio.");
        }

        if (!dto.ApproximateWeightKg.HasValue ||
            dto.ApproximateWeightKg.Value <= 0)
        {
            throw new ArgumentException(
                "El peso aproximado de la mascota es obligatorio y debe ser mayor que cero.");
        }

        if (!dto.DateOfDeath.HasValue)
        {
            throw new ArgumentException(
                "La fecha de fallecimiento de la mascota es obligatoria.");
        }

        if (dto.DateOfDeath.Value.Date >
            DateTime.UtcNow.Date)
        {
            throw new ArgumentException(
                "La fecha de fallecimiento no puede estar en el futuro.");
        }
    }

    private static void
        ValidateOptionalEnums(
            CollectionStatus? status,
            CollectionLocationType? locationType)
    {
        if (status.HasValue &&
            !Enum.IsDefined(
                typeof(CollectionStatus),
                status.Value))
        {
            throw new ArgumentException(
                "El estado de la recolección no es válido.");
        }

        if (locationType.HasValue &&
            !Enum.IsDefined(
                typeof(CollectionLocationType),
                locationType.Value))
        {
            throw new ArgumentException(
                "El tipo de ubicación de recolección no es válido.");
        }
    }

    private static string?
        NormalizeOptional(
            string? value)
    {
        return string.IsNullOrWhiteSpace(
            value)
            ? null
            : value.Trim();
    }

    private static string
        GetCustomerFullName(
            Customer customer)
    {
        return string.Join(
            " ",
            new[]
            {
                customer.FirstName,
                customer.LastName,
                customer.SecondLastName
            }
            .Where(value =>
                !string.IsNullOrWhiteSpace(
                    value)));
    }

    private static IQueryable<CollectionDto>
        ProjectToDto(
            IQueryable<Collection> query)
    {
        return query.Select(c =>
            new CollectionDto
            {
                Id =
                    c.Id,

                PetId =
                    c.PetId,

                PetName =
                    c.Pet.Name,

                PetSpecies =
                    c.Pet.Species,

                CustomerId =
                    c.Pet.CustomerId,

                CustomerName =
                    c.Pet.Customer.FirstName +
                    " " +
                    c.Pet.Customer.LastName +
                    (
                        c.Pet.Customer.SecondLastName != null
                            ? " " +
                              c.Pet.Customer.SecondLastName
                            : ""
                    ),

                CustomerPhone =
                    c.Pet.Customer.Phone,

                CollectedByUserId =
                    c.CollectedByUserId,

                CollectedByUserName =
                    c.CollectedByUser != null
                        ? c.CollectedByUser.FirstName +
                          " " +
                          c.CollectedByUser.LastName
                        : null,

                AssignedDriverId = c.AssignedDriverId,

                AssignedDriverName =
                    c.AssignedDriver != null
                        ? c.AssignedDriver.FirstName +
                          " " + c.AssignedDriver.LastName
                        : null,

                AssignedByUserId = c.AssignedByUserId,

                AssignedByUserName =
                    c.AssignedByUser != null
                        ? c.AssignedByUser.FirstName +
                          " " + c.AssignedByUser.LastName
                        : null,

                AssignedAt = c.AssignedAt,

                AcceptedByUserId = c.AcceptedByUserId,

                AcceptedByUserName =
                    c.AcceptedByUser != null
                        ? c.AcceptedByUser.FirstName +
                          " " + c.AcceptedByUser.LastName
                        : null,

                AcceptedAt = c.AcceptedAt,

                LocationType =
                    c.LocationType,

                VeterinaryClinicId =
                    c.VeterinaryClinicId,

                VeterinaryClinicName =
                    c.VeterinaryClinic != null
                        ? c.VeterinaryClinic.Name
                        : null,

                ReferringVeterinarianId =
                    c.ReferringVeterinarianId,

                ReferringVeterinarianName =
                    c.ReferringVeterinarian != null
                        ? c.ReferringVeterinarian.FirstName +
                          " " +
                          c.ReferringVeterinarian.LastName +
                          (
                              c.ReferringVeterinarian.SecondLastName != null
                                  ? " " +
                                    c.ReferringVeterinarian.SecondLastName
                                  : ""
                          )
                        : null,

                Status =
                    c.Status,

                QrCode =
                    c.QrCode,

                PickupAddress =
                    c.PickupAddress,

                PickupContactName =
                    c.PickupContactName,

                PickupContactPhone =
                    c.PickupContactPhone,

                ApproximateWeightKg =
                    c.ApproximateWeightKg,

                HasPersonalBelongings =
                    c.HasPersonalBelongings,

                PersonalBelongingsDescription =
                    c.PersonalBelongingsDescription,

                Notes =
                    c.Notes,

                ReceptionId =
                    c.Reception != null
                        ? c.Reception.Id
                        : null,

                ReceptionQrCode =
                    c.Reception != null
                        ? c.Reception.QrCode
                        : null,

                CollectedAt =
                    c.CollectedAt,

                ReceivedAt =
                    c.ReceivedAt,

                CancelledAt =
                    c.CancelledAt,

                IsActive =
                    c.IsActive,

                CreatedAt =
                    c.CreatedAt
            });
    }

    private static bool IsPreReceptionState(CollectionStatus status)
    {
        return status is CollectionStatus.Pending or
            CollectionStatus.Assigned or
            CollectionStatus.Accepted or
            CollectionStatus.Collected;
    }

        private sealed record WeightRangeInfo(
        int Index,
        decimal MinimumWeightKg,
        decimal MaximumWeightKg);

    private static WeightRangeInfo GetWeightRange(
        decimal weightKg,
        WeightPricingInterval interval)
    {
        var intervalKg =
            (decimal)(int)interval;

        var index =
            (int)Math.Ceiling(
                weightKg / intervalKg);

        if (index < 1)
        {
            index = 1;
        }

        var maximumWeightKg =
            index * intervalKg;

        var minimumWeightKg =
            index == 1
                ? 0.01m
                : ((index - 1) * intervalKg) +
                    0.01m;

        return new WeightRangeInfo(
            index,
            minimumWeightKg,
            maximumWeightKg);
    }
}
