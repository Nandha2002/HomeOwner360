using System.Net;
using System.Net.Http.Json;
using Homeowner360.Api.DTOs;

namespace Homeowner360.Api.Tests;

public class AuthIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ReturnsSuccessAndToken()
    {
        var username = $"testuser_{Guid.NewGuid():N}";
        var email = $"test_{Guid.NewGuid():N}@example.com";

        var request = new RegisterDto
        {
            Username = username,
            Email = email,
            Password = "Password123!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/register",
            request);

        var body = await response.Content.ReadAsStringAsync();

        Console.WriteLine(
            $"Register Status: {(int)response.StatusCode}");

        Console.WriteLine(
            $"Register Response: {body}");

        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected successful registration but received " +
            $"{(int)response.StatusCode}: {body}");

        var result =
            await response.Content
                .ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(result);
        Assert.False(
            string.IsNullOrWhiteSpace(result.Token));

        Assert.Equal(
            username,
            result.Username);

        Assert.Equal(
            "User",
            result.Role);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var request = new LoginDto
        {
            Username = $"doesnotexist_{Guid.NewGuid():N}",
            Password = "WrongPassword123!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            request);

        var body = await response.Content.ReadAsStringAsync();

        Console.WriteLine(
            $"Login Status: {(int)response.StatusCode}");

        Console.WriteLine(
            $"Login Response: {body}");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}