using DealerPortal.Application.Interfaces;
using DealerPortal.Domain.Common;
using DealerPortal.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DealerPortal.Infrastructure.Data.Interceptors;

public class TenantSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentTenant _currentTenant;

    public TenantSaveChangesInterceptor(ICurrentTenant currentTenant)
    {
        _currentTenant = currentTenant;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var activeTenantId = _currentTenant.TenantId;

        foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId == Guid.Empty && activeTenantId.HasValue)
                {
                    entry.Entity.TenantId = activeTenantId.Value;
                }
            }
            else if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                // Prevent tampering with cross-tenant entities
                if (activeTenantId.HasValue && entry.Entity.TenantId != activeTenantId.Value)
                {
                    throw new DomainException(
                        $"Cross-tenant operation denied. Active tenant: {activeTenantId}, Target tenant: {entry.Entity.TenantId}",
                        "CROSS_TENANT_VIOLATION");
                }
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAtUtc == default)
                {
                    entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
    }
}
