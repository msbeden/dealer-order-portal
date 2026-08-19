namespace DealerPortal.Application.Interfaces;

/// <summary>
/// Scoped tenant accessor resolving the active tenant context from current HTTP request / JWT claims.
/// </summary>
public interface ICurrentTenant
{
    Guid? TenantId { get; }
    string? TenantCode { get; }
    bool HasTenant => TenantId.HasValue;
}
