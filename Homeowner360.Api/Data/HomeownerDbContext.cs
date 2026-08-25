using Homeowner360.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Homeowner360.Api.Data;

public class HomeownerDbContext : DbContext
{
    public HomeownerDbContext(
        DbContextOptions<HomeownerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }

    public DbSet<Loan> Loans { get; set; }

    public DbSet<Payment> Payments { get; set; }

    public DbSet<User> Users { get; set; }


protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>()
        .HasOne<Customer>()
        .WithOne(customer => customer.User)
        .HasForeignKey<Customer>(customer => customer.UserId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Customer>()
        .HasMany(customer => customer.Loans)
        .WithOne(loan => loan.Customer)
        .HasForeignKey(loan => loan.CustomerId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<Loan>()
        .HasMany(loan => loan.Payments)
        .WithOne(payment => payment.Loan)
        .HasForeignKey(payment => payment.LoanId)
        .OnDelete(DeleteBehavior.Cascade);

    base.OnModelCreating(modelBuilder);
}
}