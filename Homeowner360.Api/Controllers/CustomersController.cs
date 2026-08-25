using System.Security.Claims;
using Homeowner360.Api.DTOs;
using Homeowner360.Api.Models;
using Homeowner360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeowner360.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    // GET: api/Customers
    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetCustomers()
    {
        var userId = GetCurrentUserId();

        var customers = await _customerService
            .GetCustomers(userId);

        var customerDtos = customers.Select(customer => new CustomerDto
        {
            CustomerId = customer.CustomerId,
            Name = customer.Name,
            Email = customer.Email
        }).ToList();

        return Ok(customerDtos);
    }

    // GET: api/Customers/1
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
    {
        var userId = GetCurrentUserId();

        var customer = await _customerService
            .GetCustomerById(id, userId);

        if (customer == null)
        {
            return NotFound(new
            {
                message = $"Customer with ID {id} was not found."
            });
        }

        var customerDto = new CustomerDto
        {
            CustomerId = customer.CustomerId,
            Name = customer.Name,
            Email = customer.Email
        };

        return Ok(customerDto);
    }

    // POST: api/Customers
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer(
        CreateCustomerDto customerDto)
    {
        var userId = GetCurrentUserId();

        var customer = new Customer
        {
            UserId = userId,
            Name = customerDto.Name,
            Email = customerDto.Email
        };

        var createdCustomer =
            await _customerService.CreateCustomer(customer);

        var response = new CustomerDto
        {
            CustomerId = createdCustomer.CustomerId,
            Name = createdCustomer.Name,
            Email = createdCustomer.Email
        };

        return CreatedAtAction(
            nameof(GetCustomer),
            new { id = response.CustomerId },
            response);
    }

    // PUT: api/Customers/1
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(
        int id,
        UpdateCustomerDto customerDto)
    {
        var userId = GetCurrentUserId();

        var updatedCustomer =
            await _customerService.UpdateCustomer(
                id,
                userId,
                customerDto.Name,
                customerDto.Email);

        if (updatedCustomer == null)
        {
            return NotFound(new
            {
                message = $"Customer with ID {id} was not found."
            });
        }

        var response = new CustomerDto
        {
            CustomerId = updatedCustomer.CustomerId,
            Name = updatedCustomer.Name,
            Email = updatedCustomer.Email
        };

        return Ok(response);
    }

    // DELETE: api/Customers/1
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var userId = GetCurrentUserId();

        var deleted = await _customerService
            .DeleteCustomer(id, userId);

        if (!deleted)
        {
            return NotFound(new
            {
                message = $"Customer with ID {id} was not found."
            });
        }

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(
            ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            throw new UnauthorizedAccessException(
                "User identity could not be determined.");
        }

        if (!int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException(
                "Invalid user identity.");
        }

        return userId;
    }
}