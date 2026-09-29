using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Auth;
using pcms.Application.Receptions.DTOs;
using pcms.Application.Receptions.Interfaces;
using pcms.Application.Receptions.Exceptions;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Receptions.View")]
public class ReceptionsController : ControllerBase
{
    private readonly IReceptionService _receptionService;

    public ReceptionsController(
        IReceptionService receptionService)
    {
        _receptionService = receptionService;
    }

    [HttpPost]
    [Authorize(Policy = "Receptions.Manage")]
    public async Task<ActionResult<ReceptionDto>> Create(
        CreateReceptionDto dto)
    {
        var receivedByUserId = GetCurrentUserId();

        if (receivedByUserId == null)
        {
            return Unauthorized(new
            {
                success = false,
                message =
                    "No se pudo identificar al usuario autenticado."
            });
        }

        try
        {
            var reception =
                await _receptionService.CreateAsync(
                    dto,
                    receivedByUserId.Value);

            return CreatedAtAction(
                nameof(GetById),
                new { id = reception.Id },
                reception);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<PagedReceptionsDto>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool includeInactive = false)
    {
        try
        {
            var actorUserId = includeInactive
                ? GetCurrentUserId()
                : null;

            var receptions =
                await _receptionService.GetAllAsync(
                    page,
                    pageSize,
                    includeInactive,
                    actorUserId);

            return Ok(receptions);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    success = false,
                    message = ex.Message
                });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReceptionDto>> GetById(
        Guid id,
        [FromQuery] bool includeInactive = false)
    {
        ReceptionDto? reception;

        try
        {
            reception = await _receptionService.GetByIdAsync(
                id,
                includeInactive,
                includeInactive ? GetCurrentUserId() : null);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    success = false,
                    message = ex.Message
                });
        }

        if (reception == null)
        {
            return NotFound(new
            {
                success = false,
                message = "La recepción no fue encontrada."
            });
        }

