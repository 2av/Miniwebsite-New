using MiniWebsite.Domain.Enums;

namespace MiniWebsite.Domain.Common;

/// <summary>
/// <c>roles.Prefix</c> values the code depends on. The prefix decides which portal
/// (<see cref="UserRole"/>) a user signs into; any prefix not listed here behaves as a customer.
/// </summary>
public static class RolePrefixes
{
    public const string ShareBizzUser = "SBU";
    public const string ShareBizzProfileUser = "SBPU";
    public const string FranchisePartner = "FRP";
    public const string DigitalMarketingPartner = "DMP";
    public const string PrintShopPartner = "PSP";
    public const string SocialMediaPartner = "SMP";
    public const string FieldSalesTeam = "FST";
    public const string FieldSalesPartner = "FSP";
    public const string Administrator = "ADM";

    public static readonly string[] Franchisee = [FranchisePartner];

    public static readonly string[] Team =
    [
        DigitalMarketingPartner,
        PrintShopPartner,
        SocialMediaPartner,
        FieldSalesTeam,
        FieldSalesPartner,
    ];

    public static readonly string[] Admin = [Administrator];

    /// <summary>Prefixes that sign into a non-customer portal.</summary>
    public static readonly string[] NonCustomer = [.. Franchisee, .. Team, .. Admin];

    public static readonly string[] All = [ShareBizzUser, ShareBizzProfileUser, .. NonCustomer];

    public static UserRole ToUserRole(string? prefix)
    {
        var p = (prefix ?? "").Trim().ToUpperInvariant();
        if (Franchisee.Contains(p)) return UserRole.Franchisee;
        if (Team.Contains(p)) return UserRole.Team;
        if (Admin.Contains(p)) return UserRole.Admin;
        return UserRole.Customer;
    }

    /// <summary>Role given to users created by flows that only know the portal type.</summary>
    public static string DefaultFor(UserRole role) => role switch
    {
        UserRole.Franchisee => FranchisePartner,
        UserRole.Team => DigitalMarketingPartner,
        UserRole.Admin => Administrator,
        _ => ShareBizzUser,
    };
}
