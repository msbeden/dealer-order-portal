using DealerPortal.Domain.Common;

namespace DealerPortal.Domain.Entities;

public class Category : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;

    protected Category() { }

    public Category(Guid tenantId, string name)
    {
        TenantId = tenantId;
        Name = name;
    }
}
