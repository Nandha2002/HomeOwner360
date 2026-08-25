namespace Homeowner360.Api.DTOs;

public class PaymentDto
{
    public int PaymentId { get; set; }

    public int LoanId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime PaymentDate { get; set; }
}