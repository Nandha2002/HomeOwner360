using Homeowner360.Api.DTOs;
using Homeowner360.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Homeowner360.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(
        RegisterDto registerDto)
    {
        var result = await _authService.Register(registerDto);

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        LoginDto loginDto)
    {
        var result = await _authService.Login(loginDto);

        if (result == null)
        {
            return Unauthorized(new
            {
                message = "Invalid username or password."
            });
        }

        return Ok(result);
    }
}