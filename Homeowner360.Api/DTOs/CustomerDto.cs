namespace Homeowner360.Api.DTOs;

public class CustomerDto
{
    public int CustomerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}