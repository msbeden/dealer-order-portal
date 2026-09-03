using System.Security.Claims;
using DealerPortal.Application.Common.Pagination;
using DealerPortal.Application.DTOs.Dealers;
using DealerPortal.Application.DTOs.Insights;
using DealerPortal.Application.Interfaces;
using DealerPortal.Domain.Entities;
using DealerPortal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DealerPortal.Api.Controllers;

[ApiController]
[Route("api/v1/dealers")]
[Authorize(Roles = "Admin,SalesRep")]
public class DealersController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentTenant _currentTenant;
    private readonly IOrderInsightService _insightService;

    public DealersController(
        AppDbContext dbContext,
        ICurrentTenant currentTenant,
        IOrderInsightService insightService)
    {
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _insightService = insightService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<DealerDto>>> GetDealers([FromQuery] DealerFilterParams filter)
    {
        var query = _dbContext.Dealers.AsNoTracking().AsQueryable();

        // If current user is SalesRep, only display their own assigned dealers
        if (User.IsInRole("SalesRep"))
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdStr, out var salesRepUserId))
            {
                query = query.Where(d => d.SalesRepId == salesRepUserId);
            }
        }
        else if (filter.SalesRepId.HasValue)
        {
            query = query.Where(d => d.SalesRepId == filter.SalesRepId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToUpperInvariant();
            query = query.Where(d => d.Code.Contains(search) || d.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            query = query.Where(d => d.City == filter.City.Trim());
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(d => d.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(d => new DealerDto(
                d.Id,
                d.TenantId,
                d.Code,
                d.Name,
                d.City,
                d.CreditLimit,
                d.CurrentBalance,
                d.AvailableCredit,
                d.SalesRepId))
            .ToListAsync();

        return Ok(new PagedResult<DealerDto>(items, filter.Page, filter.PageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DealerDto>> GetDealerById(Guid id)
    {
        var dealer = await _dbContext.Dealers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dealer is null)
        {
            return NotFound();
        }

        // SalesRep can only view their own dealers
        if (User.IsInRole("SalesRep"))
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdStr, out var salesRepUserId) && dealer.SalesRepId != salesRepUserId)
            {
                return Forbid();
            }
        }

        return Ok(new DealerDto(
            dealer.Id,
            dealer.TenantId,
            dealer.Code,
            dealer.Name,
            dealer.City,
            dealer.CreditLimit,
            dealer.CurrentBalance,
            dealer.AvailableCredit,
            dealer.SalesRepId));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DealerDto>> CreateDealer([FromBody] CreateDealerRequest request)
    {
        var tenantId = _currentTenant.TenantId ?? throw new UnauthorizedAccessException("Tenant context required.");

        var dealer = new Dealer(
            tenantId,
            request.Code,
            request.Name,
            request.City,
            request.CreditLimit,
            request.SalesRepId);

        _dbContext.Dealers.Add(dealer);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDealerById), new { id = dealer.Id }, new DealerDto(
            dealer.Id,
            dealer.TenantId,
            dealer.Code,
            dealer.Name,
            dealer.City,
            dealer.CreditLimit,
            dealer.CurrentBalance,
            dealer.AvailableCredit,
            dealer.SalesRepId));
    }

    [HttpPost("{id:guid}/insight")]
    public async Task<ActionResult<DealerInsightDto>> GetDealerInsight(Guid id, CancellationToken cancellationToken)
    {
        var insight = await _insightService.GetDealerInsightAsync(id, cancellationToken);
        return Ok(insight);
    }
}
