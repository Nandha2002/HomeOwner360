using Homeowner360.Api.Data;
using Homeowner360.Api.DTOs;
using Homeowner360.Api.Models;
using Homeowner360.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Homeowner360.Api.Tests;

public class CustomerServiceTests
{
    private static HomeownerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HomeownerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HomeownerDbContext(options);
    }

    private static async Task SeedUser(
        HomeownerDbContext context,
        int userId)
    {
        context.Users.Add(new User
        {
            UserId = userId,
            Username = $"user{userId}",
            Email = $"user{userId}@example.com",
            Role = "User"
        });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetCustomers_ReturnsOnlyCustomersBelongingToUser()
    {
        await using var context = CreateContext();

        await SeedUser(context, 1);
        await SeedUser(context, 2);

        context.Customers.AddRange(
            new Customer
            {
                CustomerId = 1,
                UserId = 1,
                Name = "Customer One",
                Email = "one@example.com"
            },
            new Customer
            {
                CustomerId = 2,
                UserId = 2,
                Name = "Customer Two",
                Email = "two@example.com"
            });

        await context.SaveChangesAsync();

        var service = new CustomerService(context);

        var customers = await service.GetCustomers(1);

        Assert.Single(customers);
        Assert.Equal(1, customers[0].CustomerId);
        Assert.Equal("Customer One", customers[0].Name);
    }

    [Fact]
    public async Task GetCustomerById_ReturnsCustomerForCurrentUser()
    {
        await using var context = CreateContext();

        await SeedUser(context, 1);

        context.Customers.Add(new Customer
        {
            CustomerId = 1,
            UserId = 1,
            Name = "Homeowner One",
            Email = "homeowner@example.com"
        });

        await context.SaveChangesAsync();

        var service = new CustomerService(context);

        var customer = await service.GetCustomerById(1, 1);

        Assert.NotNull(customer);
        Assert.Equal(1, customer.CustomerId);
        Assert.Equal("Homeowner One", customer.Name);
        Assert.Equal("homeowner@example.com", customer.Email);
    }

    [Fact]
    public async Task GetCustomerById_DoesNotReturnAnotherUsersCustomer()
    {
        await using var context = CreateContext();

        await SeedUser(context, 1);
        await SeedUser(context, 2);

        context.Customers.Add(new Customer
        {
            CustomerId = 1,
            UserId = 2,
            Name = "Other User",
            Email = "other@example.com"
        });

        await context.SaveChangesAsync();

        var service = new CustomerService(context);

        var customer = await service.GetCustomerById(1, 1);

        Assert.Null(customer);
    }

    [Fact]
    public async Task CreateCustomer_CreatesCustomerForUser()
    {
        await using var context = CreateContext();

        await SeedUser(context, 1);

        var service = new CustomerService(context);

        var customer = new Customer
        {
            UserId = 1,
            Name = "New Homeowner",
            Email = "new@example.com"
        };

        var createdCustomer =
            await service.CreateCustomer(customer);

        Assert.NotEqual(0, createdCustomer.CustomerId);
        Assert.Equal(1, createdCustomer.UserId);
        Assert.Equal("New Homeowner", createdCustomer.Name);
        Assert.Equal("new@example.com", createdCustomer.Email);
    }

    [Fact]
public async Task UpdateCustomer_UpdatesCustomerBelongingToUser()
{
    await using var context = CreateContext();

    await SeedUser(context, 1);

    context.Customers.Add(new Customer
    {
        CustomerId = 1,
        UserId = 1,
        Name = "Old Name",
        Email = "old@example.com"
    });

    await context.SaveChangesAsync();

    var service = new CustomerService(context);

    var updatedCustomer =
        await service.UpdateCustomer(
            1,
            1,
            "Updated Name",
            "updated@example.com");

    Assert.NotNull(updatedCustomer);

    Assert.Equal(
        "Updated Name",
        updatedCustomer.Name);

    Assert.Equal(
        "updated@example.com",
        updatedCustomer.Email);
}

    [Fact]
    public async Task UpdateCustomer_DoesNotUpdateAnotherUsersCustomer()
    {
        await using var context = CreateContext();

        await SeedUser(context, 1);
        await SeedUser(context, 2);

        context.Customers.Add(new Customer
        {
            CustomerId = 1,
            UserId = 2,
            Name = "Original Name",
            Email = "original@example.com"
        });

        await context.SaveChangesAsync();

        var service = new CustomerService(context);

        var updatedCustomer =
     await service.UpdateCustomer(
         1,
         1,
         "Updated Name",
         "updated@example.com");

        Assert.Null(updatedCustomer);

        var customer = await context.Customers
            .FirstAsync(c => c.CustomerId == 1);

        Assert.Equal("Original Name", customer.Name);
        Assert.Equal(
            "original@example.com",
            customer.Email);
    }

    [Fact]
    public async Task DeleteCustomer_DeletesCustomerBelongingToUser()
    {
        await using var context = CreateContext();

        await SeedUser(context, 1);

        context.Customers.Add(new Customer
        {
            CustomerId = 1,
            UserId = 1,
            Name = "Delete Me",
            Email = "delete@example.com"
        });

        await context.SaveChangesAsync();

        var service = new CustomerService(context);

        var deleted =
            await service.DeleteCustomer(1, 1);

        Assert.True(deleted);

        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == 1);

        Assert.Null(customer);
    }

    [Fact]
    public async Task DeleteCustomer_DoesNotDeleteAnotherUsersCustomer()
    {
        await using var context = CreateContext();

        await SeedUser(context, 1);
        await SeedUser(context, 2);

        context.Customers.Add(new Customer
        {
            CustomerId = 1,
            UserId = 2,
            Name = "Protected Customer",
            Email = "protected@example.com"
        });

        await context.SaveChangesAsync();

        var service = new CustomerService(context);

        var deleted =
            await service.DeleteCustomer(1, 1);

        Assert.False(deleted);

        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == 1);

        Assert.NotNull(customer);
    }
}