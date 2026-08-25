using Homeowner360.Api.Data;
using Homeowner360.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Homeowner360.Api.Services;

public class CustomerService : ICustomerService
{
    private readonly HomeownerDbContext _context;

    public CustomerService(HomeownerDbContext context)
    {
        _context = context;
    }
    public async Task<Customer> CreateCustomer(Customer customer)
    {
        _context.Customers.Add(customer);

        await _context.SaveChangesAsync();

        return customer;
    }
    public async Task<List<Customer>> GetCustomers(int userId)
    {
        return await _context.Customers
            .AsNoTracking()
            .Where(customer => customer.UserId == userId)
            .ToListAsync();
    }

    public async Task<Customer?> GetCustomerById(
        int id,
        int userId)
    {
        return await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(customer =>
                customer.CustomerId == id &&
                customer.UserId == userId);
    }
    public async Task<Customer?> UpdateCustomer(
        int id,
        int userId,
        string name,
        string email)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(customer =>
                customer.CustomerId == id &&
                customer.UserId == userId);

        if (customer == null)
        {
            return null;
        }

        customer.Name = name;
        customer.Email = email;

        await _context.SaveChangesAsync();

        return customer;
    }
    public async Task<bool> DeleteCustomer(
        int id,
        int userId)
    {
        var customer = await _context.Customers
            .Include(customer => customer.Loans)
            .FirstOrDefaultAsync(customer =>
                customer.CustomerId == id &&
                customer.UserId == userId);

        if (customer == null)
        {
            return false;
        }

        if (customer.Loans.Any())
        {
            throw new InvalidOperationException(
                "Customer cannot be deleted because they have existing loans.");
        }

        _context.Customers.Remove(customer);

        await _context.SaveChangesAsync();

        return true;
    }
}