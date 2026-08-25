using Homeowner360.Api.Data;
using Homeowner360.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Homeowner360.Api.Services;

public class DashboardService : IDashboardService
{
    private readonly HomeownerDbContext _context;

    public DashboardService(HomeownerDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDto> GetDashboard(int userId)
    {
        var totalCustomers = await _context.Customers
            .Where(customer => customer.UserId == userId)
            .CountAsync();

        var totalLoans = await _context.Loans
            .Where(loan =>
                loan.Customer.UserId == userId)
            .CountAsync();

        var totalLoanAmount = await _context.Loans
            .Where(loan =>
                loan.Customer.UserId == userId)
            .SumAsync(loan =>
                (decimal?)loan.OriginalAmount) ?? 0;

        var totalOutstandingBalance = await _context.Loans
            .Where(loan =>
                loan.Customer.UserId == userId)
            .SumAsync(loan =>
                (decimal?)loan.CurrentBalance) ?? 0;

        var totalPayments = await _context.Payments
            .Where(payment =>
                payment.Loan.Customer.UserId == userId)
            .SumAsync(payment =>
                (decimal?)payment.Amount) ?? 0;

        var totalPaymentsCount = await _context.Payments
            .Where(payment =>
                payment.Loan.Customer.UserId == userId)
            .CountAsync();

        return new DashboardDto
        {
            TotalCustomers = totalCustomers,
            TotalLoans = totalLoans,
            TotalLoanAmount = totalLoanAmount,
            TotalOutstandingBalance =
                totalOutstandingBalance,
            TotalPayments = totalPayments,
            TotalPaymentsCount = totalPaymentsCount
        };
    }
}