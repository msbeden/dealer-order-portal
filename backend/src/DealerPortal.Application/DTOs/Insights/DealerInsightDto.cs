namespace DealerPortal.Application.DTOs.Insights;

public record TopCategoryDto(string CategoryName, decimal TotalSpent, int OrderCount);

public record DealerInsightDto(
    Guid DealerId,
    string DealerName,
    decimal TotalRevenueLast90Days,
    int TotalOrdersLast90Days,
    IReadOnlyList<TopCategoryDto> TopCategories,
    string FrequencyTrend,
    string ExecutiveSummary,
    IReadOnlyList<string> RecommendedActions,
    DateTime GeneratedAtUtc);
