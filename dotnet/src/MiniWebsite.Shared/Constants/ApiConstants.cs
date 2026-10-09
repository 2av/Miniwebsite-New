namespace MiniWebsite.Shared.Constants;

public static class ApiConstants
{
    public const string ApiVersion = "v1";
    public const string ApiRoutePrefix = "api/v1";

    /// <summary>Role claim issued for admin-panel users; must match <c>UserRole.Admin.ToString()</c>.</summary>
    public const string AdminRole = "Admin";
}
