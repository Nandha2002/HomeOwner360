namespace Homeowner360.Api.Models;

public class Loan
{
    public int LoanId { get; set; }

    public int CustomerId { get; set; }

    public string LoanNumber { get; set; } = string.Empty;

    public decimal OriginalAmount { get; set; }

    public decimal CurrentBalance { get; set; }

    public decimal InterestRate { get; set; }

    public Customer Customer { get; set; } = null!;

    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();
}