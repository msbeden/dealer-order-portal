using System.Security.Claims;
using DealerPortal.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DealerPortal.Infrastructure.Services;

public class CurrentTenant : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenant(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var tenantClaim = user?.FindFirst("tenant_id")?.Value
                ?? user?.FindFirst("tenantId")?.Value;

            return Guid.TryParse(tenantClaim, out var tenantId) ? tenantId : null;
        }
    }

    public string? TenantCode => _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_code")?.Value;
}
