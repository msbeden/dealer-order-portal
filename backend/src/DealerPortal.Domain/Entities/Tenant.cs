using DealerPortal.Domain.Common;

namespace DealerPortal.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    protected Tenant() { }

    public Tenant(string name, string code)
    {
        Name = name;
        Code = code.ToUpperInvariant();
        IsActive = true;
    }
}
