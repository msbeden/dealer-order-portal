using DealerPortal.Application.DTOs.Insights;
using DealerPortal.Application.Interfaces;
using DealerPortal.Domain.Enums;
using DealerPortal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DealerPortal.Api.Services;

public class FakeOrderInsightService : IOrderInsightService
{
    private readonly AppDbContext _dbContext;

    public FakeOrderInsightService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DealerInsightDto> GetDealerInsightAsync(Guid dealerId, CancellationToken cancellationToken = default)
    {
        var dealer = await _dbContext.Dealers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dealerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Dealer with ID {dealerId} not found.");

        var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);

        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .Where(o => o.DealerId == dealerId && o.CreatedAtUtc >= ninetyDaysAgo && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Rejected)
            .ToListAsync(cancellationToken);

        var totalRevenue = orders.Sum(o => o.TotalGross);
        var totalOrders = orders.Count;

        // Group order lines by category to find top categories
        var productIds = orders.SelectMany(o => o.Lines).Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var categoryStats = orders
            .SelectMany(o => o.Lines)
            .GroupBy(l => products.TryGetValue(l.ProductId, out var prod) && prod.Category is not null ? prod.Category.Name : "General")
            .Select(g => new TopCategoryDto(
                CategoryName: g.Key,
                TotalSpent: g.Sum(l => l.LineTotalGross),
                OrderCount: g.Count()))
            .OrderByDescending(c => c.TotalSpent)
            .Take(5)
            .ToList();

        var trend = totalOrders switch
        {
            > 10 => "Ascending (+24% order frequency compared to previous period)",
            > 3 => "Stable (Consistent repeat purchase behavior)",
            _ => "Low activity (Infrequent purchase cycle detected)"
        };

        var summary = $"Dealer {dealer.Name} ({dealer.City}) generated a gross order turnover of {totalRevenue:C} across {totalOrders} orders in the last 90 days. " +
                      $"Remaining credit limit is {dealer.AvailableCredit:C}. Purchase frequency reflects a {trend.ToLowerInvariant()} pattern.";

        var actions = new List<string>
        {
            dealer.AvailableCredit < (dealer.CreditLimit * 0.2m)
                ? "Credit limit utilization is high (>80%). Evaluate for risk or consider limit revision."
                : "Credit capacity is healthy; open for bulk ordering promotions.",
            categoryStats.Count > 0
                ? $"Primary purchasing focus is in '{categoryStats[0].CategoryName}'. Recommend targeted catalog cross-sells."
                : "Encourage first seasonal order with catalog highlights.",
            "Schedule proactive contact prior to upcoming billing period."
        };

        return new DealerInsightDto(
            DealerId: dealer.Id,
            DealerName: dealer.Name,
            TotalRevenueLast90Days: totalRevenue,
            TotalOrdersLast90Days: totalOrders,
            TopCategories: categoryStats,
            FrequencyTrend: trend,
            ExecutiveSummary: summary,
            RecommendedActions: actions,
            GeneratedAtUtc: DateTime.UtcNow);
    }
}
