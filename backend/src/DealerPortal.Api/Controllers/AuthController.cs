using DealerPortal.Application.DTOs.Auth;
using DealerPortal.Application.Interfaces;
using DealerPortal.Infrastructure.Data;
using DealerPortal.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DealerPortal.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly AppDbContext _dbContext;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(
        UserManager<AppUser> userManager,
        ITokenService tokenService,
        AppDbContext dbContext,
        IValidator<LoginRequest> loginValidator)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _dbContext = dbContext;
        _loginValidator = loginValidator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.ToDictionary());
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Dealer";

        var (accessToken, expiresAtUtc) = _tokenService.GenerateAccessToken(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.TenantId,
            primaryRole,
            user.DealerId);

        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        var userInfo = new UserInfoDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.TenantId,
            primaryRole,
            user.DealerId);

        return Ok(new LoginResponse(accessToken, refreshToken, expiresAtUtc, userInfo));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { message = "RefreshToken is required." });
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        if (user is null || user.RefreshTokenExpiresAtUtc <= DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Dealer";

        var (accessToken, expiresAtUtc) = _tokenService.GenerateAccessToken(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.TenantId,
            primaryRole,
            user.DealerId);

        var newRefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        var userInfo = new UserInfoDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.TenantId,
            primaryRole,
            user.DealerId);

        return Ok(new LoginResponse(accessToken, newRefreshToken, expiresAtUtc, userInfo));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfoDto>> Me()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Dealer";

        return Ok(new UserInfoDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.TenantId,
            primaryRole,
            user.DealerId));
    }
}
