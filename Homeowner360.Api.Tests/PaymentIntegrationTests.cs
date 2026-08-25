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

public class PaymentIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PaymentIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPayments_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/Payments");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetPayments_UserOnlySeesOwnPayments()
    {
        using var scope =
            _factory.Services.CreateScope();

        var authService =
            scope.ServiceProvider
                .GetRequiredService<IAuthService>();

        var context =
            scope.ServiceProvider
                .GetRequiredService<HomeownerDbContext>();

        var userAResult =
            await authService.Register(
                new RegisterDto
                {
                    Username = $"paymentA_{Guid.NewGuid():N}",
                    Email = $"paymentA_{Guid.NewGuid():N}@example.com",
                    Password = "Password123!"
                });

        var userBResult =
            await authService.Register(
                new RegisterDto
                {
                    Username = $"paymentB_{Guid.NewGuid():N}",
                    Email = $"paymentB_{Guid.NewGuid():N}@example.com",
                    Password = "Password123!"
                });

        var userA = await context.Users
            .SingleAsync(user =>
                user.Username == userAResult.Username);

        var userB = await context.Users
            .SingleAsync(user =>
                user.Username == userBResult.Username);

        var customerA = new Customer
        {
            UserId = userA.UserId,
            Name = "Payment Customer A",
            Email = "paymentA@example.com"
        };

        var customerB = new Customer
        {
            UserId = userB.UserId,
            Name = "Payment Customer B",
            Email = "paymentB@example.com"
        };

        context.Customers.AddRange(
            customerA,
            customerB);

        await context.SaveChangesAsync();

        var loanA = new Loan
        {
            CustomerId = customerA.CustomerId,
            LoanNumber = "PAYMENT-LOAN-A",
            OriginalAmount = 300000,
            CurrentBalance = 250000,
            InterestRate = 5.25m
        };

        var loanB = new Loan
        {
            CustomerId = customerB.CustomerId,
            LoanNumber = "PAYMENT-LOAN-B",
            OriginalAmount = 400000,
            CurrentBalance = 350000,
            InterestRate = 5.75m
        };

        context.Loans.AddRange(
            loanA,
            loanB);

        await context.SaveChangesAsync();

        context.Payments.AddRange(
            new Payment
            {
                LoanId = loanA.LoanId,
                Amount = 2500,
                Status = "Completed",
                PaymentDate = DateTime.UtcNow
            },
            new Payment
            {
                LoanId = loanB.LoanId,
                Amount = 3000,
                Status = "Completed",
                PaymentDate = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                userAResult.Token);

        var response = await client.GetAsync(
            "/api/Payments");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var payments =
            await response.Content
                .ReadFromJsonAsync<List<PaymentDto>>();

        Assert.NotNull(payments);

        Assert.Contains(
            payments,
            payment => payment.LoanId == loanA.LoanId);

        Assert.DoesNotContain(
            payments,
            payment => payment.LoanId == loanB.LoanId);
    }
    [Fact]
public async Task CreatePayment_AsRegularUser_ReturnsForbidden()
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
                Username = $"paymentuser_{Guid.NewGuid():N}",
                Email = $"paymentuser_{Guid.NewGuid():N}@example.com",
                Password = "Password123!"
            });

    var user = await context.Users
        .SingleAsync(user =>
            user.Username == authResult.Username);

    var customer = new Customer
    {
        UserId = user.UserId,
        Name = "Payment Test Customer",
        Email = "payment.test@example.com"
    };

    context.Customers.Add(customer);

    await context.SaveChangesAsync();

    var loan = new Loan
    {
        CustomerId = customer.CustomerId,
        LoanNumber = "PAYMENT-AUTH-001",
        OriginalAmount = 300000,
        CurrentBalance = 250000,
        InterestRate = 5.25m
    };

    context.Loans.Add(loan);

    await context.SaveChangesAsync();

    var client = _factory.CreateClient();

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            authResult.Token);

    var request = new CreatePaymentDto
    {
        LoanId = loan.LoanId,
        Amount = 2500
    };

    var response = await client.PostAsJsonAsync(
        "/api/Payments",
        request);

    Assert.Equal(
        HttpStatusCode.Forbidden,
        response.StatusCode);
}
[Fact]
public async Task CreatePayment_AsAdmin_ReturnsCreated()
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
                Username = $"paymentadmin_{Guid.NewGuid():N}",
                Email = $"paymentadmin_{Guid.NewGuid():N}@example.com",
                Password = "Password123!"
            });

    var user = await context.Users
        .SingleAsync(user =>
            user.Username == authResult.Username);

    user.Role = "Admin";

    var customer = new Customer
    {
        UserId = user.UserId,
        Name = "Payment Admin Customer",
        Email = "payment.admin@example.com"
    };

    context.Customers.Add(customer);

    await context.SaveChangesAsync();

    var loan = new Loan
    {
        CustomerId = customer.CustomerId,
        LoanNumber = "PAYMENT-ADMIN-001",
        OriginalAmount = 300000,
        CurrentBalance = 250000,
        InterestRate = 5.25m
    };

    context.Loans.Add(loan);

    await context.SaveChangesAsync();

    // Generate a new JWT containing the Admin role.
    var adminToken = await authService.Login(
        new LoginDto
        {
            Username = user.Username,
            Password = "Password123!"
        });

    Assert.NotNull(adminToken);

    var client = _factory.CreateClient();

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            adminToken.Token);

    var request = new CreatePaymentDto
    {
        LoanId = loan.LoanId,
        Amount = 2500
    };

    var response = await client.PostAsJsonAsync(
        "/api/Payments",
        request);

    Assert.Equal(
        HttpStatusCode.Created,
        response.StatusCode);

    var payment =
        await response.Content
            .ReadFromJsonAsync<PaymentDto>();

    Assert.NotNull(payment);

    Assert.Equal(
        loan.LoanId,
        payment.LoanId);

    Assert.Equal(
        2500,
        payment.Amount);

    Assert.Equal(
        "Completed",
        payment.Status);
}
}