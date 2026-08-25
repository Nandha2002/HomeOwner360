using Homeowner360.Api.Models;

namespace Homeowner360.Api.Repositories;

public interface IPaymentRepository
{
    Task<List<Payment>> GetAllPayments(
        int userId);

    Task<Payment?> GetPaymentById(
        int id,
        int userId);

    Task<(List<Payment> Payments, int TotalRecords)>
        GetPaymentsByLoanId(
            int loanId,
            int page,
            int pageSize,
            string? status,
            int userId);

    Task<bool> LoanBelongsToUser(
        int loanId,
        int userId);

    Task AddPayment(Payment payment);

    Task SaveChanges();
}