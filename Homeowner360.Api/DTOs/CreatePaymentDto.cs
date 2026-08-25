using System.ComponentModel.DataAnnotations;

namespace Homeowner360.Api.DTOs;

public class CreatePaymentDto
{
    [Range(1, int.MaxValue)]
    public int LoanId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}