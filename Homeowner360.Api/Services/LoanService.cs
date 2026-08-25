using Homeowner360.Api.Data;
using Homeowner360.Api.DTOs;
using Homeowner360.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Homeowner360.Api.Services;

public class LoanService : ILoanService
{
    private readonly HomeownerDbContext _context;

    public LoanService(HomeownerDbContext context)
    {
        _context = context;
    }

    public async Task<List<Loan>> GetLoans(int userId)
    {
        return await _context.Loans
            .Include(loan => loan.Customer)
            .AsNoTracking()
            .Where(loan => loan.Customer.UserId == userId)
            .ToListAsync();
    }

    public async Task<Loan?> GetLoanById(
        int id,
        int userId)
    {
        return await _context.Loans
            .Include(loan => loan.Customer)
            .Include(loan => loan.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(loan =>
                loan.LoanId == id &&
                loan.Customer.UserId == userId);
    }

    public async Task<Loan> CreateLoan(
        CreateLoanDto loanDto,
        int userId)
    {
        // Make sure the customer belongs to the
        // currently authenticated user.
        var customerExists = await _context.Customers
            .AnyAsync(customer =>
                customer.CustomerId == loanDto.CustomerId &&
                customer.UserId == userId);

        if (!customerExists)
        {
            throw new ArgumentException(
                "Customer does not exist or does not belong to the current user.");
        }

        var loan = new Loan
        {
            CustomerId = loanDto.CustomerId,
            LoanNumber = loanDto.LoanNumber,
            OriginalAmount = loanDto.OriginalAmount,
            CurrentBalance = loanDto.CurrentBalance,
            InterestRate = loanDto.InterestRate
        };

        _context.Loans.Add(loan);

        await _context.SaveChangesAsync();

        return loan;
    }

    public async Task<List<Payment>> GetLoanPayments(
        int loanId,
        int userId)
    {
        // Verify that the loan belongs to the current user.
        var loanExists = await _context.Loans
            .AnyAsync(loan =>
                loan.LoanId == loanId &&
                loan.Customer.UserId == userId);

        if (!loanExists)
        {
            return new List<Payment>();
        }

        return await _context.Payments
            .Where(payment => payment.LoanId == loanId)
            .AsNoTracking()
            .OrderByDescending(payment => payment.PaymentDate)
            .ToListAsync();
    }

    public async Task<Loan?> UpdateLoan(
        int id,
        UpdateLoanDto loanDto,
        int userId)
    {
        var loan = await _context.Loans
            .FirstOrDefaultAsync(loan =>
                loan.LoanId == id &&
                loan.Customer.UserId == userId);

        if (loan == null)
        {
            return null;
        }

        loan.LoanNumber = loanDto.LoanNumber;
        loan.OriginalAmount = loanDto.OriginalAmount;
        loan.CurrentBalance = loanDto.CurrentBalance;
        loan.InterestRate = loanDto.InterestRate;

        await _context.SaveChangesAsync();

        return loan;
    }

    public async Task<bool> DeleteLoan(
        int id,
        int userId)
    {
        var loan = await _context.Loans
            .Include(loan => loan.Payments)
            .FirstOrDefaultAsync(loan =>
                loan.LoanId == id &&
                loan.Customer.UserId == userId);

        if (loan == null)
        {
            return false;
        }

        if (loan.Payments.Any())
        {
            throw new InvalidOperationException(
                "Loan cannot be deleted because it has payment history.");
        }

        _context.Loans.Remove(loan);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<PagedResultDto<Loan>> SearchLoans(
        LoanQueryDto query,
        int userId)
    {
        var loansQuery = _context.Loans
            .Include(loan => loan.Customer)
            .AsNoTracking()
            .Where(loan => loan.Customer.UserId == userId)
            .AsQueryable();

        // Filter by customer
        if (query.CustomerId.HasValue)
        {
            loansQuery = loansQuery.Where(
                loan => loan.CustomerId == query.CustomerId.Value);
        }

        // Filter by loan number
        if (!string.IsNullOrWhiteSpace(query.LoanNumber))
        {
            loansQuery = loansQuery.Where(
                loan => loan.LoanNumber.Contains(query.LoanNumber));
        }

        // Minimum balance
        if (query.MinBalance.HasValue)
        {
            loansQuery = loansQuery.Where(
                loan => loan.CurrentBalance >= query.MinBalance.Value);
        }

        // Maximum balance
        if (query.MaxBalance.HasValue)
        {
            loansQuery = loansQuery.Where(
                loan => loan.CurrentBalance <= query.MaxBalance.Value);
        }

        // Sorting
        loansQuery = query.SortBy?.ToLower() switch
        {
            "balance" => query.SortDescending
                ? loansQuery.OrderByDescending(
                    loan => loan.CurrentBalance)
                : loansQuery.OrderBy(
                    loan => loan.CurrentBalance),

            "amount" => query.SortDescending
                ? loansQuery.OrderByDescending(
                    loan => loan.OriginalAmount)
                : loansQuery.OrderBy(
                    loan => loan.OriginalAmount),

            "interest" => query.SortDescending
                ? loansQuery.OrderByDescending(
                    loan => loan.InterestRate)
                : loansQuery.OrderBy(
                    loan => loan.InterestRate),

            "loannumber" => query.SortDescending
                ? loansQuery.OrderByDescending(
                    loan => loan.LoanNumber)
                : loansQuery.OrderBy(
                    loan => loan.LoanNumber),

            _ => loansQuery.OrderBy(
                loan => loan.LoanId)
        };

        // Validate pagination
        var page = query.Page < 1
            ? 1
            : query.Page;

        var pageSize = query.PageSize switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => query.PageSize
        };

        var totalRecords = await loansQuery.CountAsync();

        var totalPages = (int)Math.Ceiling(
            totalRecords / (double)pageSize);

        var loans = await loansQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<Loan>
        {
            Items = loans,
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = totalPages
        };
    }
}