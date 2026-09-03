using DealerPortal.Application.Common.Pagination;
using DealerPortal.Application.DTOs.Invoices;
using DealerPortal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DealerPortal.Api.Controllers;

[ApiController]
[Route("api/v1/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public InvoicesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> GetInvoices([FromQuery] InvoiceFilterParams filter)
    {
        var query = _dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .AsQueryable();

        if (filter.FromDate.HasValue)
        {
            query = query.Where(i => i.IssuedAtUtc >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(i => i.IssuedAtUtc <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync();

        var invoices = await query
            .OrderByDescending(i => i.IssuedAtUtc)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = invoices.Select(i => new InvoiceDto(
            i.Id,
            i.TenantId,
            i.InvoiceNo,
            i.OrderId,
            i.IssuedAtUtc,
            i.TotalNet,
            i.TotalVat,
            i.TotalGross,
            i.Lines.Select(l => new InvoiceLineDto(
                l.Id,
                l.ProductId,
                l.Sku,
                l.ProductName,
                l.Quantity,
                l.UnitPrice,
                l.VatRate,
                l.LineTotalNet,
                l.LineTotalVat,
                l.LineTotalGross)).ToList())).ToList();

        return Ok(new PagedResult<InvoiceDto>(items, filter.Page, filter.PageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoiceById(Guid id)
    {
        var invoice = await _dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice is null)
        {
            return NotFound();
        }

        return Ok(new InvoiceDto(
            invoice.Id,
            invoice.TenantId,
            invoice.InvoiceNo,
            invoice.OrderId,
            invoice.IssuedAtUtc,
            invoice.TotalNet,
            invoice.TotalVat,
            invoice.TotalGross,
            invoice.Lines.Select(l => new InvoiceLineDto(
                l.Id,
                l.ProductId,
                l.Sku,
                l.ProductName,
                l.Quantity,
                l.UnitPrice,
                l.VatRate,
                l.LineTotalNet,
                l.LineTotalVat,
                l.LineTotalGross)).ToList()));
    }
}
