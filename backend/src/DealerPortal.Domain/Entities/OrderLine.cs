using DealerPortal.Domain.Common;

namespace DealerPortal.Domain.Entities;

public class OrderLine : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }

    public decimal LineTotalNet => Math.Round(Quantity * UnitPrice, 2);
    public decimal LineTotalVat => Math.Round(LineTotalNet * (VatRate / 100m), 2);
    public decimal LineTotalGross => LineTotalNet + LineTotalVat;

    // Navigation property
    public Order? Order { get; set; }

    protected OrderLine() { }

    public OrderLine(Guid orderId, Guid productId, string sku, string productName, int quantity, decimal unitPrice, decimal vatRate)
    {
        OrderId = orderId;
        ProductId = productId;
        Sku = sku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        VatRate = vatRate;
    }
}
