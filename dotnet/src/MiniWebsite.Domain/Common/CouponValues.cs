using System.Security.Cryptography;

namespace MiniWebsite.Domain.Common;

public static class CouponValues
{
    public const string StatusActive = "Active";
    public const string StatusInActive = "InActive";
    public const string StatusExpired = "Expired";

    public const string TypeFestive = "Festive Discount";
    public const string TypePromotional = "Promotional Discount";
    public static readonly string[] Types = [TypeFestive, TypePromotional];

    public const string DiscountAmount = "Amount";
    public const string DiscountPercentage = "Percentage";

    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    /// <summary>Random code in the form 4 letters + 4 digits, e.g. KDHG4513.</summary>
    public static string GenerateCode()
    {
        Span<char> chars = stackalloc char[8];
        for (var i = 0; i < 4; i++)
            chars[i] = Letters[RandomNumberGenerator.GetInt32(Letters.Length)];
        for (var i = 4; i < 8; i++)
            chars[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
        return new string(chars);
    }

    public static bool IsExpired(DateTime? validityDate) =>
        validityDate.HasValue && validityDate.Value.Date < AppClock.Today;

    /// <summary>Status shown to admins: Expired once the validity date has passed, otherwise the stored status.</summary>
    public static string DisplayStatus(string? storedStatus, DateTime? validityDate) =>
        IsExpired(validityDate)
            ? StatusExpired
            : string.Equals(storedStatus, StatusActive, StringComparison.OrdinalIgnoreCase)
                ? StatusActive
                : StatusInActive;
}
