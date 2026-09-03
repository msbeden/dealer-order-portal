using DealerPortal.Domain.Enums;
using DealerPortal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DealerPortal.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize(Roles = "Admin,SalesRep")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public ReportsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboardMetrics()
    {
        var totalOrders = await _dbContext.Orders.CountAsync();
        var pendingApproval = await _dbContext.Orders.CountAsync(o => o.Status == OrderStatus.Submitted);
        var invoicedOrders = await _dbContext.Orders.CountAsync(o => o.Status == OrderStatus.Invoiced);
        var totalRevenue = await _dbContext.Invoices.SumAsync(i => i.TotalGross);
        var activeDealers = await _dbContext.Dealers.CountAsync();

        return Ok(new
        {
            TotalOrders = totalOrders,
            PendingApproval = pendingApproval,
            InvoicedOrders = invoicedOrders,
            TotalRevenue = totalRevenue,
            ActiveDealers = activeDealers
        });
    }

    [HttpGet("top-products")]
    public async Task<ActionResult> GetTopProducts([FromQuery] int limit = 5)
    {
        var topProducts = await _dbContext.InvoiceLines
            .AsNoTracking()
            .GroupBy(l => new { l.ProductId, l.ProductName, l.Sku })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.Sku,
                TotalQuantity = g.Sum(l => l.Quantity),
                TotalGross = g.Sum(l => l.LineTotalGross)
            })
            .OrderByDescending(p => p.TotalGross)
            .Take(limit)
            .ToListAsync();

        return Ok(topProducts);
    }
}
