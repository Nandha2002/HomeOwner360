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

public class LoanIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LoanIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLoans_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/Loans");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetLoans_UserOnlySeesOwnLoans()
    {
        using var scope =
            _factory.Services.CreateScope();

        var authService =
            scope.ServiceProvider
                .GetRequiredService<IAuthService>();

        var context =
            scope.ServiceProvider
                .GetRequiredService<HomeownerDbContext>();

        // Create User A.
        var userAResult =
            await authService.Register(
                new RegisterDto
                {
                    Username = $"loanuserA_{Guid.NewGuid():N}",
                    Email = $"loanuserA_{Guid.NewGuid():N}@example.com",
                    Password = "Password123!"
                });

        var userA = await context.Users
            .SingleAsync(user =>
                user.Username == userAResult.Username);

        // Create User B.
        var userBResult =
            await authService.Register(
                new RegisterDto
                {
                    Username = $"loanuserB_{Guid.NewGuid():N}",
                    Email = $"loanuserB_{Guid.NewGuid():N}@example.com",
                    Password = "Password123!"
                });

        var userB = await context.Users
            .SingleAsync(user =>
                user.Username == userBResult.Username);

        // Create customers for each user.
        var customerA = new Customer
        {
            UserId = userA.UserId,
            Name = "Loan User A",
            Email = "loanusera@example.com"
        };

        var customerB = new Customer
        {
            UserId = userB.UserId,
            Name = "Loan User B",
            Email = "loanuserb@example.com"
        };

        context.Customers.AddRange(
            customerA,
            customerB);

        await context.SaveChangesAsync();

        // Create loans for each customer.
        context.Loans.AddRange(
            new Loan
            {
                CustomerId = customerA.CustomerId,
                LoanNumber = "LOAN-A-001",
                OriginalAmount = 300000,
                CurrentBalance = 250000,
                InterestRate = 5.25m
            },
            new Loan
            {
                CustomerId = customerB.CustomerId,
                LoanNumber = "LOAN-B-001",
                OriginalAmount = 400000,
                CurrentBalance = 350000,
                InterestRate = 5.75m
            });

        await context.SaveChangesAsync();

        // Authenticate as User A.
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                userAResult.Token);

        var response = await client.GetAsync(
            "/api/Loans");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var loans =
            await response.Content
                .ReadFromJsonAsync<List<LoanDto>>();

        Assert.NotNull(loans);

        Assert.Contains(
            loans,
            loan => loan.LoanNumber == "LOAN-A-001");

        Assert.DoesNotContain(
            loans,
            loan => loan.LoanNumber == "LOAN-B-001");
    }
    [Fact]
public async Task CreateLoan_AsRegularUser_ReturnsForbidden()
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
                Username = $"loanuser_{Guid.NewGuid():N}",
                Email = $"loanuser_{Guid.NewGuid():N}@example.com",
                Password = "Password123!"
            });

    var user = await context.Users
        .SingleAsync(user =>
            user.Username == authResult.Username);

    var customer = new Customer
    {
        UserId = user.UserId,
        Name = "Regular User Customer",
        Email = "regularuser@example.com"
    };

    context.Customers.Add(customer);

    await context.SaveChangesAsync();

    var client = _factory.CreateClient();

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            authResult.Token);

    var request = new CreateLoanDto
    {
        CustomerId = customer.CustomerId,
        LoanNumber = "REGULAR-LOAN-001",
        OriginalAmount = 250000,
        CurrentBalance = 250000,
        InterestRate = 5.5m
    };

    var response = await client.PostAsJsonAsync(
        "/api/Loans",
        request);

    Assert.Equal(
        HttpStatusCode.Forbidden,
        response.StatusCode);
}
[Fact]
public async Task CreateLoan_AsAdmin_ReturnsCreated()
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
                Username = $"admin_{Guid.NewGuid():N}",
                Email = $"admin_{Guid.NewGuid():N}@example.com",
                Password = "Password123!"
            });

    var user = await context.Users
        .SingleAsync(user =>
            user.Username == authResult.Username);

    user.Role = "Admin";

    var customer = new Customer
    {
        UserId = user.UserId,
        Name = "Admin Customer",
        Email = "admin.customer@example.com"
    };

    context.Customers.Add(customer);

    await context.SaveChangesAsync();

    var client = _factory.CreateClient();

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            authResult.Token);

    // Important: the token generated before changing the role
    // still contains "User". Generate a new token after changing
    // the role.
    authResult = await authService.Login(
        new LoginDto
        {
            Username = user.Username,
            Password = "Password123!"
        });

    Assert.NotNull(authResult);

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            authResult.Token);

    var request = new CreateLoanDto
    {
        CustomerId = customer.CustomerId,
        LoanNumber = "ADMIN-LOAN-001",
        OriginalAmount = 300000,
        CurrentBalance = 300000,
        InterestRate = 5.25m
    };

    var response = await client.PostAsJsonAsync(
        "/api/Loans",
        request);

    Assert.Equal(
        HttpStatusCode.Created,
        response.StatusCode);

    var loan =
        await response.Content
            .ReadFromJsonAsync<LoanDto>();

    Assert.NotNull(loan);

    Assert.Equal(
        "ADMIN-LOAN-001",
        loan.LoanNumber);

    Assert.Equal(
        customer.CustomerId,
        loan.CustomerId);
}
}