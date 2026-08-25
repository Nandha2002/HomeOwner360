using System.ComponentModel.DataAnnotations;

namespace Homeowner360.Api.DTOs;

public class UpdateCustomerDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;
}