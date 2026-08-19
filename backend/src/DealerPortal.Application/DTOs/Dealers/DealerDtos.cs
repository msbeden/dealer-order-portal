using DealerPortal.Application.Common.Pagination;

namespace DealerPortal.Application.DTOs.Dealers;

public record DealerDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    string City,
    decimal CreditLimit,
    decimal CurrentBalance,
    decimal AvailableCredit,
    Guid? SalesRepId);

public record CreateDealerRequest(
    string Code,
    string Name,
    string City,
    decimal CreditLimit,
    Guid? SalesRepId);

public record UpdateDealerRequest(
    string Name,
    string City,
    decimal CreditLimit,
    Guid? SalesRepId);

public record DealerFilterParams : PaginationParams
{
    public string? Search { get; init; }
    public string? City { get; init; }
    public Guid? SalesRepId { get; init; }
}
