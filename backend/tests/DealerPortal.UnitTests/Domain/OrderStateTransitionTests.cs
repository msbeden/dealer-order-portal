using DealerPortal.Domain.Entities;
using DealerPortal.Domain.Enums;
using DealerPortal.Domain.Exceptions;

namespace DealerPortal.UnitTests.Domain;

public class OrderStateTransitionTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _dealerId = Guid.NewGuid();

    [Fact]
    public void AddLine_InDraftOrder_CalculatesTotalsCorrectly()
    {
        var order = new Order(_tenantId, "ORD-2026-001", _dealerId);
        var productId = Guid.NewGuid();

        order.AddLine(productId, "SKU-001", "Gaming Laptop", 2, 1000m, 20m);

        Assert.Equal(2000m, order.TotalNet);
        Assert.Equal(400m, order.TotalVat);
        Assert.Equal(2400m, order.TotalGross);
        Assert.Single(order.Lines);
    }

    [Fact]
    public void Submit_WithSufficientCreditLimit_TransitionsToSubmitted()
    {
        var order = new Order(_tenantId, "ORD-2026-002", _dealerId);
        order.AddLine(Guid.NewGuid(), "SKU-002", "Office Chair", 1, 500m, 20m); // Gross: 600

        order.Submit(currentDealerBalance: 1000m, dealerCreditLimit: 2000m);

        Assert.Equal(OrderStatus.Submitted, order.Status);
    }

    [Fact]
    public void Submit_WhenExceedingCreditLimit_ThrowsDomainException()
    {
        var order = new Order(_tenantId, "ORD-2026-003", _dealerId);
        order.AddLine(Guid.NewGuid(), "SKU-003", "Server Rack", 1, 5000m, 20m); // Gross: 6000

        var ex = Assert.Throws<DomainException>(() =>
            order.Submit(currentDealerBalance: 2000m, dealerCreditLimit: 5000m));

        Assert.Equal("CREDIT_LIMIT_EXCEEDED", ex.ErrorCode);
    }

    [Fact]
    public void Submit_WithoutLines_ThrowsDomainException()
    {
        var order = new Order(_tenantId, "ORD-2026-004", _dealerId);

        var ex = Assert.Throws<DomainException>(() =>
            order.Submit(currentDealerBalance: 0m, dealerCreditLimit: 10000m));

        Assert.Equal("EMPTY_ORDER", ex.ErrorCode);
    }

    [Fact]
    public void Approve_WhenInSubmittedStatus_TransitionsToApproved()
    {
        var order = new Order(_tenantId, "ORD-2026-005", _dealerId);
        order.AddLine(Guid.NewGuid(), "SKU-005", "Monitor", 2, 300m, 20m);
        order.Submit(0m, 5000m);

        order.Approve("salesrep@company.com");

        Assert.Equal(OrderStatus.Approved, order.Status);
        Assert.Equal("salesrep@company.com", order.ApprovedBy);
        Assert.NotNull(order.ApprovedAtUtc);
    }

    [Fact]
    public void Approve_DirectlyFromDraft_ThrowsDomainException()
    {
        var order = new Order(_tenantId, "ORD-2026-006", _dealerId);
        order.AddLine(Guid.NewGuid(), "SKU-006", "Keyboard", 1, 50m, 20m);

        var ex = Assert.Throws<DomainException>(() => order.Approve("salesrep@company.com"));

        Assert.Equal("INVALID_STATE_TRANSITION", ex.ErrorCode);
    }

    [Fact]
    public void Reject_WhenSubmitted_TransitionsToRejected()
    {
        var order = new Order(_tenantId, "ORD-2026-007", _dealerId);
        order.AddLine(Guid.NewGuid(), "SKU-007", "Mouse", 1, 20m, 20m);
        order.Submit(0m, 1000m);

        order.Reject("Out of stock allocation for dealer", "salesrep@company.com");

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Equal("Out of stock allocation for dealer", order.RejectionReason);
    }

    [Fact]
    public void MarkInvoiced_WhenApproved_TransitionsToInvoiced()
    {
        var order = new Order(_tenantId, "ORD-2026-008", _dealerId);
        order.AddLine(Guid.NewGuid(), "SKU-008", "Headset", 1, 100m, 20m);
        order.Submit(0m, 1000m);
        order.Approve("salesrep@company.com");

        order.MarkInvoiced();

        Assert.Equal(OrderStatus.Invoiced, order.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyInvoiced_ThrowsDomainException()
    {
        var order = new Order(_tenantId, "ORD-2026-009", _dealerId);
        order.AddLine(Guid.NewGuid(), "SKU-009", "Webcam", 1, 80m, 20m);
        order.Submit(0m, 1000m);
        order.Approve("salesrep@company.com");
        order.MarkInvoiced();

        var ex = Assert.Throws<DomainException>(() => order.Cancel("Customer changed mind"));

        Assert.Equal("INVALID_STATE_TRANSITION", ex.ErrorCode);
    }
}
