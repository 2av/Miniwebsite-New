using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Portal;

/// <summary>
/// Which portal features a role prefix may open at its current onboarding stage.
/// Social Media Partner, Field Sales Team, and profile-only users are not stage-gated.
/// </summary>
public static class OnboardingAccess
{
    public const int SbReferralLockDays = 45;

    public static bool IsStageGated(string? prefix)
    {
        var p = Norm(prefix);
        return p is RolePrefixes.ShareBizzUser
            or RolePrefixes.DigitalMarketingPartner
            or RolePrefixes.FieldSalesPartner
            or RolePrefixes.PrintShopPartner
            or RolePrefixes.FranchisePartner;
    }

    public static bool PaysMembership(string? prefix)
    {
        var p = Norm(prefix);
        return p is RolePrefixes.DigitalMarketingPartner
            or RolePrefixes.FieldSalesPartner
            or RolePrefixes.PrintShopPartner
            or RolePrefixes.FranchisePartner;
    }

    public static string MembershipFeeLabel(string? prefix) =>
        Norm(prefix) == RolePrefixes.PrintShopPartner ? "Membership fees" : "DM Membership fees";

    public static bool ReferralUnlocked(string? prefix, DateTime? referralEligibleAt, DateTime now)
    {
        if (Norm(prefix) != RolePrefixes.ShareBizzUser) return true;
        return referralEligibleAt is DateTime at && now >= at;
    }

    public static IReadOnlyList<string> OpenFeatures(
        string? prefix, string? stage, DateTime? referralEligibleAt, DateTime now)
    {
        var catalog = PortalMenu.FeaturesFor(prefix);
        if (!IsStageGated(prefix)) return catalog;

        var current = string.IsNullOrWhiteSpace(stage) ? OnboardingStage.ReadyToSell : stage.Trim();
        if (Norm(prefix) == RolePrefixes.ShareBizzUser)
        {
            if (current is OnboardingStage.AccountOnly or OnboardingStage.PaymentDue)
                return [PortalMenu.Dashboard];

            var open = new List<string> { PortalMenu.Dashboard, PortalMenu.Customers, PortalMenu.Marketing };
            if (ReferralUnlocked(prefix, referralEligibleAt, now))
            {
                open.Add(PortalMenu.Referral);
                open.Add(PortalMenu.Verification);
            }

            return open.Where(catalog.Contains).ToList();
        }

        if (current is OnboardingStage.AccountOnly or OnboardingStage.PaymentDue)
            return [PortalMenu.Dashboard];
        if (current is OnboardingStage.DocumentVerification or OnboardingStage.WaitingForReview)
            return [PortalMenu.Dashboard, PortalMenu.Verification];
        return catalog;
    }

    public static bool Allows(
        string? prefix, string? stage, DateTime? referralEligibleAt, string feature, DateTime now)
    {
        if (!PortalMenu.Allows(prefix, feature)) return false;
        if (!IsStageGated(prefix)) return true;
        return OpenFeatures(prefix, stage, referralEligibleAt, now).Contains(feature);
    }

    public static bool DocumentsOpen(string? prefix, string? stage, DateTime? referralEligibleAt, DateTime now) =>
        Allows(prefix, stage, referralEligibleAt, PortalMenu.Verification, now);

    public static void SetStage(User user, string stage, DateTime now)
    {
        user.OnboardingStage = stage;
        user.OnboardingStageAt = now;
        user.UpdatedAt = now;
    }

    public static void MarkDbProfilePaid(User user, DateTime now)
    {
        if (!string.Equals(user.AssignedRole?.Prefix, RolePrefixes.ShareBizzUser, StringComparison.OrdinalIgnoreCase))
            return;

        if (user.PaidAt == null)
        {
            user.PaidAt = now;
            user.ReferralEligibleAt ??= now.AddDays(SbReferralLockDays);
        }

        if (user.OnboardingStage is OnboardingStage.AccountOnly or OnboardingStage.PaymentDue)
            SetStage(user, OnboardingStage.Features, now);
    }

    public static void MarkMembershipPaid(User user, DateTime now)
    {
        if (!PaysMembership(user.AssignedRole?.Prefix)) return;
        user.PaidAt ??= now;
        if (user.OnboardingStage is OnboardingStage.AccountOnly or OnboardingStage.PaymentDue or OnboardingStage.Features)
            SetStage(user, OnboardingStage.DocumentVerification, now);
    }

    public static void MarkDocumentsSubmitted(User user, DateTime now) =>
        SetStage(user, OnboardingStage.WaitingForReview, now);

    /// <param name="decision">Approved, Resubmission Required, or Rejected.</param>
    public static void ApplyDocumentDecision(User user, string decision, DateTime now)
    {
        if (string.Equals(decision, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            var readyToSell = !string.Equals(
                user.AssignedRole?.Prefix, RolePrefixes.ShareBizzUser, StringComparison.OrdinalIgnoreCase);
            SetStage(user, readyToSell ? OnboardingStage.ReadyToSell : OnboardingStage.Approved, now);
            return;
        }

        SetStage(user, OnboardingStage.DocumentVerification, now);
    }

    private static string Norm(string? prefix) => (prefix ?? "").Trim().ToUpperInvariant();
}
