using DealerPortal.Domain.Common;

namespace DealerPortal.Domain.Entities;

public class Dealer : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public Guid? SalesRepId { get; set; }

    protected Dealer() { }

    public Dealer(Guid tenantId, string code, string name, string city, decimal creditLimit, Guid? salesRepId = null)
    {
        TenantId = tenantId;
        Code = code.ToUpperInvariant();
        Name = name;
        City = city;
        CreditLimit = creditLimit;
        CurrentBalance = 0m;
        SalesRepId = salesRepId;
    }

    public decimal AvailableCredit => Math.Max(0m, CreditLimit - CurrentBalance);

    public bool CanAfford(decimal amount) => (CurrentBalance + amount) <= CreditLimit;
}
