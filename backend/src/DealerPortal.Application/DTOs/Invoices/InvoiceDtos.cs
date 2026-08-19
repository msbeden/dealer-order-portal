using DealerPortal.Application.Common.Pagination;

namespace DealerPortal.Application.DTOs.Invoices;

public record InvoiceLineDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotalNet,
    decimal LineTotalVat,
    decimal LineTotalGross);

public record InvoiceDto(
    Guid Id,
    Guid TenantId,
    string InvoiceNo,
    Guid OrderId,
    DateTime IssuedAtUtc,
    decimal TotalNet,
    decimal TotalVat,
    decimal TotalGross,
    IReadOnlyList<InvoiceLineDto> Lines);

public record InvoiceFilterParams : PaginationParams
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}
