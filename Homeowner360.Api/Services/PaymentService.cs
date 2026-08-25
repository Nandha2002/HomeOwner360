using Homeowner360.Api.DTOs;
using Homeowner360.Api.Models;
using Homeowner360.Api.Repositories;

namespace Homeowner360.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;

    public PaymentService(
        IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<List<Payment>> GetPayments(
        int userId)
    {
        return await _paymentRepository
            .GetAllPayments(userId);
    }

    public async Task<Payment?> GetPaymentById(
        int id,
        int userId)
    {
        return await _paymentRepository
            .GetPaymentById(id, userId);
    }

    public async Task<PaymentHistoryDto> GetPaymentsByLoanId(
        int loanId,
        int page,
        int pageSize,
        string? status,
        int userId)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var result =
            await _paymentRepository.GetPaymentsByLoanId(
                loanId,
                page,
                pageSize,
                status,
                userId);

        var totalPages = (int)Math.Ceiling(
            result.TotalRecords / (double)pageSize);

        return new PaymentHistoryDto
        {
            Payments = result.Payments
                .Select(payment => new PaymentHistoryItemDto
                {
                    PaymentId = payment.PaymentId,
                    LoanId = payment.LoanId,
                    Amount = payment.Amount,
                    Status = payment.Status ?? string.Empty,
                    PaymentDate = payment.PaymentDate
                })
                .ToList(),

            Page = page,
            PageSize = pageSize,
            TotalRecords = result.TotalRecords,
            TotalPages = totalPages
        };
    }

    public async Task<Payment> CreatePayment(
        CreatePaymentDto paymentDto,
        int userId)
    {
        if (paymentDto.Amount <= 0)
        {
            throw new ArgumentException(
                "Payment amount must be greater than zero.");
        }

        var loanBelongsToUser =
            await _paymentRepository.LoanBelongsToUser(
                paymentDto.LoanId,
                userId);

        if (!loanBelongsToUser)
        {
            throw new ArgumentException(
                "Loan does not exist or does not belong to the current user.");
        }

        var payment = new Payment
        {
            LoanId = paymentDto.LoanId,
            Amount = paymentDto.Amount,
            Status = "Completed",
            PaymentDate = DateTime.UtcNow
        };

        await _paymentRepository.AddPayment(payment);

        await _paymentRepository.SaveChanges();

        return payment;
    }
}