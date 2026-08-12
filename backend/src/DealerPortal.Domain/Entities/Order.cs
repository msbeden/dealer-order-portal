using DealerPortal.Domain.Common;
using DealerPortal.Domain.Enums;
using DealerPortal.Domain.Exceptions;

namespace DealerPortal.Domain.Entities;

public class Order : BaseEntity, ITenantEntity
{
    private readonly List<OrderLine> _lines = [];

    public Guid TenantId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public Guid DealerId { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? CancellationReason { get; private set; }

    public decimal TotalNet { get; private set; }
    public decimal TotalVat { get; private set; }
    public decimal TotalGross { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    // Navigation properties
    public Dealer? Dealer { get; set; }

    protected Order() { }

    public Order(Guid tenantId, string orderNo, Guid dealerId)
    {
        TenantId = tenantId;
        OrderNo = orderNo;
        DealerId = dealerId;
        Status = OrderStatus.Draft;
    }

    public OrderLine AddLine(Guid productId, string sku, string productName, int quantity, decimal unitPrice, decimal vatRate)
    {
        EnsureInDraft("Lines can only be added to draft orders.");

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.", "INVALID_QUANTITY");
        }

        var existing = _lines.FirstOrDefault(l => l.ProductId == productId);
        if (existing is not null)
        {
            existing.Quantity += quantity;
            RecalculateTotals();
            return existing;
        }

        var line = new OrderLine(Id, productId, sku, productName, quantity, unitPrice, vatRate);
        _lines.Add(line);
        RecalculateTotals();
        return line;
    }

    public void UpdateLineQuantity(Guid lineId, int newQuantity)
    {
        EnsureInDraft("Line quantities can only be modified in draft orders.");

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new DomainException($"Order line {lineId} not found.", "LINE_NOT_FOUND");

        if (newQuantity <= 0)
        {
            _lines.Remove(line);
        }
        else
        {
            line.Quantity = newQuantity;
        }

        RecalculateTotals();
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureInDraft("Lines can only be removed from draft orders.");

        var line = _lines.FirstOrDefault(l => l.Id == lineId)
            ?? throw new DomainException($"Order line {lineId} not found.", "LINE_NOT_FOUND");

        _lines.Remove(line);
        RecalculateTotals();
    }

    public void Submit(decimal currentDealerBalance, decimal dealerCreditLimit)
    {
        EnsureInDraft("Only draft orders can be submitted.");

        if (_lines.Count == 0)
        {
            throw new DomainException("Cannot submit an empty order.", "EMPTY_ORDER");
        }

        RecalculateTotals();

        var projectedBalance = currentDealerBalance + TotalGross;
        if (projectedBalance > dealerCreditLimit)
        {
            throw new DomainException(
                $"Order submission rejected: Dealer credit limit ({dealerCreditLimit:C}) exceeded. Current: {currentDealerBalance:C}, Order: {TotalGross:C}",
                "CREDIT_LIMIT_EXCEEDED");
        }

        Status = OrderStatus.Submitted;
    }

    public void Approve(string approvedBy)
    {
        if (Status != OrderStatus.Submitted)
        {
            throw new DomainException(
                $"Cannot approve order with status '{Status}'. Only submitted orders can be approved.",
                "INVALID_STATE_TRANSITION");
        }

        Status = OrderStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = DateTime.UtcNow;
    }

    public void Reject(string reason, string rejectedBy)
    {
        if (Status != OrderStatus.Submitted)
        {
            throw new DomainException(
                $"Cannot reject order with status '{Status}'. Only submitted orders can be rejected.",
                "INVALID_STATE_TRANSITION");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Rejection reason must be provided.", "REASON_REQUIRED");
        }

        Status = OrderStatus.Rejected;
        RejectionReason = reason;
        ApprovedBy = rejectedBy;
        ApprovedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Invoiced)
        {
            throw new DomainException("Invoiced orders cannot be cancelled.", "INVALID_STATE_TRANSITION");
        }

        if (Status == OrderStatus.Cancelled)
        {
            throw new DomainException("Order is already cancelled.", "ALREADY_CANCELLED");
        }

        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
    }

    public void MarkInvoiced()
    {
        if (Status != OrderStatus.Approved)
        {
            throw new DomainException(
                $"Cannot invoice order with status '{Status}'. Order must be approved first.",
                "INVALID_STATE_TRANSITION");
        }

        Status = OrderStatus.Invoiced;
    }

    private void EnsureInDraft(string message)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new DomainException(message, "ORDER_NOT_IN_DRAFT");
        }
    }

    private void RecalculateTotals()
    {
        TotalNet = _lines.Sum(l => l.LineTotalNet);
        TotalVat = _lines.Sum(l => l.LineTotalVat);
        TotalGross = TotalNet + TotalVat;
    }
}
