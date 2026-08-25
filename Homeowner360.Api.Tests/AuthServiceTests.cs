using Homeowner360.Api.Data;
using Homeowner360.Api.DTOs;
using Homeowner360.Api.Models;
using Homeowner360.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Homeowner360.Api.Tests;

public class AuthServiceTests
{
    private static HomeownerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HomeownerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HomeownerDbContext(options);
    }

    private static IConfiguration CreateConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] =
                "ThisIsATestJwtSecretKeyThatIsLongEnough123456789",
            ["Jwt:Issuer"] = "Homeowner360.Tests",
            ["Jwt:Audience"] = "Homeowner360.Tests.Client",
            ["Jwt:ExpirationMinutes"] = "60"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    [Fact]
    public async Task Register_CreatesUserAndReturnsToken()
    {
        await using var context = CreateContext();

        var service = new AuthService(
            context,
            CreateConfiguration());

        var dto = new RegisterDto
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "SecurePassword123!"
        };

        var result = await service.Register(dto);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("newuser", result.Username);
        Assert.Equal("User", result.Role);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == "newuser");

        Assert.NotNull(user);
        Assert.Equal("newuser@example.com", user.Email);
        Assert.NotEqual(
            "SecurePassword123!",
            user.PasswordHash);
    }

    [Fact]
    public async Task Register_HashesPassword()
    {
        await using var context = CreateContext();

        var service = new AuthService(
            context,
            CreateConfiguration());

        var dto = new RegisterDto
        {
            Username = "hashuser",
            Email = "hash@example.com",
            Password = "MyPassword123!"
        };

        await service.Register(dto);

        var user = await context.Users
            .FirstAsync(u => u.Username == "hashuser");

        Assert.NotEqual(
            dto.Password,
            user.PasswordHash);

        Assert.NotEmpty(user.PasswordHash);
    }

    [Fact]
    public async Task Register_RejectsDuplicateUsername()
    {
        await using var context = CreateContext();

        context.Users.Add(new User
        {
            UserId = 1,
            Username = "existing",
            Email = "existing@example.com",
            PasswordHash = "existing-hash",
            Role = "User"
        });

        await context.SaveChangesAsync();

        var service = new AuthService(
            context,
            CreateConfiguration());

        var dto = new RegisterDto
        {
            Username = "existing",
            Email = "different@example.com",
            Password = "Password123!"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.Register(dto));

        Assert.Contains(
            "already exists",
            exception.Message);
    }

    [Fact]
    public async Task Register_RejectsDuplicateEmail()
    {
        await using var context = CreateContext();

        context.Users.Add(new User
        {
            UserId = 1,
            Username = "existinguser",
            Email = "existing@example.com",
            PasswordHash = "existing-hash",
            Role = "User"
        });

        await context.SaveChangesAsync();

        var service = new AuthService(
            context,
            CreateConfiguration());

        var dto = new RegisterDto
        {
            Username = "differentuser",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.Register(dto));

        Assert.Contains(
            "already exists",
            exception.Message);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsToken()
    {
        await using var context = CreateContext();

        var service = new AuthService(
            context,
            CreateConfiguration());

        await service.Register(new RegisterDto
        {
            Username = "loginuser",
            Email = "login@example.com",
            Password = "CorrectPassword123!"
        });

        var result = await service.Login(
            new LoginDto
            {
                Username = "loginuser",
                Password = "CorrectPassword123!"
            });

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("loginuser", result.Username);
        Assert.Equal("User", result.Role);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsNull()
    {
        await using var context = CreateContext();

        var service = new AuthService(
            context,
            CreateConfiguration());

        await service.Register(new RegisterDto
        {
            Username = "loginuser",
            Email = "login@example.com",
            Password = "CorrectPassword123!"
        });

        var result = await service.Login(
            new LoginDto
            {
                Username = "loginuser",
                Password = "WrongPassword123!"
            });

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_WithUnknownUsername_ReturnsNull()
    {
        await using var context = CreateContext();

        var service = new AuthService(
            context,
            CreateConfiguration());

        var result = await service.Login(
            new LoginDto
            {
                Username = "doesnotexist",
                Password = "Password123!"
            });

        Assert.Null(result);
    }

    [Fact]
    public async Task Register_UsesUserRoleByDefault()
    {
        await using var context = CreateContext();

        var service = new AuthService(
            context,
            CreateConfiguration());

        var result = await service.Register(
            new RegisterDto
            {
                Username = "roleuser",
                Email = "role@example.com",
                Password = "Password123!"
            });

        Assert.Equal("User", result.Role);

        var user = await context.Users
            .FirstAsync(u => u.Username == "roleuser");

        Assert.Equal("User", user.Role);
    }

    [Fact]
    public async Task Login_ReturnsConfiguredExpirationTime()
    {
        await using var context = CreateContext();

        var service = new AuthService(
            context,
            CreateConfiguration());

        var result = await service.Register(
            new RegisterDto
            {
                Username = "expirationuser",
                Email = "expiration@example.com",
                Password = "Password123!"
            });

        var remainingTime =
            result.ExpiresAt - DateTime.UtcNow;

        Assert.True(
            remainingTime > TimeSpan.FromMinutes(59));

        Assert.True(
            remainingTime <= TimeSpan.FromMinutes(60));
    }
}