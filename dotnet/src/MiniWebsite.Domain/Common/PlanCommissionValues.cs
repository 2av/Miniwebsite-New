namespace MiniWebsite.Domain.Common;

/// <summary>Allowed values for pricing plans and plan commissions.</summary>
public static class PlanCommissionValues
{
    public const decimal GstRate = 1.18m;

    public const string CategoryDbProfile = "DB Profile";
    public const string CategoryFranchise = "Franchise";
    public const string CategoryMembership = "Membership";
    public static readonly string[] Categories = [CategoryDbProfile, CategoryFranchise, CategoryMembership];

    public const string DiscountFixed = "Fixed";
    public const string DiscountPartnerDecides = "Partner Decides";
    public const string DiscountOccasional = "Occasional";
    public static readonly string[] DiscountTypes = [DiscountFixed, DiscountPartnerDecides, DiscountOccasional];

    public const string EarningFixed = "Fixed";
    public const string EarningSalaryIncentive = "Salary + Incentive";
    public static readonly string[] EarningTypes = [EarningFixed, EarningSalaryIncentive];

    public const string StatusActive = "Active";
    public const string StatusInActive = "InActive";

    /// <summary>Amount including 18% GST, rounded to the nearest rupee.</summary>
    public static decimal WithGst(decimal amount) =>
        Math.Round(amount * GstRate, 0, MidpointRounding.AwayFromZero);
}
