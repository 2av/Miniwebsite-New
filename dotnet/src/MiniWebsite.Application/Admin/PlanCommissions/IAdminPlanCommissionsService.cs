using MiniWebsite.Application.Admin.PlanCommissions.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.PlanCommissions;

public interface IAdminPlanCommissionsService
{
    Task<ApiResult<PlanCommissionsMetaDto>> GetMetaAsync(CancellationToken ct = default);
    Task<ApiResult<PlanCommissionsPageDto>> ListAsync(PlanCommissionsQuery query, CancellationToken ct = default);
    Task<ApiResult<PlanCommissionRowDto>> GetAsync(int id, CancellationToken ct = default);
    Task<ApiResult<IReadOnlyList<PlanCommissionRowDto>>> CreateAsync(UpsertPlanCommissionRequest request, CancellationToken ct = default);
    Task<ApiResult<PlanCommissionRowDto>> UpdateAsync(int id, UpsertPlanCommissionRequest request, CancellationToken ct = default);
    Task<ApiResult> ToggleStatusAsync(int id, CancellationToken ct = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken ct = default);
}
