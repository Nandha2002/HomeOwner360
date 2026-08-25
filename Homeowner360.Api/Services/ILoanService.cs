using Homeowner360.Api.DTOs;
using Homeowner360.Api.Models;

namespace Homeowner360.Api.Services;

public interface ILoanService
{
    Task<List<Loan>> GetLoans(int userId);

    Task<Loan?> GetLoanById(
        int id,
        int userId);

    Task<Loan> CreateLoan(
        CreateLoanDto loanDto,
        int userId);

    Task<List<Payment>> GetLoanPayments(
        int loanId,
        int userId);

    Task<Loan?> UpdateLoan(
        int id,
        UpdateLoanDto loanDto,
        int userId);

    Task<bool> DeleteLoan(
        int id,
        int userId);

    Task<PagedResultDto<Loan>> SearchLoans(
        LoanQueryDto query,
        int userId);
}