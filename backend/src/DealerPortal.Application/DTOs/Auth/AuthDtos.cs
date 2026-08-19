namespace DealerPortal.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string RefreshToken);

public record UserInfoDto(
    Guid UserId,
    string Email,
    string FullName,
    Guid TenantId,
    string Role,
    Guid? DealerId);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    UserInfoDto User);
