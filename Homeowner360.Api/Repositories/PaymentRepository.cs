using Homeowner360.Api.Data;
using Homeowner360.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Homeowner360.Api.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly HomeownerDbContext _context;

    public PaymentRepository(HomeownerDbContext context)
    {
        _context = context;
    }

    public async Task<List<Payment>> GetAllPayments(
        int userId)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.Loan.Customer.UserId == userId)
            .ToListAsync();
    }

    public async Task<Payment?> GetPaymentById(
        int id,
        int userId)
    {
        return await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(payment =>
                payment.PaymentId == id &&
                payment.Loan.Customer.UserId == userId);
    }

    public async Task<(List<Payment> Payments, int TotalRecords)>
        GetPaymentsByLoanId(
            int loanId,
            int page,
            int pageSize,
            string? status,
            int userId)
    {
        var query = _context.Payments
            .Where(payment =>
                payment.LoanId == loanId &&
                payment.Loan.Customer.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(
                payment => payment.Status == status);
        }

        var totalRecords = await query.CountAsync();

        var payments = await query
            .AsNoTracking()
            .OrderByDescending(
                payment => payment.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (payments, totalRecords);
    }

    public async Task<bool> LoanBelongsToUser(
        int loanId,
        int userId)
    {
        return await _context.Loans
            .AnyAsync(loan =>
                loan.LoanId == loanId &&
                loan.Customer.UserId == userId);
    }

    public async Task AddPayment(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
    }

    public async Task SaveChanges()
    {
        await _context.SaveChangesAsync();
    }
}