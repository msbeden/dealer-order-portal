using DealerPortal.Application.Common.Pagination;
using DealerPortal.Domain.Enums;

namespace DealerPortal.Application.DTOs.Orders;

public record OrderLineDto(
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

public record OrderDto(
    Guid Id,
    Guid TenantId,
    string OrderNo,
    Guid DealerId,
    string? DealerName,
    OrderStatus Status,
    string? ApprovedBy,
    DateTime? ApprovedAtUtc,
    string? RejectionReason,
    string? CancellationReason,
    decimal TotalNet,
    decimal TotalVat,
    decimal TotalGross,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderLineDto> Lines);

public record CreateOrderRequest(Guid DealerId);

public record AddOrderLineRequest(Guid ProductId, int Quantity);

public record UpdateOrderLineRequest(int Quantity);

public record RejectOrderRequest(string Reason);

public record OrderFilterParams : PaginationParams
{
    public OrderStatus? Status { get; init; }
    public Guid? DealerId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}
