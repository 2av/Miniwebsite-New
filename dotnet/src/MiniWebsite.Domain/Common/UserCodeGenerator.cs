using System.Security.Cryptography;

namespace MiniWebsite.Domain.Common;

/// <summary>Public user ID: role prefix + 8 random uppercase letters/digits, e.g. <c>SBU8F4K2X9P</c>.</summary>
public static class UserCodeGenerator
{
    public const int RandomLength = 8;
    public const string FallbackPrefix = "USR";
    public const string OrderPrefix = "ORD";
    public const string DbPrefix = "DB";

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>Uniqueness is not checked here; callers must verify against the database.</summary>
    public static string Generate(string? rolePrefix)
    {
        var prefix = string.IsNullOrWhiteSpace(rolePrefix) ? FallbackPrefix : rolePrefix.Trim().ToUpperInvariant();
        var chars = new char[RandomLength];
        do
        {
            for (var i = 0; i < chars.Length; i++)
                chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        while (!chars.Any(char.IsLetter) || !chars.Any(char.IsDigit));

        return prefix + new string(chars);
    }

    /// <summary>Display form with a space after the role prefix, e.g. <c>SBU 8F4K2X9P</c>.</summary>
    public static string FormatDisplay(string? userCode)
    {
        if (string.IsNullOrWhiteSpace(userCode)) return "";
        var code = userCode.Trim().ToUpperInvariant();
        if (code.Contains(' ')) return code;

        string[] prefixes =
        [
            RolePrefixes.ShareBizzProfileUser,
            RolePrefixes.ShareBizzUser,
            RolePrefixes.FranchisePartner,
            RolePrefixes.DigitalMarketingPartner,
            RolePrefixes.PrintShopPartner,
            RolePrefixes.SocialMediaPartner,
            RolePrefixes.FieldSalesTeam,
            RolePrefixes.FieldSalesPartner,
            RolePrefixes.Administrator,
            OrderPrefix,
            FallbackPrefix,
        ];

        foreach (var prefix in prefixes)
        {
            if (code.StartsWith(prefix, StringComparison.Ordinal) && code.Length > prefix.Length)
                return prefix + " " + code[prefix.Length..];
        }

        return code;
    }

    /// <summary>
    /// Public DB profile id, saved as <c>DB 4E5T8Y9C</c>.
    /// <c>DB</c> is fixed, then 8 letters/digits that start with a number.
    /// </summary>
    public static string GenerateDbCode()
    {
        const string digits = "0123456789";
        var chars = new char[RandomLength];
        do
        {
            chars[0] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            for (var i = 1; i < chars.Length; i++)
                chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        while (!chars.Any(char.IsLetter));

        return DbPrefix + " " + new string(chars);
    }

    /// <summary>DB profile ID, e.g. <c>DB4E5T8Y9C</c> or <c>DB 4E5T8Y9C</c> → <c>DB 4E5T8Y9C</c>.</summary>
    public static string FormatDbId(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return "";
        var code = slug.Trim().ToUpperInvariant();
        if (code.StartsWith(DbPrefix + " ", StringComparison.Ordinal))
            return DbPrefix + " " + code[(DbPrefix.Length + 1)..].Replace(" ", "");
        if (code.StartsWith(DbPrefix, StringComparison.Ordinal) && code.Length > DbPrefix.Length)
            return DbPrefix + " " + code[DbPrefix.Length..].Replace(" ", "");
        return code;
    }
}
