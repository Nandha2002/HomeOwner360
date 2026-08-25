namespace Homeowner360.Api.DTOs;

public class PaymentHistoryDto
{
    public List<PaymentHistoryItemDto> Payments { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalRecords { get; set; }

    public int TotalPages { get; set; }
}