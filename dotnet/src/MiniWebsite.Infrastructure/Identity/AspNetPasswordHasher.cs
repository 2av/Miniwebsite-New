using Microsoft.AspNetCore.Identity;
using MiniWebsite.Application.Common.Interfaces;

namespace MiniWebsite.Infrastructure.Identity;

/// <summary>
/// Verifies PHP <c>password_hash</c> (BCrypt) and ASP.NET Identity hashes.
/// New hashes are written as BCrypt so PHP and .NET stay compatible.
/// </summary>
public class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _identityHasher = new();

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return false;

        // PHP password_hash / BCrypt.Net
        if (passwordHash.StartsWith("$2y$", StringComparison.Ordinal)
            || passwordHash.StartsWith("$2a$", StringComparison.Ordinal)
            || passwordHash.StartsWith("$2b$", StringComparison.Ordinal))
        {
            try
            {
                // Normalize $2y$ (PHP) to $2a$ for BCrypt.Net
                var normalized = passwordHash.StartsWith("$2y$", StringComparison.Ordinal)
                    ? "$2a$" + passwordHash[4..]
                    : passwordHash;
                return BCrypt.Net.BCrypt.Verify(password, normalized);
            }
            catch
            {
                return false;
            }
        }

        var result = _identityHasher.VerifyHashedPassword(new object(), passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    public bool IsHashed(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        if (value.Length == 60
            && (value.StartsWith("$2y$", StringComparison.Ordinal)
                || value.StartsWith("$2a$", StringComparison.Ordinal)
                || value.StartsWith("$2b$", StringComparison.Ordinal)))
            return true;

        // ASP.NET Identity: base64 payload whose first byte is the format marker (0x00 v2, 0x01 v3).
        Span<byte> buffer = stackalloc byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out var written)
               && written >= 49
               && buffer[0] is 0x00 or 0x01;
    }
}
