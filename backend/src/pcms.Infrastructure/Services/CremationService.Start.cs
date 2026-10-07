using Microsoft.EntityFrameworkCore;
using Npgsql;
using pcms.Application.Cremations.DTOs;
using pcms.Domain.Entities;
using pcms.Domain.Enums;

namespace pcms.Infrastructure.Services;

public partial class CremationService
{
    private const string StartIdentityMessage =
        "La identificación del caso no coincide con esta cremación.";
    private const string StartRequestConflictMessage =
        "El inicio de esta cremación ya fue confirmado con otra solicitud.";
    private const string StartPricingMessage =
        "La cremación no conserva un paquete y una cotización válidos para iniciar.";
    private const string StartUrnMessage =
        "Debe reservar la urna seleccionada antes de iniciar la cremación.";

    private static void ValidateStartRequest(ChangeCremationStatusDto dto)
    {
        if (dto.Status != CremationStatus.InProgress)
        {
            if (dto.RequestId.HasValue || dto.ReceptionQrCode is not null ||
                dto.CustodyAccepted.HasValue)
            {
                throw new ArgumentException(
                    "La confirmación de identidad y custodia solo corresponde al inicio de la cremación.");
            }
            return;
        }

        if (!dto.RequestId.HasValue || dto.RequestId.Value == Guid.Empty)
            throw new ArgumentException("La solicitud de inicio es obligatoria.");
        if (string.IsNullOrWhiteSpace(dto.ReceptionQrCode))
            throw new ArgumentException("Escanea o ingresa el QR de la recepción antes de iniciar.");
        if (dto.CustodyAccepted != true)
            throw new ArgumentException(
                "Confirma la recepción y custodia del caso antes de iniciar la cremación.");
    }

    private async Task<User> LockActiveStartActorAsync(Guid actorUserId)
    {
        var actorIds = await _context.Database.SqlQuery<Guid>($"""
            SELECT "Id" AS "Value" FROM "Usuarios"
            WHERE "Id" = {actorUserId} AND "Activo" = TRUE
            FOR SHARE
            """).ToListAsync();

        if (actorIds.Count != 1)
            throw new InvalidOperationException("El usuario que confirma el inicio no está activo.");

        return await _context.Users.AsNoTracking()
            .SingleAsync(user => user.Id == actorUserId);
    }

    private static string NormalizeStartQr(string qr) => qr.Trim().ToUpperInvariant();

    private static void EnsureMatchingStartReplay(
        Cremation cremation,
        CremationStartVerification verification,
        ChangeCremationStatusDto dto,
        Guid actorUserId)
    {
        if (verification.CremationId != cremation.Id ||
            verification.ReceptionId != cremation.ReceptionId ||
            cremation.Reception.Id != cremation.ReceptionId ||
            verification.RequestId != dto.RequestId ||
            verification.ConfirmedByUserId != actorUserId ||
            verification.ReceptionQrCodeSnapshot != NormalizeStartQr(dto.ReceptionQrCode!) ||
            dto.CustodyAccepted != true)
        {
            throw new InvalidOperationException(StartRequestConflictMessage);
        }
    }

