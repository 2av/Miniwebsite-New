namespace MiniWebsite.Application.Admin.RoleManagement.Dtos;

public class ManageRolesQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? SalesType { get; set; }
}

public record ManageRolesPageDto(
    IReadOnlyList<ManageRoleRowDto> Roles,
    int TotalCount,
    int Page,
    int PageSize);

public record ManageRoleRowDto(
    int RoleId,
    string RoleName,
    string RoleDescription,
    string Prefix,
    string SalesType);

public record UpsertRoleRequest(
    string RoleName,
    string? RoleDescription,
    string? Prefix,
    string? SalesType);

public record ManageRolesMetaDto(IReadOnlyList<RoleSalesTypeOptionDto> SalesTypes);

public record RoleSalesTypeOptionDto(string Value, string Label);
