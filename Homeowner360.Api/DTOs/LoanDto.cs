namespace Homeowner360.Api.DTOs;

public class LoanDto
{
    public int LoanId { get; set; }

    public int CustomerId { get; set; }

    public string LoanNumber { get; set; } = string.Empty;

    public decimal OriginalAmount { get; set; }

    public decimal CurrentBalance { get; set; }

    public decimal InterestRate { get; set; }
}