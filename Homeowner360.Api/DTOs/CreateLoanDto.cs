using System.ComponentModel.DataAnnotations;

namespace Homeowner360.Api.DTOs;

public class CreateLoanDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    [Required]
    [StringLength(50)]
    public string LoanNumber { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal OriginalAmount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CurrentBalance { get; set; }

    [Range(0, 100)]
    public decimal InterestRate { get; set; }
}