using Homeowner360.Api.DTOs;

namespace Homeowner360.Api.Services;

public interface IAuthService
{
    Task<AuthResponseDto> Register(RegisterDto registerDto);

    Task<AuthResponseDto?> Login(LoginDto loginDto);
}