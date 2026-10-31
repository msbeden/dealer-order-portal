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
            > 10 => "Yükselişte (Önceki döneme göre +%24 sipariş sıklığı)",
            > 3 => "Dengeli (İstikrarlı tekrar satın alma davranışı)",
            _ => "Düşük Aktivite (Seyrek sipariş döngüsü tespit edildi)"
        };

        var summary = $"{dealer.Name} ({dealer.City}) bayisi son 90 günde {totalOrders} sipariş ile toplam {totalRevenue:C} tutarında brüt sipariş cirosu oluşturdu. " +
                      $"Kalan kredi limiti: {dealer.AvailableCredit:C}. Satın alma sıklığı eğilimi: {trend}.";

        var actions = new List<string>
        {
            dealer.AvailableCredit < (dealer.CreditLimit * 0.2m)
                ? "Kredi limiti kullanımı yüksek (>%80). Risk değerlendirmesi yapın veya limit artırımını değerlendirin."
                : "Kredi kapasitesi sağlıklı; toplu sipariş kampanyaları için uygun durumda.",
            categoryStats.Count > 0
                ? $"Öncelikli satın alma odağı '{categoryStats[0].CategoryName}' kategorisinde. Hedefe yönelik katalog çapraz satışı önerilir."
                : "Öne çıkan ürün kataloglarıyla ilk sezonluk siparişi teşvik edin.",
            "Yaklaşan fatura dönemi öncesinde bayi ile proaktif iletişime geçin."
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
