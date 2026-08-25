using Homeowner360.Api.Models;

namespace Homeowner360.Api.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetCustomers(int userId);

    Task<Customer?> GetCustomerById(int id, int userId);

    Task<Customer> CreateCustomer(Customer customer);

    Task<Customer?> UpdateCustomer(
        int id,
        int userId,
        string name,
        string email);

    Task<bool> DeleteCustomer(int id, int userId);
}