using MiniWebsite.Application.Admin.RoleManagement.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.RoleManagement;

public interface IAdminRoleManagementService
{
    ManageRolesMetaDto GetMeta();
    Task<ApiResult<ManageRolesPageDto>> ListAsync(ManageRolesQuery query, CancellationToken ct = default);
    Task<ApiResult<ManageRoleRowDto>> GetAsync(int roleId, CancellationToken ct = default);
    Task<ApiResult<ManageRoleRowDto>> CreateAsync(UpsertRoleRequest request, CancellationToken ct = default);
    Task<ApiResult<ManageRoleRowDto>> UpdateAsync(int roleId, UpsertRoleRequest request, CancellationToken ct = default);
    Task<ApiResult> DeleteAsync(int roleId, CancellationToken ct = default);
}
