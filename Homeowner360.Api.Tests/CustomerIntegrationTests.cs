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

public class CustomerIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCustomers_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/Customers");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCustomers_WithAuthentication_ReturnsCustomers()
    {
        using var scope =
            _factory.Services.CreateScope();

        var authService =
            scope.ServiceProvider
                .GetRequiredService<IAuthService>();

        var context =
            scope.ServiceProvider
                .GetRequiredService<HomeownerDbContext>();

        var username =
            $"integration_{Guid.NewGuid():N}";

        var email =
            $"integration_{Guid.NewGuid():N}@example.com";

        // Create a user through the real authentication service.
        var authResult =
            await authService.Register(
                new RegisterDto
                {
                    Username = username,
                    Email = email,
                    Password = "Password123!"
                });

        // Retrieve the newly created user.
        var user = await context.Users
            .SingleAsync(user =>
                user.Username == username);

        // Create a customer belonging to that user.
        var customer = new Customer
        {
            UserId = user.UserId,
            Name = "Integration Test Customer",
            Email = "integration.customer@example.com"
        };

        context.Customers.Add(customer);

        await context.SaveChangesAsync();

        // Create an HTTP client with the JWT.
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                authResult.Token);

        // Call the protected endpoint.
        var response = await client.GetAsync(
            "/api/Customers");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var customers =
            await response.Content
                .ReadFromJsonAsync<List<CustomerDto>>();

        Assert.NotNull(customers);
        Assert.Single(customers);

        Assert.Equal(
            "Integration Test Customer",
            customers[0].Name);

        Assert.Equal(
            "integration.customer@example.com",
            customers[0].Email);
    }
    [Fact]
public async Task GetCustomers_UserCannotSeeAnotherUsersCustomers()
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
                Username = $"userA_{Guid.NewGuid():N}",
                Email = $"userA_{Guid.NewGuid():N}@example.com",
                Password = "Password123!"
            });

    var userA = await context.Users
        .SingleAsync(user =>
            user.Username ==
            userAResult.Username);

    // Create User B.
    var userBResult =
        await authService.Register(
            new RegisterDto
            {
                Username = $"userB_{Guid.NewGuid():N}",
                Email = $"userB_{Guid.NewGuid():N}@example.com",
                Password = "Password123!"
            });

    var userB = await context.Users
        .SingleAsync(user =>
            user.Username ==
            userBResult.Username);

    // Create a customer for User A.
    context.Customers.Add(new Customer
    {
        UserId = userA.UserId,
        Name = "User A Customer",
        Email = "usera.customer@example.com"
    });

    // Create a customer for User B.
    context.Customers.Add(new Customer
    {
        UserId = userB.UserId,
        Name = "User B Customer",
        Email = "userb.customer@example.com"
    });

    await context.SaveChangesAsync();

    // Authenticate as User A.
    var client = _factory.CreateClient();

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue(
            "Bearer",
            userAResult.Token);

    // User A requests customers.
    var response = await client.GetAsync(
        "/api/Customers");

    Assert.Equal(
        HttpStatusCode.OK,
        response.StatusCode);

    var customers =
        await response.Content
            .ReadFromJsonAsync<List<CustomerDto>>();

    Assert.NotNull(customers);

    // User A should see their own customer.
    Assert.Contains(
        customers,
        customer =>
            customer.Name == "User A Customer");

    // User A must NOT see User B's customer.
    Assert.DoesNotContain(
        customers,
        customer =>
            customer.Name == "User B Customer");
}
}