using System.Security.Cryptography;

namespace MiniWebsite.Domain.Common;

/// <summary>Public user ID: role prefix + 8 random uppercase letters/digits, e.g. <c>SBU8F4K2X9P</c>.</summary>
public static class UserCodeGenerator
{
    public const int RandomLength = 8;
    public const string FallbackPrefix = "USR";

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
}
