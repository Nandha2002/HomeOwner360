using Homeowner360.Api.Data;
using Homeowner360.Api.DTOs;
using Homeowner360.Api.Models;
using Homeowner360.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Homeowner360.Api.Tests;

public class LoanServiceTests
{
    private static HomeownerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HomeownerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HomeownerDbContext(options);
    }

    [Fact]
    public async Task GetLoans_ReturnsOnlyLoansBelongingToUser()
    {
        await using var context = CreateContext();

        var user1 = new User
        {
            UserId = 1,
            Username = "user1",
            Email = "user1@example.com",
            Role = "User"
        };

        var user2 = new User
        {
            UserId = 2,
            Username = "user2",
            Email = "user2@example.com",
            Role = "User"
        };

        var customer1 = new Customer
        {
            CustomerId = 1,
            UserId = 1,
            Name = "Customer One",
            Email = "customer1@example.com"
        };

        var customer2 = new Customer
        {
            CustomerId = 2,
            UserId = 2,
            Name = "Customer Two",
            Email = "customer2@example.com"
        };

        context.Users.AddRange(user1, user2);
        context.Customers.AddRange(customer1, customer2);

        context.Loans.AddRange(
            new Loan
            {
                LoanId = 1,
                CustomerId = 1,
                LoanNumber = "MTG-001",
                OriginalAmount = 450000,
                CurrentBalance = 423750,
                InterestRate = 5.25m
            },
            new Loan
            {
                LoanId = 2,
                CustomerId = 2,
                LoanNumber = "MTG-002",
                OriginalAmount = 300000,
                CurrentBalance = 280000,
                InterestRate = 4.75m
            });

        await context.SaveChangesAsync();

        var service = new LoanService(context);

        var loans = await service.GetLoans(1);

        Assert.Single(loans);
        Assert.Equal(1, loans[0].LoanId);
        Assert.Equal("MTG-001", loans[0].LoanNumber);
    }

    [Fact]
    public async Task CreateLoan_CreatesLoanForUserOwnedCustomer()
    {
        await using var context = CreateContext();

        context.Users.Add(new User
        {
            UserId = 1,
            Username = "nandhu",
            Email = "nandhu@example.com",
            Role = "Admin"
        });

        context.Customers.Add(new Customer
        {
            CustomerId = 1,
            UserId = 1,
            Name = "Homeowner",
            Email = "homeowner@example.com"
        });

        await context.SaveChangesAsync();

        var service = new LoanService(context);

        var dto = new CreateLoanDto
        {
            CustomerId = 1,
            LoanNumber = "MTG-TEST-001",
            OriginalAmount = 500000,
            CurrentBalance = 500000,
            InterestRate = 5.5m
        };

        var loan = await service.CreateLoan(dto, 1);

        Assert.NotEqual(0, loan.LoanId);
        Assert.Equal(1, loan.CustomerId);
        Assert.Equal("MTG-TEST-001", loan.LoanNumber);
        Assert.Equal(500000, loan.OriginalAmount);
        Assert.Equal(500000, loan.CurrentBalance);
        Assert.Equal(5.5m, loan.InterestRate);
    }

    [Fact]
    public async Task CreateLoan_RejectsCustomerOwnedByAnotherUser()
    {
        await using var context = CreateContext();

        context.Users.AddRange(
            new User
            {
                UserId = 1,
                Username = "user1",
                Email = "user1@example.com"
            },
            new User
            {
                UserId = 2,
                Username = "user2",
                Email = "user2@example.com"
            });

        context.Customers.Add(new Customer
        {
            CustomerId = 1,
            UserId = 2,
            Name = "Other User Customer",
            Email = "other@example.com"
        });

        await context.SaveChangesAsync();

        var service = new LoanService(context);

        var dto = new CreateLoanDto
        {
            CustomerId = 1,
            LoanNumber = "MTG-UNAUTHORIZED",
            OriginalAmount = 400000,
            CurrentBalance = 400000,
            InterestRate = 5.0m
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateLoan(dto, 1));

        Assert.Contains(
            "does not belong to the current user",
            exception.Message);
    }

    [Fact]
    public async Task GetLoanById_ReturnsLoanForCurrentUser()
    {
        await using var context = CreateContext();

        context.Users.Add(new User
        {
            UserId = 1,
            Username = "user1",
            Email = "user1@example.com"
        });

        context.Customers.Add(new Customer
        {
            CustomerId = 1,
            UserId = 1,
            Name = "Customer One",
            Email = "customer@example.com"
        });

        context.Loans.Add(new Loan
        {
            LoanId = 1,
            CustomerId = 1,
            LoanNumber = "MTG-001",
            OriginalAmount = 450000,
            CurrentBalance = 423750,
            InterestRate = 5.25m
        });

        await context.SaveChangesAsync();

        var service = new LoanService(context);

        var loan = await service.GetLoanById(1, 1);

        Assert.NotNull(loan);
        Assert.Equal(1, loan.LoanId);
        Assert.Equal("MTG-001", loan.LoanNumber);
    }

    [Fact]
    public async Task GetLoanById_DoesNotReturnAnotherUsersLoan()
    {
        await using var context = CreateContext();

        context.Users.AddRange(
            new User
            {
                UserId = 1,
                Username = "user1",
                Email = "user1@example.com"
            },
            new User
            {
                UserId = 2,
                Username = "user2",
                Email = "user2@example.com"
            });

        context.Customers.Add(new Customer
        {
            CustomerId = 1,
            UserId = 2,
            Name = "Other Customer",
            Email = "other@example.com"
        });

        context.Loans.Add(new Loan
        {
            LoanId = 1,
            CustomerId = 1,
            LoanNumber = "MTG-002",
            OriginalAmount = 300000,
            CurrentBalance = 290000,
            InterestRate = 4.5m
        });

        await context.SaveChangesAsync();

        var service = new LoanService(context);

        var loan = await service.GetLoanById(1, 1);

        Assert.Null(loan);
    }

    [Fact]
    public async Task DeleteLoan_DeletesLoanWithoutPayments()
    {
        await using var context = CreateContext();

        context.Users.Add(new User
        {
            UserId = 1,
            Username = "user1",
            Email = "user1@example.com"
        });

        context.Customers.Add(new Customer
        {
            CustomerId = 1,
            UserId = 1,
            Name = "Customer",
            Email = "customer@example.com"
        });

        context.Loans.Add(new Loan
        {
            LoanId = 1,
            CustomerId = 1,
            LoanNumber = "MTG-DELETE",
            OriginalAmount = 250000,
            CurrentBalance = 250000,
            InterestRate = 5.0m
        });

        await context.SaveChangesAsync();

        var service = new LoanService(context);

        var deleted = await service.DeleteLoan(1, 1);

        Assert.True(deleted);

        var loan = await context.Loans
            .FirstOrDefaultAsync(l => l.LoanId == 1);

        Assert.Null(loan);
    }

    [Fact]
    public async Task DeleteLoan_RejectsLoanWithPaymentHistory()
    {
        await using var context = CreateContext();

        context.Users.Add(new User
        {
            UserId = 1,
            Username = "user1",
            Email = "user1@example.com"
        });

        context.Customers.Add(new Customer
        {
            CustomerId = 1,
            UserId = 1,
            Name = "Customer",
            Email = "customer@example.com"
        });

        context.Loans.Add(new Loan
        {
            LoanId = 1,
            CustomerId = 1,
            LoanNumber = "MTG-PAYMENTS",
            OriginalAmount = 300000,
            CurrentBalance = 290000,
            InterestRate = 5.0m
        });

        context.Payments.Add(new Payment
        {
            PaymentId = 1,
            LoanId = 1,
            Amount = 2500,
            Status = "Completed",
            PaymentDate = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new LoanService(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteLoan(1, 1));

        Assert.Contains(
            "payment history",
            exception.Message);
    }
}