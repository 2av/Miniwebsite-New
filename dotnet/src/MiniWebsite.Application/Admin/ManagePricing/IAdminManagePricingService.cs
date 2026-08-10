using MiniWebsite.Application.Admin.ManagePricing.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.ManagePricing;

public interface IAdminManagePricingService
{
    Task<ApiResult<ManagePricingMetaDto>> GetMetaAsync(CancellationToken ct = default);
    Task<ApiResult<ManagePricingPageDto>> ListAsync(ManagePricingQuery query, CancellationToken ct = default);
    Task<ApiResult<ManagePricingRowDto>> GetAsync(int id, CancellationToken ct = default);
    Task<ApiResult<ManagePricingRowDto>> CreateAsync(UpsertPricingRequest request, CancellationToken ct = default);
    Task<ApiResult<ManagePricingRowDto>> UpdateAsync(int id, UpsertPricingRequest request, CancellationToken ct = default);
    Task<ApiResult> ToggleStatusAsync(int id, CancellationToken ct = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken ct = default);
}
