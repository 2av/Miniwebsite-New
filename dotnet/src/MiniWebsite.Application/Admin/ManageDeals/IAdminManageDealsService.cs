using MiniWebsite.Application.Admin.ManageDeals.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.ManageDeals;

public interface IAdminManageDealsService
{
    Task<ApiResult<ManageDealsPageDto>> ListAsync(ManageDealsQuery query, CancellationToken ct = default);
    Task<ApiResult<ManageDealRowDto>> GetAsync(int id, CancellationToken ct = default);
    Task<ApiResult<ManageDealRowDto>> CreateAsync(UpsertDealRequest request, CancellationToken ct = default);
    Task<ApiResult<ManageDealRowDto>> UpdateAsync(int id, UpsertDealRequest request, CancellationToken ct = default);
    Task<ApiResult> ToggleStatusAsync(int id, CancellationToken ct = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken ct = default);
    ManageDealsMetaDto GetMeta();
}
