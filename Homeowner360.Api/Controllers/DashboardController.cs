using System.Security.Claims;
using Homeowner360.Api.DTOs;
using Homeowner360.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeowner360.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(
        IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var userIdClaim = User.FindFirst(
            ClaimTypes.NameIdentifier);

        if (userIdClaim == null ||
            !int.TryParse(
                userIdClaim.Value,
                out var userId))
        {
            return Unauthorized(new
            {
                message =
                    "User identity could not be determined."
            });
        }

        var dashboard =
            await _dashboardService.GetDashboard(userId);

        return Ok(dashboard);
    }
}