namespace Homeowner360.Api.DTOs;

public class LoanQueryDto
{
    public int? CustomerId { get; set; }

    public string? LoanNumber { get; set; }

    public decimal? MinBalance { get; set; }

    public decimal? MaxBalance { get; set; }

    public string? SortBy { get; set; }

    public bool SortDescending { get; set; } = false;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}