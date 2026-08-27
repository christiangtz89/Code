using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pcms.Application.Payments.DTOs;
using pcms.Application.Payments.Interfaces;
using pcms.Domain.Enums;

namespace pcms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Payments.View")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(
        IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("accounts")]
    [Authorize(Policy = "Payments.Manage")]
    public async Task<ActionResult<PaymentAccountDto>>
        CreateAccount(
            CreatePaymentAccountDto dto)
    {
        try
        {
            var account =
                await _paymentService.CreateAccountAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = account.Id },
                account);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
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

    [HttpGet("accounts")]
    public async Task<ActionResult<PagedPaymentAccountsDto>>
        GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] PaymentStatus? status = null)
    {
        if (page < 1)
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "La página debe ser mayor que cero."
            });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "El tamaño de página debe estar entre 1 y 100."
            });
        }

        if (status.HasValue &&
            !Enum.IsDefined(
                typeof(PaymentStatus),
                status.Value))
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "El estado de pago no es válido."
            });
        }

        var accounts =
            await _paymentService.GetAllAsync(
                page,
                pageSize,
                status);

        return Ok(accounts);
    }

    [HttpGet("accounts/{id:guid}")]
    public async Task<ActionResult<PaymentAccountDto>>
        GetById(Guid id)
    {
        var account =
            await _paymentService.GetByIdAsync(id);

        if (account == null)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "La cuenta de pago no fue encontrada."
            });
        }

        return Ok(account);
    }

    [HttpGet(
        "accounts/cremation/{cremationId:guid}")]
    public async Task<ActionResult<PaymentAccountDto>>
        GetByCremationId(Guid cremationId)
    {
        var account =
            await _paymentService
                .GetByCremationIdAsync(
                    cremationId);

        if (account == null)
        {
            return NotFound(new
            {
                success = false,
                message =
                    "No se encontró una cuenta de pago para esta cremación."
            });
        }

        return Ok(account);
    }

    [HttpPost(
        "accounts/{id:guid}/payments")]
    [Authorize(Policy = "Payments.Manage")]
    public async Task<ActionResult<PaymentDto>>
        AddPayment(
            Guid id,
            CreatePaymentDto dto)
    {
        var recordedByUserId =
            GetCurrentUserId();

        if (recordedByUserId == null)
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
            var payment =
                await _paymentService.AddPaymentAsync(
                    id,
                    dto,
                    recordedByUserId.Value);

            return Ok(payment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
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

    [HttpGet(
        "accounts/{id:guid}/payments")]
    public async Task<
        ActionResult<IEnumerable<PaymentDto>>>
        GetPaymentHistory(Guid id)
    {
        try
        {
            var payments =
                await _paymentService
                    .GetPaymentHistoryAsync(id);

            return Ok(payments);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpGet("accounts/search")]
    public async Task<
        ActionResult<IEnumerable<PaymentAccountDto>>>
        Search(
            [FromQuery] string search,
            [FromQuery] PaymentStatus? status = null)
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

        if (status.HasValue &&
            !Enum.IsDefined(
                typeof(PaymentStatus),
                status.Value))
        {
            return BadRequest(new
            {
                success = false,
                message =
                    "El estado de pago no es válido."
            });
        }

        var accounts =
            await _paymentService.SearchAsync(
                search,
                status);

        return Ok(accounts);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;

        return Guid.TryParse(
            userIdValue,
            out var userId)
            ? userId
            : null;
    }

    [HttpGet("options/cremations")]
    public async Task<
    ActionResult<IEnumerable<PaymentCremationOptionDto>>>
    GetAvailableCremationOptions()
    {
        var cremations =
            await _paymentService
                .GetAvailableCremationOptionsAsync();

        return Ok(cremations);
    }
}
