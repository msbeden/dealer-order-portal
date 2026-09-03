using DealerPortal.Application.Common.Pagination;
using DealerPortal.Application.DTOs.Products;
using DealerPortal.Application.Interfaces;
using DealerPortal.Domain.Entities;
using DealerPortal.Infrastructure.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DealerPortal.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;
    private readonly IValidator<CreateProductRequest> _createValidator;

    public ProductsController(
        AppDbContext dbContext,
        ICurrentTenant currentTenant,
        IValidator<CreateProductRequest> createValidator)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts([FromQuery] ProductFilterParams filter)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToUpperInvariant();
            query = query.Where(p => p.Sku.Contains(search) || p.Name.Contains(search));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
        }

        if (filter.InStockOnly)
        {
            query = query.Where(p => p.StockQuantity > 0);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(p => new ProductDto(
                p.Id,
                p.TenantId,
                p.Sku,
                p.Name,
                p.CategoryId,
                p.Category != null ? p.Category.Name : null,
                p.UnitPrice,
                p.VatRate,
                p.StockQuantity,
                p.RowVersion))
            .ToListAsync();

        return Ok(new PagedResult<ProductDto>(items, filter.Page, filter.PageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetProductById(Guid id)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(new ProductDto(
            product.Id,
            product.TenantId,
            product.Sku,
            product.Name,
            product.CategoryId,
            product.Category?.Name,
            product.UnitPrice,
            product.VatRate,
            product.StockQuantity,
            product.RowVersion));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(validation.ToDictionary());
        }

        var tenantId = _currentTenant.TenantId ?? throw new UnauthorizedAccessException("Tenant context required.");

        var product = new Product(
            tenantId,
            request.Sku,
            request.Name,
            request.CategoryId,
            request.UnitPrice,
            request.VatRate,
            request.InitialStock);

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, new ProductDto(
            product.Id,
            product.TenantId,
            product.Sku,
            product.Name,
            product.CategoryId,
            null,
            product.UnitPrice,
            product.VatRate,
            product.StockQuantity,
            product.RowVersion));
    }
}
