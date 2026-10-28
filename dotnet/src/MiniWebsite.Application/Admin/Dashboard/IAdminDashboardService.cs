using MiniWebsite.Application.Admin.Dashboard.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.Dashboard;

public interface IAdminDashboardService
{
    Task<ApiResult<AdminDashboardDto>> GetAsync(CancellationToken ct = default);
}
