using Homeowner360.Api.DTOs;

namespace Homeowner360.Api.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboard(int userId);
}