        return Ok(reception);
    }

    [HttpGet("qr/{qrCode}")]
    public async Task<ActionResult<ReceptionDto>> GetByQrCode(
        string qrCode)
    {
        var reception =
            await _receptionService.GetByQrCodeAsync(qrCode);

        if (reception == null)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "No se encontró una recepción con ese código QR."
            });
        }

        return Ok(reception);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Receptions.Manage")]
    public async Task<ActionResult<ReceptionDto>> Update(
    Guid id,
    UpdateReceptionDto dto)
    {
        var actorUserId = GetCurrentUserId();

        if (!actorUserId.HasValue)
        {
            return Unauthorized(new
            {
                success = false,
                message =
                    "No se pudo identificar al usuario autenticado."
            });
        }

        try
        {
            var reception =
                await _receptionService.UpdateAsync(
                    id,
                    dto,
                    actorUserId.Value);

            if (reception == null)
            {
                return NotFound(new
                {
                    success = false,
                    message =
                        "Recepción no encontrada."
                });
            }

            return Ok(reception);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (WeightRangeChangeConfirmationRequiredException ex)
        {
            return Conflict(new
            {
                success = false,

                code =
                    "WEIGHT_RANGE_CHANGE_CONFIRMATION_REQUIRED",

                message = ex.Message,

                weightChange = new
                {
                    previousWeightKg =
                        ex.PreviousWeightKg,

                    newWeightKg =
                        ex.NewWeightKg,

                    previousMinimumWeightKg =
                        ex.PreviousMinimumWeightKg,

                    previousMaximumWeightKg =
                        ex.PreviousMaximumWeightKg,

                    newMinimumWeightKg =
                        ex.NewMinimumWeightKg,

                    newMaximumWeightKg =
                        ex.NewMaximumWeightKg,

                    previousPrice =
                        ex.PreviousPrice,

                    priceDifference =
                        ex.PriceDifference,

                    amountPaid =
                        ex.AmountPaid,

                    remainingBalance =
                        ex.RemainingBalance,

                    overpaymentAmount =
                        ex.OverpaymentAmount,

                    requiresFinancialReview =
                        ex.RequiresFinancialReview,

                    newPrice =
                        ex.NewPrice
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpPost("{id:guid}/amendments")]
    [Authorize(Policy = PermissionCodes.ReceptionsAmend)]
    public async Task<ActionResult<ReceptionHistoryEventDto>>
        CreateCorrection(
            Guid id,
            CreateReceptionCorrectionDto dto)
    {
        var actorUserId = GetCurrentUserId();

        if (!actorUserId.HasValue)
        {
            return Unauthorized(new
            {
                success = false,
                message =
                    "No se pudo identificar al usuario autenticado."
            });
        }

        try
        {
            var historyEvent =
                await _receptionService.CreateCorrectionAsync(
                    id,
                    dto,
                    actorUserId.Value);

            if (historyEvent is null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Recepción no encontrada."
                });
            }

            return Ok(historyEvent);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (WeightRangeChangeConfirmationRequiredException ex)
        {
            return WeightRangeConfirmationRequired(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    success = false,
                    message = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpPost("{id:guid}/clarifications")]
    [Authorize(Policy = PermissionCodes.ReceptionsManage)]
    public async Task<ActionResult<ReceptionHistoryEventDto>>
        CreateClarification(
            Guid id,
            CreateReceptionClarificationDto dto)
    {
        var actorUserId = GetCurrentUserId();

        if (!actorUserId.HasValue)
        {
            return Unauthorized(new
            {
                success = false,
                message =
                    "No se pudo identificar al usuario autenticado."
            });
        }

        try
        {
            var historyEvent =
                await _receptionService.CreateClarificationAsync(
                    id,
                    dto,
                    actorUserId.Value);

            if (historyEvent is null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Recepción no encontrada."
                });
            }

            return Ok(historyEvent);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    success = false,
                    message = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpGet("{id:guid}/amendments")]
    [Authorize(Policy = PermissionCodes.ReceptionsView)]
    public async Task<ActionResult<
        IReadOnlyList<ReceptionHistoryEventDto>>> GetHistory(Guid id)
    {
        var history = await _receptionService.GetHistoryAsync(id);

        if (history is null)
        {
            return NotFound(new
            {
                success = false,
                message = "Recepción no encontrada."
            });
        }

        return Ok(history);
    }

    [HttpPost("{id:guid}/lifecycle/deactivate")]
    [Authorize(Policy = PermissionCodes.ReceptionsManage)]
    public async Task<ActionResult<ReceptionLifecycleDecisionDto>>
        DeactivateLifecycle(
            Guid id,
            ReceptionLifecycleActionRequestDto request) =>
        await ExecuteLifecycleAction(
            id,
            request,
            _receptionService.DeactivateAsync);

    [HttpPost("{id:guid}/lifecycle/restore")]
    [Authorize(Policy = PermissionCodes.ReceptionsManage)]
    public async Task<ActionResult<ReceptionLifecycleDecisionDto>>
        RestoreLifecycle(
            Guid id,
            ReceptionLifecycleActionRequestDto request) =>
        await ExecuteLifecycleAction(
            id,
            request,
            _receptionService.RestoreAsync);

    [HttpPost("{id:guid}/lifecycle/deactivation-request")]
    [Authorize(Policy = PermissionCodes.ReceptionsManage)]
    public async Task<ActionResult<ReceptionLifecycleDecisionDto>>
        RequestDeactivation(
            Guid id,
            ReceptionLifecycleActionRequestDto request) =>
        await ExecuteLifecycleAction(
            id,
            request,
            _receptionService.RequestDeactivationAsync);

    [HttpGet("{id:guid}/lifecycle")]
    [Authorize(Policy = PermissionCodes.ReceptionsView)]
    public async Task<ActionResult<
        IReadOnlyList<ReceptionLifecycleEventDto>>>
        GetLifecycleHistory(Guid id)
    {
        var history =
            await _receptionService.GetLifecycleHistoryAsync(id);

        if (history is null)
        {
            return NotFound(new
            {
                success = false,
                message = "Recepción no encontrada."
            });
        }

        return Ok(history);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ReceptionDto>>>
        Search([FromQuery] string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "Debe proporcionar un término de búsqueda."
            });
        }

        var receptions =
            await _receptionService.SearchAsync(search);

        return Ok(receptions);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        return Guid.TryParse(userIdValue, out var userId)
            ? userId
            : null;
    }

    private async Task<ActionResult<ReceptionLifecycleDecisionDto>>
        ExecuteLifecycleAction(
            Guid id,
            ReceptionLifecycleActionRequestDto request,
            Func<Guid, ReceptionLifecycleActionRequestDto, Guid,
                Task<ReceptionLifecycleDecisionDto?>> action)
    {
        var actorUserId = GetCurrentUserId();

        if (!actorUserId.HasValue)
        {
            return Unauthorized(new
            {
                success = false,
                message =
                    "No se pudo identificar al usuario autenticado."
            });
        }

        try
        {
            var decision = await action(
                id,
                request,
                actorUserId.Value);

            if (decision is null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Recepción no encontrada."
                });
            }

            return decision.IsBlocked
                ? Conflict(decision)
                : Ok(decision);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    success = false,
                    message = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    private ConflictObjectResult WeightRangeConfirmationRequired(
        WeightRangeChangeConfirmationRequiredException ex)
    {
        return Conflict(new
        {
            success = false,
            code = "WEIGHT_RANGE_CHANGE_CONFIRMATION_REQUIRED",
            message = ex.Message,
            weightChange = new
            {
                previousWeightKg = ex.PreviousWeightKg,
                newWeightKg = ex.NewWeightKg,
                previousMinimumWeightKg =
                    ex.PreviousMinimumWeightKg,
                previousMaximumWeightKg =
                    ex.PreviousMaximumWeightKg,
                newMinimumWeightKg = ex.NewMinimumWeightKg,
                newMaximumWeightKg = ex.NewMaximumWeightKg,
                previousPrice = ex.PreviousPrice,
                priceDifference = ex.PriceDifference,
                amountPaid = ex.AmountPaid,
                remainingBalance = ex.RemainingBalance,
                overpaymentAmount = ex.OverpaymentAmount,
                requiresFinancialReview = ex.RequiresFinancialReview,
                newPrice = ex.NewPrice,
                newCremationPriceId = ex.NewCremationPriceId
            }
        });
    }
}
