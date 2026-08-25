using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Homeowner360.Api.Data;
using Homeowner360.Api.DTOs;
using Homeowner360.Api.Models;
using Homeowner360.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Homeowner360.Api.Tests;

public class DashboardIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DashboardIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDashboard_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/Dashboard");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_WithAuthentication_ReturnsDashboard()
    {
        using var scope =
            _factory.Services.CreateScope();

        var authService =
            scope.ServiceProvider
                .GetRequiredService<IAuthService>();

        var context =
            scope.ServiceProvider
                .GetRequiredService<HomeownerDbContext>();

        var authResult =
            await authService.Register(
                new RegisterDto
                {
                    Username =
                        $"dashboard_{Guid.NewGuid():N}",

                    Email =
                        $"dashboard_{Guid.NewGuid():N}@example.com",

                    Password = "Password123!"
                });

        var user = await context.Users
            .SingleAsync(user =>
                user.Username == authResult.Username);

        var customer = new Customer
        {
            UserId = user.UserId,
            Name = "Dashboard Customer",
            Email = "dashboard.customer@example.com"
        };

        context.Customers.Add(customer);

        await context.SaveChangesAsync();

        var loan = new Loan
        {
            CustomerId = customer.CustomerId,
            LoanNumber = "DASHBOARD-001",
            OriginalAmount = 300000,
            CurrentBalance = 250000,
            InterestRate = 5.25m
        };

        context.Loans.Add(loan);

        await context.SaveChangesAsync();

        context.Payments.AddRange(
            new Payment
            {
                LoanId = loan.LoanId,
                Amount = 2500,
                Status = "Completed",
                PaymentDate = DateTime.UtcNow
            },
            new Payment
            {
                LoanId = loan.LoanId,
                Amount = 3000,
                Status = "Completed",
                PaymentDate = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                authResult.Token);

        var response = await client.GetAsync(
            "/api/Dashboard");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var dashboard =
            await response.Content
                .ReadFromJsonAsync<DashboardDto>();

        Assert.NotNull(dashboard);

        Assert.True(
            dashboard.TotalCustomers >= 1);

        Assert.True(
            dashboard.TotalLoans >= 1);

        Assert.True(
            dashboard.TotalPaymentsCount >= 2);

        Assert.True(
            dashboard.TotalLoanAmount >= 300000);

        Assert.True(
            dashboard.TotalOutstandingBalance >= 250000);

        Assert.True(
            dashboard.TotalPayments >= 5500);
    }
}
