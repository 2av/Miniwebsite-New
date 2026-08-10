namespace MiniWebsite.Application.Admin.RoleAccessSettings.Dtos;

public record RoleAccessProfileDto(
    int Id,
    string ProfileKey,
    string ProfileLabel,
    string BaseRole,
    string RequiresCollaboration,
    string RequiresInfluencer,
    int SortOrder);

public record RoleAccessFeatureDto(
    int Id,
    string FeatureKey,
    string FeatureLabel,
    string FeatureGroup,
    string FieldType,
    int SortOrder);

public record RoleAccessFeatureGroupDto(
    string Name,
    IReadOnlyList<RoleAccessFeatureDto> Features);

public record RoleAccessCellDto(
    int SettingId,
    string ProfileKey,
    string FeatureKey,
    bool IsNotApplicable,
    string SettingValue);

public record RoleAccessMatrixDto(
    bool TablesExist,
    IReadOnlyList<RoleAccessProfileDto> Profiles,
    IReadOnlyList<RoleAccessFeatureGroupDto> FeatureGroups,
    IReadOnlyList<RoleAccessCellDto> Cells);

public record UpdateRoleAccessSettingRequest(
    bool IsNotApplicable,
    string? SettingValue,
    string? UpdatedBy);
