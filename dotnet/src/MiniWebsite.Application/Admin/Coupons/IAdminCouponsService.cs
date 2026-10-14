using MiniWebsite.Application.Admin.Coupons.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.Coupons;

public interface IAdminCouponsService
{
    Task<ApiResult<CouponsMetaDto>> GetMetaAsync(CancellationToken ct = default);
    Task<ApiResult<CouponsPageDto>> ListAsync(CouponsQuery query, CancellationToken ct = default);
    Task<ApiResult<CouponRowDto>> GetAsync(int id, CancellationToken ct = default);
    Task<ApiResult<CouponCodeDto>> GenerateCodeAsync(CancellationToken ct = default);
    Task<ApiResult<CouponRowDto>> CreateAsync(UpsertCouponRequest request, CancellationToken ct = default);
    Task<ApiResult<CouponRowDto>> UpdateAsync(int id, UpsertCouponRequest request, CancellationToken ct = default);
    Task<ApiResult> DeleteAsync(int id, CancellationToken ct = default);
}
