using System.Security.Claims;
using DealerPortal.Application.DTOs.Auth;

namespace DealerPortal.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(
        Guid userId,
        string email,
        string fullName,
        Guid tenantId,
        string role,
        Guid? dealerId = null);

    string GenerateRefreshToken();

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
