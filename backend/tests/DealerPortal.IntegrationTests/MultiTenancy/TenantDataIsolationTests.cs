using DealerPortal.Application.Interfaces;
using DealerPortal.Domain.Entities;
using DealerPortal.Domain.Exceptions;
using DealerPortal.Infrastructure.Data;
using DealerPortal.Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace DealerPortal.IntegrationTests.MultiTenancy;

public class TestTenantProvider : ICurrentTenant
{
    public Guid? TenantId { get; set; }
    public string? TenantCode { get; set; }
}

public class TenantDataIsolationTests
{
    private static (AppDbContext Context, TestTenantProvider Provider) CreateTestContext()
    {
        var tenantProvider = new TestTenantProvider();
        var interceptor = new TenantSaveChangesInterceptor(tenantProvider);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        var context = new AppDbContext(options, tenantProvider);
        return (context, tenantProvider);
    }

    [Fact]
    public async Task Query_WhenTenantAIsActive_CannotSeeTenantBData()
    {
        var (context, provider) = CreateTestContext();

        var tenantA = new Tenant("Tenant A Alpha", "TNA");
        var tenantB = new Tenant("Tenant B Beta", "TNB");

        context.Tenants.AddRange(tenantA, tenantB);
        await context.SaveChangesAsync();

        var orderA = new Order(tenantA.Id, "ORD-A-001", Guid.NewGuid());
        var orderB = new Order(tenantB.Id, "ORD-B-001", Guid.NewGuid());

        context.Orders.AddRange(orderA, orderB);
        await context.SaveChangesAsync();

        // Switch to Tenant A context
        provider.TenantId = tenantA.Id;

        var visibleOrders = await context.Orders.ToListAsync();

        Assert.Single(visibleOrders);
        Assert.Equal("ORD-A-001", visibleOrders[0].OrderNo);

        // Attempting to retrieve Tenant B's order directly must return null (404)
        var orderFromB = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderB.Id);
        Assert.Null(orderFromB);
    }

    [Fact]
    public async Task SaveChanges_AutoAssignsActiveTenantId_ToNewTenantEntities()
    {
        var (context, provider) = CreateTestContext();

        var tenant = new Tenant("Acme Corp", "ACM");
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        // Set active tenant
        provider.TenantId = tenant.Id;

        // Create category without setting TenantId
        var category = new Category(Guid.Empty, "Cloud Services");
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        Assert.Equal(tenant.Id, category.TenantId);
    }

    [Fact]
    public async Task SaveChanges_ThrowsException_WhenTamperingCrossTenantEntity()
    {
        var (context, provider) = CreateTestContext();

        var tenantA = new Tenant("Tenant A", "TNA");
        var tenantB = new Tenant("Tenant B", "TNB");
        context.Tenants.AddRange(tenantA, tenantB);
        await context.SaveChangesAsync();

        var categoryB = new Category(tenantB.Id, "Hardware B");
        context.Categories.Add(categoryB);
        await context.SaveChangesAsync();

        // Switch active tenant to Tenant A
        provider.TenantId = tenantA.Id;

        // Attempt to modify Tenant B's category while in Tenant A context
        categoryB.Name = "Hijacked Name";
        context.Entry(categoryB).State = EntityState.Modified;

        var ex = await Assert.ThrowsAsync<DomainException>(() => context.SaveChangesAsync());
        Assert.Equal("CROSS_TENANT_VIOLATION", ex.ErrorCode);
    }
}
