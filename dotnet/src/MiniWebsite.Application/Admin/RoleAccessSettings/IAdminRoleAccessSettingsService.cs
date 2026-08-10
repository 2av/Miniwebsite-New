using MiniWebsite.Application.Admin.RoleAccessSettings.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.RoleAccessSettings;

public interface IAdminRoleAccessSettingsService
{
    Task<RoleAccessMatrixDto> GetMatrixAsync(CancellationToken ct = default);
    Task<ApiResult> UpdateSettingAsync(int settingId, UpdateRoleAccessSettingRequest request, CancellationToken ct = default);
}
