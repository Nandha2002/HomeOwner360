namespace Homeowner360.Api.Models;

public class Customer
{
    public int CustomerId { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public ICollection<Loan> Loans { get; set; }
        = new List<Loan>();
}