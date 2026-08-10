using MiniWebsite.Application.Admin.ManageTeams.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.ManageTeams;

public interface IAdminManageTeamsService
{
    Task<ApiResult<ManageTeamsPageDto>> ListAsync(ManageTeamsQuery query, CancellationToken ct = default);
    Task<ApiResult<ManageTeamRowDto>> GetAsync(int id, CancellationToken ct = default);
    Task<ApiResult<ManageTeamRowDto>> CreateAsync(CreateTeamMemberRequest request, CancellationToken ct = default);
    Task<ApiResult<ManageTeamRowDto>> UpdateAsync(int id, UpdateTeamMemberRequest request, CancellationToken ct = default);
    Task<ApiResult<ManageTeamRowDto>> ToggleStatusAsync(int id, ToggleTeamStatusRequest request, CancellationToken ct = default);
    Task<ApiResult> ResetPasswordAsync(int id, ResetTeamPasswordRequest request, CancellationToken ct = default);
    Task<ApiResult<TeamReferralsDto>> GetReferralsAsync(int id, CancellationToken ct = default);
    Task<ApiResult<TeamTrackerDto>> GetTrackerAsync(int id, CancellationToken ct = default);
    Task<(byte[] Content, string FileName)?> ExportTrackerCsvAsync(int id, CancellationToken ct = default);
}
