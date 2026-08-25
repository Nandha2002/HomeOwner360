using System.Security.Claims;
using Homeowner360.Api.DTOs;
using Homeowner360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeowner360.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // GET: api/Payments
    [HttpGet]
    public async Task<ActionResult<List<PaymentDto>>> GetPayments()
    {
        var userId = GetCurrentUserId();

        var payments = await _paymentService.GetPayments(userId);

        var paymentDtos = payments
            .Select(payment => new PaymentDto
            {
                PaymentId = payment.PaymentId,
                LoanId = payment.LoanId,
                Amount = payment.Amount,
                Status = payment.Status ?? string.Empty,
                PaymentDate = payment.PaymentDate
            })
            .ToList();

        return Ok(paymentDtos);
    }

    // GET: api/Payments/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<PaymentDto>> GetPayment(int id)
    {
        var userId = GetCurrentUserId();

        var payment = await _paymentService.GetPaymentById(
            id,
            userId);

        if (payment == null)
        {
            return NotFound(new
            {
                message = $"Payment with ID {id} was not found."
            });
        }

        var paymentDto = new PaymentDto
        {
            PaymentId = payment.PaymentId,
            LoanId = payment.LoanId,
            Amount = payment.Amount,
            Status = payment.Status ?? string.Empty,
            PaymentDate = payment.PaymentDate
        };

        return Ok(paymentDto);
    }

    // POST: api/Payments
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> CreatePayment(
        CreatePaymentDto paymentDto)
    {
        var userId = GetCurrentUserId();

        var payment = await _paymentService.CreatePayment(
            paymentDto,
            userId);

        var response = new PaymentDto
        {
            PaymentId = payment.PaymentId,
            LoanId = payment.LoanId,
            Amount = payment.Amount,
            Status = payment.Status ?? string.Empty,
            PaymentDate = payment.PaymentDate
        };

        return CreatedAtAction(
            nameof(GetPayment),
            new { id = response.PaymentId },
            response);
    }

    // GET: api/Payments/loan/{loanId}/history
    [HttpGet("loan/{loanId}/history")]
    public async Task<ActionResult<PaymentHistoryDto>> GetPaymentHistory(
        int loanId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        var userId = GetCurrentUserId();

        var result = await _paymentService.GetPaymentsByLoanId(
            loanId,
            page,
            pageSize,
            status,
            userId);

        return Ok(result);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(
            ClaimTypes.NameIdentifier);

        if (userIdClaim == null ||
            !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException(
                "User identity could not be determined.");
        }

        return userId;
    }
}