    private async Task ValidateNewStartAsync(
        Cremation cremation,
        ChangeCremationStatusDto dto,
        Guid actorUserId)
    {
        if (cremation.Status != CremationStatus.Scheduled ||
            cremation.StartedAt.HasValue || cremation.CompletedAt.HasValue ||
            cremation.ReadyForDeliveryAt.HasValue || cremation.DeliveredAt.HasValue)
        {
            throw new InvalidOperationException("La cremación ya no está disponible para iniciar.");
        }

        if (cremation.AssignedToUserId != actorUserId)
            throw new InvalidOperationException(
                "Solo el empleado asignado puede confirmar la custodia e iniciar esta cremación.");

        if (cremation.Reception.Id != cremation.ReceptionId ||
            cremation.Reception.QrCode != NormalizeStartQr(dto.ReceptionQrCode!))
            throw new InvalidOperationException(StartIdentityMessage);

        if (!cremation.RequiredStartPaymentAmount.HasValue)
            throw new InvalidOperationException(
                "Debe validar el pago requerido de esta cremación antes de iniciarla.");

        // Validate preserved case facts; today's catalog does not replace history.
        if (!cremation.CremationPackageId.HasValue ||
            cremation.CremationPackageId == Guid.Empty ||
            cremation.QuotedPrice is null or <= 0m ||
            !Enum.IsDefined(cremation.CremationType) ||
            cremation.QuotedWeightKg is null or <= 0m ||
            cremation.QuotedMinimumWeightKg is null or < 0m ||
            cremation.QuotedMaximumWeightKg is null or <= 0m ||
            cremation.QuotedMinimumWeightKg > cremation.QuotedMaximumWeightKg ||
            cremation.QuotedWeightKg < cremation.QuotedMinimumWeightKg ||
            cremation.QuotedWeightKg > cremation.QuotedMaximumWeightKg ||
            cremation.RequiredStartPaymentAmount < 0m)
        {
            throw new InvalidOperationException(StartPricingMessage);
        }

        var accountIds = await _context.Database.SqlQuery<Guid>($"""
            SELECT "Id" AS "Value" FROM "CuentasPago"
            WHERE "CremacionId" = {cremation.Id}
            FOR UPDATE
            """).ToListAsync();

        var amountPaid = accountIds.Count == 0 ? 0m :
            await _context.Payments.AsNoTracking()
                .Where(payment => payment.PaymentAccountId == accountIds[0])
                .SumAsync(payment => payment.Amount);

        if (amountPaid < cremation.RequiredStartPaymentAmount.Value)
            throw new InvalidOperationException(
                "El pago registrado no alcanza el monto requerido para iniciar la cremación.");

        if (!cremation.IncludesUrn)
        {
            if (cremation.UrnId.HasValue)
                throw new InvalidOperationException("La selección de urna no corresponde al servicio de esta cremación.");
            return;
        }

        if (!cremation.UrnId.HasValue)
            throw new InvalidOperationException(StartUrnMessage);

        var reservationIds = await _context.Database.SqlQuery<Guid>($"""
            SELECT "Id" AS "Value" FROM "ReservasUrnaCremacion"
            WHERE "CremationId" = {cremation.Id} AND "Status" = 1
            FOR UPDATE
            """).ToListAsync();

        var reservation = reservationIds.Count == 1
            ? await _context.CremationUrnReservations.AsNoTracking()
                .SingleAsync(item => item.Id == reservationIds[0])
            : null;

        if (reservation is null || reservation.CremationId != cremation.Id ||
            reservation.UrnId != cremation.UrnId ||
            reservation.Status != UrnReservationStatus.Active ||
            reservation.CancelledAt.HasValue || reservation.FulfilledAt.HasValue ||
            reservation.FulfillmentId.HasValue)
        {
            throw new InvalidOperationException(StartUrnMessage);
        }
    }

    private void AddStartVerification(
        Cremation cremation,
        ChangeCremationStatusDto dto,
        User actor,
        DateTime confirmedAt)
    {
        // Independent FKs alone do not guarantee this cross-row identity.
        if (cremation.Reception.Id != cremation.ReceptionId)
            throw new InvalidOperationException(StartIdentityMessage);

        _context.CremationStartVerifications.Add(new CremationStartVerification
        {
            Id = Guid.NewGuid(),
            CremationId = cremation.Id,
            ReceptionId = cremation.Reception.Id,
            ReceptionQrCodeSnapshot = cremation.Reception.QrCode,
            ConfirmedByUserId = actor.Id,
            ConfirmedByUserNameSnapshot = string.Join(" ",
                new[] { actor.FirstName, actor.LastName }
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim())),
            ConfirmedAt = confirmedAt,
            RequestId = dto.RequestId!.Value
        });
    }

    private static bool IsStartVerificationUniqueConflict(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres &&
                postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
                postgres.ConstraintName == "IX_VerificacionesInicioCremacion_CremacionId")
                return true;
        }
        return false;
    }
}
