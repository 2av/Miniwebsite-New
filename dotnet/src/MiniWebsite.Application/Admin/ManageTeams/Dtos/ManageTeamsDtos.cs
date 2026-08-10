namespace MiniWebsite.Application.Admin.ManageTeams.Dtos;

public class ManageTeamsQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Status { get; set; }
}

public record ManageTeamsPageDto(
    IReadOnlyList<ManageTeamRowDto> Members,
    int TotalCount,
    int Page,
    int PageSize);

public record ManageTeamRowDto(
    int Id,
    int? LegacyTeamId,
    string Name,
    string Email,
    string Phone,
    string District,
    string State,
    string Status,
    string StatusTone,
    string CreatedAtDisplay,
    int TotalSales,
    int TotalMwCreated,
    int ReferralCount,
    int TrackerCount,
    IReadOnlyList<int> OwnMwIds);

public record CreateTeamMemberRequest(
    string Name,
    string Email,
    string? Phone,
    string? District,
    string? State,
    string Password);

public record UpdateTeamMemberRequest(
    string Name,
    string Email,
    string? Phone,
    string? District,
    string? State);

public record ResetTeamPasswordRequest(string NewPassword);

public record ToggleTeamStatusRequest(string? NewStatus);

public record TeamReferralsDto(
    string MemberName,
    string MemberEmail,
    int TotalSales,
    int TotalMwCreated,
    IReadOnlyList<TeamReferralRowDto> Rows);

public record TeamReferralRowDto(
    int? UserId,
    string ReferredName,
    string ReferredEmail,
    string Phone,
    string Type,
    string MwPaymentStatus,
    string MwPaymentStatusTone,
    string PaidOnDisplay,
    string ReferralDateDisplay);

public record TeamTrackerDto(
    string MemberName,
    string MemberEmail,
    IReadOnlyList<TeamTrackerRowDto> Rows);

public record TeamTrackerRowDto(
    int Id,
    string ShopName,
    string ContactNumber,
    string ApproachedFor,
    string Address,
    string DateVisitedDisplay,
    string FinalStatus,
    string FinalStatusTone,
    string LastUpdatedDisplay);
