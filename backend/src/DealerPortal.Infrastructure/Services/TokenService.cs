using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DealerPortal.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DealerPortal.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly byte[] _key;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        var secret = _configuration["Jwt:SecretKey"] ?? "EnterpriseSecretKeyForDealerPortalMultiTenantJwtAuth2026!";
        _key = Encoding.UTF8.GetBytes(secret);
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(
        Guid userId,
        string email,
        string fullName,
        Guid tenantId,
        string role,
        Guid? dealerId = null)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(
            double.TryParse(_configuration["Jwt:ExpiryMinutes"], out var minutes) ? minutes : 60);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", fullName),
            new("tenant_id", tenantId.ToString()),
            new(ClaimTypes.Role, role)
        };

        if (dealerId.HasValue)
        {
            claims.Add(new Claim("dealer_id", dealerId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAtUtc,
            Issuer = _configuration["Jwt:Issuer"] ?? "DealerPortalApi",
            Audience = _configuration["Jwt:Audience"] ?? "DealerPortalClient",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return (tokenHandler.WriteToken(token), expiresAtUtc);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_key),
            ValidateLifetime = false // Here we want the claims even if the token has expired
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return principal;
    }
}
