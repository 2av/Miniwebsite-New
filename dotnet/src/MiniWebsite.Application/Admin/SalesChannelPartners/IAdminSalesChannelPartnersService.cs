using MiniWebsite.Application.Admin.SalesChannelPartners.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.SalesChannelPartners;

public interface IAdminSalesChannelPartnersService
{
    Task<ApiResult<IReadOnlyList<SalesChannelPartnerRoleDto>>> ListRolesAsync(CancellationToken ct = default);
    Task<ApiResult<SalesChannelPartnersPageDto>> ListAsync(string slug, SalesChannelPartnersQuery query, CancellationToken ct = default);
    Task<ApiResult<SalesChannelPartnerRowDto>> CreateAsync(string slug, CreateSalesChannelPartnerRequest request, CancellationToken ct = default);
    Task<ApiResult<SalesChannelPartnerRowDto>> UpdateAsync(string slug, int id, UpdateSalesChannelPartnerRequest request, CancellationToken ct = default);
    Task<ApiResult<SalesChannelPartnerRowDto>> ToggleStatusAsync(string slug, int id, ToggleSalesChannelPartnerStatusRequest request, CancellationToken ct = default);
    Task<ApiResult> ResetPasswordAsync(string slug, int id, ResetSalesChannelPartnerPasswordRequest request, CancellationToken ct = default);
}
