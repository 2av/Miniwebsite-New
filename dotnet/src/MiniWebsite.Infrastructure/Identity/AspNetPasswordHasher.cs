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
}
