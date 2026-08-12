using DealerPortal.Domain.Common;
using DealerPortal.Domain.Enums;

namespace DealerPortal.Domain.Entities;

public class StockMovement : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public StockMovementType Type { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Description { get; set; }

    // Navigation property
    public Product? Product { get; set; }

    protected StockMovement() { }

    public StockMovement(Guid tenantId, Guid productId, int quantity, StockMovementType type, Guid? referenceId = null, string? description = null)
    {
        TenantId = tenantId;
        ProductId = productId;
        Quantity = quantity;
        Type = type;
        ReferenceId = referenceId;
        Description = description;
    }
}
