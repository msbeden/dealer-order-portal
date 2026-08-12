using DealerPortal.Domain.Common;
using DealerPortal.Domain.Exceptions;

namespace DealerPortal.Domain.Entities;

public class Product : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public int StockQuantity { get; set; }
    public byte[] RowVersion { get; set; } = [];

    // Navigation property
    public Category? Category { get; set; }

    protected Product() { }

    public Product(Guid tenantId, string sku, string name, Guid categoryId, decimal unitPrice, decimal vatRate, int stockQuantity)
    {
        TenantId = tenantId;
        Sku = sku.ToUpperInvariant();
        Name = name;
        CategoryId = categoryId;
        UnitPrice = unitPrice >= 0 ? unitPrice : throw new DomainException("Unit price cannot be negative.");
        VatRate = vatRate >= 0 ? vatRate : throw new DomainException("VAT rate cannot be negative.");
        StockQuantity = stockQuantity >= 0 ? stockQuantity : throw new DomainException("Initial stock quantity cannot be negative.");
    }

    public void DeductStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Deduction quantity must be positive.", "INVALID_QUANTITY");
        }

        if (StockQuantity < quantity)
        {
            throw new DomainException($"Insufficient stock for product {Sku}. Requested: {quantity}, Available: {StockQuantity}", "INSUFFICIENT_STOCK");
        }

        StockQuantity -= quantity;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Replenishment quantity must be positive.", "INVALID_QUANTITY");
        }

        StockQuantity += quantity;
    }
}
