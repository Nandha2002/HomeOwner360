namespace Homeowner360.Api.DTOs;

public class DashboardDto
{
    public int TotalCustomers { get; set; }

    public int TotalLoans { get; set; }

    public decimal TotalLoanAmount { get; set; }

    public decimal TotalOutstandingBalance { get; set; }

    public decimal TotalPayments { get; set; }

    public int TotalPaymentsCount { get; set; }
}