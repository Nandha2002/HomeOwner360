using Homeowner360.Api.Data;
using Homeowner360.Api.DTOs;
using Homeowner360.Api.Models;
using Homeowner360.Api.Repositories;
using Homeowner360.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Homeowner360.Api.Tests;

public class PaymentServiceTests
{
    private static HomeownerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HomeownerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HomeownerDbContext(options);
    }

    private static async Task SeedUserWithLoan(
        HomeownerDbContext context,
        int userId = 1,
        int customerId = 1,
        int loanId = 1)
    {
        context.Users.Add(new User
        {
            UserId = userId,
            Username = $"user{userId}",
            Email = $"user{userId}@example.com",
            Role = "User"
        });

        context.Customers.Add(new Customer
        {
            CustomerId = customerId,
            UserId = userId,
            Name = $"Customer {customerId}",
            Email = $"customer{customerId}@example.com"
        });

        context.Loans.Add(new Loan
        {
            LoanId = loanId,
            CustomerId = customerId,
            LoanNumber = $"MTG-{loanId:000}",
            OriginalAmount = 450000,
            CurrentBalance = 425000,
            InterestRate = 5.25m
        });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPayments_ReturnsOnlyPaymentsBelongingToUser()
    {
        await using var context = CreateContext();

        await SeedUserWithLoan(context);

        context.Users.Add(new User
        {
            UserId = 2,
            Username = "user2",
            Email = "user2@example.com",
            Role = "User"
        });

        context.Customers.Add(new Customer
        {
            CustomerId = 2,
            UserId = 2,
            Name = "Customer Two",
            Email = "customer2@example.com"
        });

        context.Loans.Add(new Loan
        {
            LoanId = 2,
            CustomerId = 2,
            LoanNumber = "MTG-002",
            OriginalAmount = 300000,
            CurrentBalance = 290000,
            InterestRate = 4.75m
        });

        context.Payments.AddRange(
            new Payment
            {
                PaymentId = 1,
                LoanId = 1,
                Amount = 2500,
                Status = "Completed",
                PaymentDate = DateTime.UtcNow
            },
            new Payment
            {
                PaymentId = 2,
                LoanId = 2,
                Amount = 3000,
                Status = "Completed",
                PaymentDate = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var repository = new PaymentRepository(context);
        var service = new PaymentService(repository);

        var payments = await service.GetPayments(1);

        Assert.Single(payments);
        Assert.Equal(1, payments[0].PaymentId);
        Assert.Equal(2500, payments[0].Amount);
    }

    [Fact]
    public async Task GetPaymentById_ReturnsPaymentForCurrentUser()
    {
        await using var context = CreateContext();

        await SeedUserWithLoan(context);

        context.Payments.Add(new Payment
        {
            PaymentId = 1,
            LoanId = 1,
            Amount = 2500,
            Status = "Completed",
            PaymentDate = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var repository = new PaymentRepository(context);
        var service = new PaymentService(repository);

        var payment = await service.GetPaymentById(1, 1);

        Assert.NotNull(payment);
        Assert.Equal(1, payment.PaymentId);
        Assert.Equal(2500, payment.Amount);
    }

    [Fact]
    public async Task GetPaymentById_DoesNotReturnAnotherUsersPayment()
    {
        await using var context = CreateContext();

        await SeedUserWithLoan(
            context,
            userId: 2,
            customerId: 2,
            loanId: 2);

        context.Payments.Add(new Payment
        {
            PaymentId = 1,
            LoanId = 2,
            Amount = 2500,
            Status = "Completed",
            PaymentDate = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var repository = new PaymentRepository(context);
        var service = new PaymentService(repository);

        var payment = await service.GetPaymentById(1, 1);

        Assert.Null(payment);
    }

    [Fact]
    public async Task CreatePayment_CreatesCompletedPayment()
    {
        await using var context = CreateContext();

        await SeedUserWithLoan(context);

        var repository = new PaymentRepository(context);
        var service = new PaymentService(repository);

        var dto = new CreatePaymentDto
        {
            LoanId = 1,
            Amount = 2500
        };

        var payment = await service.CreatePayment(dto, 1);

        Assert.NotEqual(0, payment.PaymentId);
        Assert.Equal(1, payment.LoanId);
        Assert.Equal(2500, payment.Amount);
        Assert.Equal("Completed", payment.Status);
    }

    [Fact]
    public async Task CreatePayment_RejectsZeroAmount()
    {
        await using var context = CreateContext();

        await SeedUserWithLoan(context);

        var repository = new PaymentRepository(context);
        var service = new PaymentService(repository);

        var dto = new CreatePaymentDto
        {
            LoanId = 1,
            Amount = 0
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreatePayment(dto, 1));

        Assert.Contains(
            "greater than zero",
            exception.Message);
    }

    [Fact]
    public async Task CreatePayment_RejectsNegativeAmount()
    {
        await using var context = CreateContext();

        await SeedUserWithLoan(context);

        var repository = new PaymentRepository(context);
        var service = new PaymentService(repository);

        var dto = new CreatePaymentDto
        {
            LoanId = 1,
            Amount = -100
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreatePayment(dto, 1));

        Assert.Contains(
            "greater than zero",
            exception.Message);
    }

    [Fact]
    public async Task GetPaymentsByLoanId_ReturnsOnlyCurrentUsersPayments()
    {
        await using var context = CreateContext();

        await SeedUserWithLoan(context);

        context.Payments.AddRange(
            new Payment
            {
                PaymentId = 1,
                LoanId = 1,
                Amount = 2500,
                Status = "Completed",
                PaymentDate = DateTime.UtcNow.AddDays(-2)
            },
            new Payment
            {
                PaymentId = 2,
                LoanId = 1,
                Amount = 2700,
                Status = "Completed",
                PaymentDate = DateTime.UtcNow.AddDays(-1)
            });

        await context.SaveChangesAsync();

        var repository = new PaymentRepository(context);
        var service = new PaymentService(repository);

        var result = await service.GetPaymentsByLoanId(
            1,
            1,
            10,
            null,
            1);

        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(2, result.Payments.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetPaymentsByLoanId_FiltersByStatus()
    {
        await using var context = CreateContext();

        await SeedUserWithLoan(context);

        context.Payments.AddRange(
            new Payment
            {
                PaymentId = 1,
                LoanId = 1,
                Amount = 2500,
                Status = "Completed",
                PaymentDate = DateTime.UtcNow.AddDays(-2)
            },
            new Payment
            {
                PaymentId = 2,
                LoanId = 1,
                Amount = 1000,
                Status = "Pending",
                PaymentDate = DateTime.UtcNow.AddDays(-1)
            });

        await context.SaveChangesAsync();

        var repository = new PaymentRepository(context);
        var service = new PaymentService(repository);

        var result = await service.GetPaymentsByLoanId(
            1,
            1,
            10,
            "Completed",
            1);

        Assert.Equal(1, result.TotalRecords);
        Assert.Single(result.Payments);
        Assert.Equal("Completed", result.Payments[0].Status);
    }

    [Fact]
    public async Task GetPaymentsByLoanId_RespectsPagination()
    {
        await using var context = CreateContext();

        await SeedUserWithLoan(context);

        for (var i = 1; i <= 5; i++)
        {
            context.Payments.Add(new Payment
            {
                PaymentId = i,
                LoanId = 1,
                Amount = i * 100,
                Status = "Completed",
                PaymentDate = DateTime.UtcNow.AddDays(-i)
            });
        }

        await context.SaveChangesAsync();

        var repository = new PaymentRepository(context);
        var service = new PaymentService(repository);

        var result = await service.GetPaymentsByLoanId(
            1,
            2,
            2,
            null,
            1);

        Assert.Equal(5, result.TotalRecords);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Payments.Count);
    }
}