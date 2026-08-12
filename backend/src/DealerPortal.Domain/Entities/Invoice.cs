using DealerPortal.Domain.Common;

namespace DealerPortal.Domain.Entities;

public class Invoice : BaseEntity, ITenantEntity
{
    private readonly List<InvoiceLine> _lines = [];

    public Guid TenantId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

    public decimal TotalNet { get; private set; }
    public decimal TotalVat { get; private set; }
    public decimal TotalGross { get; private set; }

    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();

    // Navigation property
    public Order? Order { get; set; }

    protected Invoice() { }

    public Invoice(Guid tenantId, string invoiceNo, Guid orderId)
    {
        TenantId = tenantId;
        InvoiceNo = invoiceNo;
        OrderId = orderId;
        IssuedAtUtc = DateTime.UtcNow;
    }

    public void AddLine(Guid productId, string sku, string productName, int quantity, decimal unitPrice, decimal vatRate)
    {
        var line = new InvoiceLine(Id, productId, sku, productName, quantity, unitPrice, vatRate);
        _lines.Add(line);
        TotalNet = _lines.Sum(l => l.LineTotalNet);
        TotalVat = _lines.Sum(l => l.LineTotalVat);
        TotalGross = TotalNet + TotalVat;
    }
}
