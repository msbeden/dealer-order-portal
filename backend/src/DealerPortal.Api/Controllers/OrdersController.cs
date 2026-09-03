using System.Security.Claims;
using DealerPortal.Application.Common.Pagination;
using DealerPortal.Application.DTOs.Orders;
using DealerPortal.Application.Interfaces;
using DealerPortal.Domain.Entities;
using DealerPortal.Domain.Enums;
using DealerPortal.Domain.Exceptions;
using DealerPortal.Infrastructure.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DealerPortal.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;
    private readonly IValidator<AddOrderLineRequest> _addLineValidator;

    public OrdersController(
        AppDbContext dbContext,
        ICurrentTenant currentTenant,
        IValidator<AddOrderLineRequest> addLineValidator)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _addLineValidator = addLineValidator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetOrders([FromQuery] OrderFilterParams filter)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Dealer)
            .Include(o => o.Lines)
            .AsQueryable();

        // Enforce role-based data visibility
        if (User.IsInRole("Dealer"))
        {
            var dealerIdStr = User.FindFirst("dealer_id")?.Value;
            if (Guid.TryParse(dealerIdStr, out var currentDealerId))
            {
                query = query.Where(o => o.DealerId == currentDealerId);
            }
            else
            {
                return Forbid();
            }
        }
        else if (User.IsInRole("SalesRep"))
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdStr, out var salesRepUserId))
            {
                query = query.Where(o => o.Dealer != null && o.Dealer.SalesRepId == salesRepUserId);
            }
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        if (filter.DealerId.HasValue && !User.IsInRole("Dealer"))
        {
            query = query.Where(o => o.DealerId == filter.DealerId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAtUtc >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAtUtc <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = orders.Select(MapToDto).ToList();

        return Ok(new PagedResult<OrderDto>(items, filter.Page, filter.PageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Dealer)
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Dealer"))
        {
            var dealerIdStr = User.FindFirst("dealer_id")?.Value;
            if (Guid.TryParse(dealerIdStr, out var currentDealerId) && order.DealerId != currentDealerId)
            {
                return Forbid();
            }
        }
        else if (User.IsInRole("SalesRep"))
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdStr, out var salesRepUserId) && order.Dealer?.SalesRepId != salesRepUserId)
            {
                return Forbid();
            }
        }

        return Ok(MapToDto(order));
    }

    [HttpPost]
    [Authorize(Roles = "Dealer,Admin")]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var tenantId = _currentTenant.TenantId ?? throw new UnauthorizedAccessException("Tenant context required.");

        var dealerId = request.DealerId;
        if (User.IsInRole("Dealer"))
        {
            var dealerIdStr = User.FindFirst("dealer_id")?.Value;
            if (Guid.TryParse(dealerIdStr, out var currentDealerId))
            {
                dealerId = currentDealerId;
            }
        }

        var dealer = await _dbContext.Dealers.FirstOrDefaultAsync(d => d.Id == dealerId)
            ?? throw new KeyNotFoundException($"Dealer {dealerId} not found.");

        var count = await _dbContext.Orders.CountAsync() + 1;
        var orderNo = $"ORD-{DateTime.UtcNow:yyyyMM}-{count:D5}";

        var order = new Order(tenantId, orderNo, dealerId);
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        order.Dealer = dealer;
        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, MapToDto(order));
    }

    [HttpPost("{id:guid}/lines")]
    [Authorize(Roles = "Dealer,Admin")]
    public async Task<ActionResult<OrderDto>> AddLine(Guid id, [FromBody] AddOrderLineRequest request)
    {
        var validation = await _addLineValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.ToDictionary());
        }

        var order = await _dbContext.Orders
            .Include(o => o.Dealer)
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId)
            ?? throw new KeyNotFoundException($"Product {request.ProductId} not found.");

        order.AddLine(product.Id, product.Sku, product.Name, request.Quantity, product.UnitPrice, product.VatRate);
        await _dbContext.SaveChangesAsync();

        return Ok(MapToDto(order));
    }

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    [Authorize(Roles = "Dealer,Admin")]
    public async Task<ActionResult<OrderDto>> UpdateLineQuantity(Guid id, Guid lineId, [FromBody] UpdateOrderLineRequest request)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Dealer)
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        order.UpdateLineQuantity(lineId, request.Quantity);
        await _dbContext.SaveChangesAsync();

        return Ok(MapToDto(order));
    }

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    [Authorize(Roles = "Dealer,Admin")]
    public async Task<ActionResult<OrderDto>> RemoveLine(Guid id, Guid lineId)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Dealer)
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        order.RemoveLine(lineId);
        await _dbContext.SaveChangesAsync();

        return Ok(MapToDto(order));
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "Dealer,Admin")]
    public async Task<ActionResult<OrderDto>> SubmitOrder(Guid id)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Dealer)
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        var dealer = order.Dealer
            ?? await _dbContext.Dealers.FirstOrDefaultAsync(d => d.Id == order.DealerId)
            ?? throw new KeyNotFoundException($"Dealer for order {id} not found.");

        order.Submit(dealer.CurrentBalance, dealer.CreditLimit);
        await _dbContext.SaveChangesAsync();

        return Ok(MapToDto(order));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "SalesRep,Admin")]
    public async Task<ActionResult<OrderDto>> ApproveOrder(Guid id)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Dealer)
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        // Check stock availability
        var productIds = order.Lines.Select(l => l.ProductId).ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var line in order.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product) || product.StockQuantity < line.Quantity)
            {
                throw new DomainException(
                    $"Insufficient stock for product '{line.ProductName}' ({line.Sku}). Available: {product?.StockQuantity ?? 0}, Requested: {line.Quantity}",
                    "INSUFFICIENT_STOCK");
            }
        }

        var approver = User.Identity?.Name ?? "SalesRep";
        order.Approve(approver);

        // Perform atomic invoice and stock decrement transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var invoiceCount = await _dbContext.Invoices.CountAsync() + 1;
            var invoiceNo = $"INV-{DateTime.UtcNow:yyyyMM}-{invoiceCount:D5}";
            var invoice = new Invoice(order.TenantId, invoiceNo, order.Id);

            foreach (var line in order.Lines)
            {
                var product = products[line.ProductId];
                product.DeductStock(line.Quantity);

                invoice.AddLine(line.ProductId, line.Sku, line.ProductName, line.Quantity, line.UnitPrice, line.VatRate);

                var movement = new StockMovement(
                    order.TenantId,
                    line.ProductId,
                    -line.Quantity,
                    StockMovementType.Sale,
                    order.Id,
                    $"Sale invoice {invoiceNo} for order {order.OrderNo}");

                _dbContext.StockMovements.Add(movement);
            }

            _dbContext.Invoices.Add(invoice);
            order.MarkInvoiced();

            // Increase dealer current balance
            if (order.Dealer is not null)
            {
                order.Dealer.CurrentBalance += order.TotalGross;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return Ok(MapToDto(order));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "SalesRep,Admin")]
    public async Task<ActionResult<OrderDto>> RejectOrder(Guid id, [FromBody] RejectOrderRequest request)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Dealer)
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException($"Order {id} not found.");

        var rejector = User.Identity?.Name ?? "SalesRep";
        order.Reject(request.Reason, rejector);
        await _dbContext.SaveChangesAsync();

        return Ok(MapToDto(order));
    }

    private static OrderDto MapToDto(Order order) => new(
        order.Id,
        order.TenantId,
        order.OrderNo,
        order.DealerId,
        order.Dealer?.Name,
        order.Status,
        order.ApprovedBy,
        order.ApprovedAtUtc,
        order.RejectionReason,
        order.CancellationReason,
        order.TotalNet,
        order.TotalVat,
        order.TotalGross,
        order.CreatedAtUtc,
        order.Lines.Select(l => new OrderLineDto(
            l.Id,
            l.ProductId,
            l.Sku,
            l.ProductName,
            l.Quantity,
            l.UnitPrice,
            l.VatRate,
            l.LineTotalNet,
            l.LineTotalVat,
            l.LineTotalGross)).ToList());
}
