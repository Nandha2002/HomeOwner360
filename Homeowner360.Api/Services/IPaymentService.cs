using Homeowner360.Api.DTOs;
using Homeowner360.Api.Models;

namespace Homeowner360.Api.Services;

public interface IPaymentService
{
    Task<List<Payment>> GetPayments(
        int userId);

    Task<Payment?> GetPaymentById(
        int id,
        int userId);

    Task<PaymentHistoryDto> GetPaymentsByLoanId(
        int loanId,
        int page,
        int pageSize,
        string? status,
        int userId);

    Task<Payment> CreatePayment(
        CreatePaymentDto paymentDto,
        int userId);
}