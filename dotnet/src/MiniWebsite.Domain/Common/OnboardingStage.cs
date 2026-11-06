namespace MiniWebsite.Domain.Common;

/// <summary>Where a user is in onboarding. Labels are stored as written.</summary>
public static class OnboardingStage
{
    public const string AccountOnly = "Account only";
    public const string PaymentDue = "Payment due";
    public const string Features = "Features";
    public const string DocumentVerification = "Document verification";
    public const string WaitingForReview = "Waiting for review";
    public const string Approved = "Approved";
    public const string ReadyToSell = "Ready to sell";

    public static readonly string[] All =
    [
        AccountOnly,
        PaymentDue,
        Features,
        DocumentVerification,
        WaitingForReview,
        Approved,
        ReadyToSell,
    ];
}

/// <summary>Referral payout invoice, before and after the user accepts it.</summary>
public static class PayoutAcceptanceStatus
{
    public const string Awaiting = "Awaiting acceptance";
    public const string Accepted = "Accepted";
    public const string Processed = "Processed";

    public static readonly string[] All = [Awaiting, Accepted, Processed];
}
