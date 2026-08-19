using DealerPortal.Application.Common.Pagination;

namespace DealerPortal.Application.DTOs.Products;

public record ProductDto(
    Guid Id,
    Guid TenantId,
    string Sku,
    string Name,
    Guid CategoryId,
    string? CategoryName,
    decimal UnitPrice,
    decimal VatRate,
    int StockQuantity,
    byte[] RowVersion);

public record CreateProductRequest(
    string Sku,
    string Name,
    Guid CategoryId,
    decimal UnitPrice,
    decimal VatRate,
    int InitialStock);

public record UpdateProductRequest(
    string Name,
    Guid CategoryId,
    decimal UnitPrice,
    decimal VatRate,
    int StockQuantity,
    byte[] RowVersion);

public record ProductFilterParams : PaginationParams
{
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public bool InStockOnly { get; init; }
}
