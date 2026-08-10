namespace DealerPortal.Domain.Common;

/// <summary>
/// Enforces tenant ownership across multi-tenant data structures.
/// Query filters and interceptors use this interface for automatic data isolation.
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
