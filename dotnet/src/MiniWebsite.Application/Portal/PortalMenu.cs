using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Portal;

/// <summary>
/// Menu assigned to each user-web role prefix. Page guards and portal APIs both use this list.
/// SBPU stays dashboard-only until its menu is confirmed.
/// Keep <c>user-web/src/shared/portal/menu.ts</c> in the same order.
/// </summary>
public static class PortalMenu
{
    public const string Dashboard = "dashboard";
    public const string Referral = "referral";
    public const string Sales = "sales";
    public const string Verification = "verification";
    public const string Customers = "customers";
    public const string Marketing = "marketing";
    public const string IdCard = "id-card";

    private static readonly string[] ShareBizzUser =
        [Dashboard, Referral, Customers, Verification, Marketing];

    /// <summary>ShareBizz Profile User — additional items are not assigned yet.</summary>
    private static readonly string[] ProfileUser = [Dashboard];

    private static readonly string[] Partner =
        [Dashboard, Verification, Referral, Customers, IdCard, Marketing];

    private static readonly string[] FieldSalesTeamMenu =
        [Dashboard, Sales, Customers, IdCard, Marketing];

    private static readonly Dictionary<string, string[]> ByPrefix = new(StringComparer.OrdinalIgnoreCase)
    {
        [RolePrefixes.ShareBizzUser] = ShareBizzUser,
        [RolePrefixes.ShareBizzProfileUser] = ProfileUser,
        [RolePrefixes.FranchisePartner] = Partner,
        [RolePrefixes.DigitalMarketingPartner] = Partner,
        [RolePrefixes.PrintShopPartner] = Partner,
        [RolePrefixes.SocialMediaPartner] = Partner,
        [RolePrefixes.FieldSalesPartner] = Partner,
        [RolePrefixes.FieldSalesTeam] = FieldSalesTeamMenu,
    };

    public static bool Allows(string? prefix, string feature)
    {
        if (string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(feature)) return false;
        return ByPrefix.TryGetValue(prefix.Trim(), out var items) && items.Contains(feature);
    }

    /// <summary>Referral Details and Sales Details share the referral APIs.</summary>
    public static bool AllowsReferralArea(string? prefix) =>
        Allows(prefix, Referral) || Allows(prefix, Sales);

    /// <summary>Field Sales Team sees Sales Details. Other types see Referral Details.</summary>
    public static bool IsSalesDetails(string? prefix) =>
        Allows(prefix, Sales) && !Allows(prefix, Referral);

    public static IReadOnlyList<string> FeaturesFor(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return [];
        return ByPrefix.TryGetValue(prefix.Trim(), out var items) ? items : [];
    }
}
