namespace Homeowner360.Api.Models;

public class Payment
{
    public int PaymentId { get; set; }

    public int LoanId { get; set; }

    public decimal Amount { get; set; }

    public string? Status { get; set; }

    public DateTime PaymentDate { get; set; }

    public Loan Loan { get; set; } = null!;
}