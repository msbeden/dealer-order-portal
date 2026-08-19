using DealerPortal.Application.DTOs.Insights;

namespace DealerPortal.Application.Interfaces;

/// <summary>
/// Service providing LLM-backed strategic purchase insights and anomaly summaries for dealers.
/// Implementations must use pre-aggregated metrics to guarantee customer PII is never transmitted.
/// </summary>
public interface IOrderInsightService
{
    Task<DealerInsightDto> GetDealerInsightAsync(Guid dealerId, CancellationToken cancellationToken = default);
}
