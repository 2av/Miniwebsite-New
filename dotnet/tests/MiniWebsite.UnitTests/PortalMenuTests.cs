using MiniWebsite.Application.Portal;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.UnitTests;

public class PortalMenuTests
{
    [Fact]
    public void ShareBizzUser_matches_assigned_menu()
    {
        Assert.Equal(
            [PortalMenu.Dashboard, PortalMenu.Referral, PortalMenu.Customers, PortalMenu.Verification, PortalMenu.Marketing],
            PortalMenu.FeaturesFor(RolePrefixes.ShareBizzUser));
    }

    [Fact]
    public void ProfileUser_is_dashboard_only_until_confirmed()
    {
        Assert.Equal([PortalMenu.Dashboard], PortalMenu.FeaturesFor(RolePrefixes.ShareBizzProfileUser));
        Assert.False(PortalMenu.Allows(RolePrefixes.ShareBizzProfileUser, PortalMenu.Referral));
        Assert.False(PortalMenu.Allows(RolePrefixes.ShareBizzProfileUser, PortalMenu.IdCard));
    }

    [Theory]
    [InlineData(RolePrefixes.FranchisePartner)]
    [InlineData(RolePrefixes.DigitalMarketingPartner)]
    [InlineData(RolePrefixes.PrintShopPartner)]
    [InlineData(RolePrefixes.SocialMediaPartner)]
    [InlineData(RolePrefixes.FieldSalesPartner)]
    public void Partners_share_the_partner_menu(string prefix)
    {
        Assert.Equal(
            [PortalMenu.Dashboard, PortalMenu.Verification, PortalMenu.Referral, PortalMenu.Customers, PortalMenu.IdCard, PortalMenu.Marketing],
            PortalMenu.FeaturesFor(prefix));
        Assert.False(PortalMenu.IsSalesDetails(prefix));
    }

    [Fact]
    public void FieldSalesTeam_sees_sales_details_without_verification_or_referral()
    {
        Assert.Equal(
            [PortalMenu.Dashboard, PortalMenu.Sales, PortalMenu.Customers, PortalMenu.IdCard, PortalMenu.Marketing],
            PortalMenu.FeaturesFor(RolePrefixes.FieldSalesTeam));
        Assert.True(PortalMenu.IsSalesDetails(RolePrefixes.FieldSalesTeam));
        Assert.True(PortalMenu.AllowsReferralArea(RolePrefixes.FieldSalesTeam));
        Assert.False(PortalMenu.Allows(RolePrefixes.FieldSalesTeam, PortalMenu.Verification));
        Assert.False(PortalMenu.Allows(RolePrefixes.FieldSalesTeam, PortalMenu.Referral));
    }

    [Fact]
    public void ShareBizzUser_cannot_open_id_card()
    {
        Assert.False(PortalMenu.Allows(RolePrefixes.ShareBizzUser, PortalMenu.IdCard));
        Assert.True(PortalMenu.AllowsReferralArea(RolePrefixes.ShareBizzUser));
    }

    [Fact]
    public void Unknown_prefix_has_no_menu()
    {
        Assert.Empty(PortalMenu.FeaturesFor("ADM"));
        Assert.False(PortalMenu.Allows(null, PortalMenu.Dashboard));
    }
}
