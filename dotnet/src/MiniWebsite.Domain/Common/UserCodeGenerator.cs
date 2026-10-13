using System.Security.Cryptography;

namespace MiniWebsite.Domain.Common;

/// <summary>Public user ID: role prefix + 8 random uppercase letters/digits, e.g. <c>SBU8F4K2X9P</c>.</summary>
public static class UserCodeGenerator
{
    public const int RandomLength = 8;
    public const string FallbackPrefix = "USR";
    public const string OrderPrefix = "ORD";

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

    /// <summary>DB profile ID from the card slug, e.g. <c>DB8I7A4B5D</c> → <c>DB 8I7A4B5D</c>.</summary>
    public static string FormatDbId(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return "";
        var code = slug.Trim();
        if (code.Contains(' ')) return code;
        if (code.StartsWith("DB", StringComparison.OrdinalIgnoreCase) && code.Length > 2)
            return "DB " + code[2..].ToUpperInvariant();
        return code;
    }
}
