using MiniWebsite.Application.Admin.Franchisees.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.Franchisees;

public interface IAdminFranchiseesService
{
    Task<ApiResult<FranchiseePageDto>> ListAsync(FranchiseeQuery query, CancellationToken ct = default);
    Task<ApiResult> ActivateAsync(ActivateFranchiseeRequest request, CancellationToken ct = default);
    Task<ApiResult> CreateAsync(CreateFranchiseeRequest request, CancellationToken ct = default);
    Task<ApiResult<FranchiseeDashboardDto>> GetDashboardAsync(string email, CancellationToken ct = default);
}
