using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;
using System.Security.Claims;

namespace MiniWebsite.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string CreateAccessToken(User user);
    string CreateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
