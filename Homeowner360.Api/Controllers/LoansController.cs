using System.Security.Claims;
using Homeowner360.Api.DTOs;
using Homeowner360.Api.Models;
using Homeowner360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeowner360.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(
            ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            throw new UnauthorizedAccessException(
                "User identity could not be determined.");
        }

        return int.Parse(userIdClaim.Value);
    }

    // GET: api/Loans
    [HttpGet]
    public async Task<ActionResult<List<LoanDto>>> GetLoans()
    {
        var userId = GetCurrentUserId();

        var loans = await _loanService.GetLoans(userId);

        var loanDtos = loans.Select(loan => new LoanDto
        {
            LoanId = loan.LoanId,
            CustomerId = loan.CustomerId,
            LoanNumber = loan.LoanNumber,
            OriginalAmount = loan.OriginalAmount,
            CurrentBalance = loan.CurrentBalance,
            InterestRate = loan.InterestRate
        }).ToList();

        return Ok(loanDtos);
    }

    // GET: api/Loans/1
    [HttpGet("{id}")]
    public async Task<ActionResult<LoanDto>> GetLoan(int id)
    {
        var userId = GetCurrentUserId();

        var loan = await _loanService.GetLoanById(
            id,
            userId);

        if (loan == null)
        {
            return NotFound(new
            {
                message = $"Loan with ID {id} was not found."
            });
        }

        var loanDto = new LoanDto
        {
            LoanId = loan.LoanId,
            CustomerId = loan.CustomerId,
            LoanNumber = loan.LoanNumber,
            OriginalAmount = loan.OriginalAmount,
            CurrentBalance = loan.CurrentBalance,
            InterestRate = loan.InterestRate
        };

        return Ok(loanDto);
    }

    // POST: api/Loans
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<LoanDto>> CreateLoan(
        CreateLoanDto loanDto)
    {
        var userId = GetCurrentUserId();

        var createdLoan = await _loanService.CreateLoan(
            loanDto,
            userId);

        var response = new LoanDto
        {
            LoanId = createdLoan.LoanId,
            CustomerId = createdLoan.CustomerId,
            LoanNumber = createdLoan.LoanNumber,
            OriginalAmount = createdLoan.OriginalAmount,
            CurrentBalance = createdLoan.CurrentBalance,
            InterestRate = createdLoan.InterestRate
        };

        return CreatedAtAction(
            nameof(GetLoan),
            new { id = response.LoanId },
            response);
    }

    // GET: api/Loans/1/payments
    [HttpGet("{id}/payments")]
    public async Task<ActionResult<List<PaymentDto>>> GetLoanPayments(
        int id)
    {
        var userId = GetCurrentUserId();

        var payments = await _loanService.GetLoanPayments(
            id,
            userId);

        var paymentDtos = payments.Select(payment => new PaymentDto
        {
            PaymentId = payment.PaymentId,
            LoanId = payment.LoanId,
            Amount = payment.Amount,
            Status = payment.Status ?? string.Empty,
            PaymentDate = payment.PaymentDate
        }).ToList();

        return Ok(paymentDtos);
    }

    // PUT: api/Loans/1
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<LoanDto>> UpdateLoan(
        int id,
        UpdateLoanDto loanDto)
    {
        var userId = GetCurrentUserId();

        var updatedLoan = await _loanService.UpdateLoan(
            id,
            loanDto,
            userId);

        if (updatedLoan == null)
        {
            return NotFound(new
            {
                message = $"Loan with ID {id} was not found."
            });
        }

        var response = new LoanDto
        {
            LoanId = updatedLoan.LoanId,
            CustomerId = updatedLoan.CustomerId,
            LoanNumber = updatedLoan.LoanNumber,
            OriginalAmount = updatedLoan.OriginalAmount,
            CurrentBalance = updatedLoan.CurrentBalance,
            InterestRate = updatedLoan.InterestRate
        };

        return Ok(response);
    }

    // DELETE: api/Loans/1
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLoan(int id)
    {
        var userId = GetCurrentUserId();

        var deleted = await _loanService.DeleteLoan(
            id,
            userId);

        if (!deleted)
        {
            return NotFound(new
            {
                message = $"Loan with ID {id} was not found."
            });
        }

        return NoContent();
    }

    // GET: api/Loans/search
    [HttpGet("search")]
    public async Task<ActionResult<PagedResultDto<Loan>>> SearchLoans(
        [FromQuery] LoanQueryDto query)
    {
        var userId = GetCurrentUserId();

        var result = await _loanService.SearchLoans(
            query,
            userId);

        return Ok(result);
    }
}