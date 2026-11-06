using MiniWebsite.Application.Portal;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.UnitTests;

public class OnboardingAccessTests
{
    private static readonly DateTime Now = new(2026, 11, 4, 12, 0, 0);

    private static User User(string prefix, string stage, DateTime? referralEligibleAt = null) => new()
    {
        Email = "user@example.com",
        FirstName = "User",
        Password = "hash",
        OnboardingStage = stage,
        ReferralEligibleAt = referralEligibleAt,
        AssignedRole = new Role { Prefix = prefix },
    };

    [Fact]
    public void SbUser_payment_due_is_dashboard_only()
    {
        var open = OnboardingAccess.OpenFeatures(RolePrefixes.ShareBizzUser, OnboardingStage.PaymentDue, null, Now);
        Assert.Equal([PortalMenu.Dashboard], open);
    }

    [Fact]
    public void SbUser_referral_stays_closed_until_the_eligible_date()
    {
        var later = Now.AddDays(10);
        Assert.False(OnboardingAccess.ReferralUnlocked(RolePrefixes.ShareBizzUser, later, Now));
        Assert.DoesNotContain(
            PortalMenu.Referral,
            OnboardingAccess.OpenFeatures(RolePrefixes.ShareBizzUser, OnboardingStage.Features, later, Now));
    }

    [Fact]
    public void SbUser_referral_opens_on_the_eligible_date()
    {
        Assert.Contains(
            PortalMenu.Referral,
            OnboardingAccess.OpenFeatures(RolePrefixes.ShareBizzUser, OnboardingStage.Features, Now, Now));
    }

    [Fact]
    public void Db_profile_payment_sets_a_45_day_referral_lock()
    {
        var user = User(RolePrefixes.ShareBizzUser, OnboardingStage.PaymentDue);
        OnboardingAccess.MarkDbProfilePaid(user, Now);
        Assert.Equal(OnboardingStage.Features, user.OnboardingStage);
        Assert.Equal(Now.AddDays(45), user.ReferralEligibleAt);
    }

    [Fact]
    public void Membership_payment_opens_document_verification()
    {
        var user = User(RolePrefixes.DigitalMarketingPartner, OnboardingStage.PaymentDue);
        OnboardingAccess.MarkMembershipPaid(user, Now);
        Assert.Equal(OnboardingStage.DocumentVerification, user.OnboardingStage);
        Assert.Equal(
            [PortalMenu.Dashboard, PortalMenu.Verification],
            OnboardingAccess.OpenFeatures(user.AssignedRole!.Prefix, user.OnboardingStage, null, Now));
    }

    [Fact]
    public void Partner_approval_is_ready_to_sell_and_sb_approval_stays_approved()
    {
        var partner = User(RolePrefixes.FieldSalesPartner, OnboardingStage.WaitingForReview);
        OnboardingAccess.ApplyDocumentDecision(partner, "Approved", Now);
        Assert.Equal(OnboardingStage.ReadyToSell, partner.OnboardingStage);

        var sb = User(RolePrefixes.ShareBizzUser, OnboardingStage.WaitingForReview);
        OnboardingAccess.ApplyDocumentDecision(sb, "Approved", Now);
        Assert.Equal(OnboardingStage.Approved, sb.OnboardingStage);
    }

    [Fact]
    public void Print_shop_fee_label_differs_from_dm_membership()
    {
        Assert.Equal("Membership fees", OnboardingAccess.MembershipFeeLabel(RolePrefixes.PrintShopPartner));
        Assert.Equal("DM Membership fees", OnboardingAccess.MembershipFeeLabel(RolePrefixes.FranchisePartner));
    }
}
