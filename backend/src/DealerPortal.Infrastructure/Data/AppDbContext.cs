using System.Reflection;
using DealerPortal.Application.Interfaces;
using DealerPortal.Domain.Common;
using DealerPortal.Domain.Entities;
using DealerPortal.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DealerPortal.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    private readonly ICurrentTenant _currentTenant;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Dealer> Dealers => Set<Dealer>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentTenant currentTenant) : base(options)
    {
        _currentTenant = currentTenant;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply tenant isolation global query filters
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var filterMethod = typeof(AppDbContext)
                    .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                filterMethod.Invoke(this, [builder]);
            }
        }

        // Decimal precision for monetary amounts
        foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        // Entity-specific configurations
        builder.Entity<Tenant>(b =>
        {
            b.HasIndex(t => t.Code).IsUnique();
            b.Property(t => t.Name).HasMaxLength(150).IsRequired();
            b.Property(t => t.Code).HasMaxLength(20).IsRequired();
        });

        builder.Entity<Dealer>(b =>
        {
            b.HasIndex(d => new { d.TenantId, d.Code }).IsUnique();
            b.Property(d => d.Code).HasMaxLength(50).IsRequired();
            b.Property(d => d.Name).HasMaxLength(200).IsRequired();
            b.Property(d => d.City).HasMaxLength(100).IsRequired();
        });

        builder.Entity<Category>(b =>
        {
            b.HasIndex(c => new { c.TenantId, c.Name });
            b.Property(c => c.Name).HasMaxLength(100).IsRequired();
        });

        builder.Entity<Product>(b =>
        {
            b.HasIndex(p => new { p.TenantId, p.Sku }).IsUnique();
            b.Property(p => p.Sku).HasMaxLength(50).IsRequired();
            b.Property(p => p.Name).HasMaxLength(200).IsRequired();
            b.Property(p => p.RowVersion).IsRowVersion().IsConcurrencyToken();

            b.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Order>(b =>
        {
            b.HasIndex(o => new { o.TenantId, o.OrderNo }).IsUnique();
            b.HasIndex(o => new { o.TenantId, o.DealerId, o.CreatedAtUtc });
            b.Property(o => o.OrderNo).HasMaxLength(50).IsRequired();

            b.HasOne(o => o.Dealer)
                .WithMany()
                .HasForeignKey(o => o.DealerId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(o => o.Lines)
                .WithOne(l => l.Order)
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderLine>(b =>
        {
            b.Property(l => l.Sku).HasMaxLength(50).IsRequired();
            b.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        });

        builder.Entity<Invoice>(b =>
        {
            b.HasIndex(i => new { i.TenantId, i.InvoiceNo }).IsUnique();
            b.Property(i => i.InvoiceNo).HasMaxLength(50).IsRequired();

            b.HasOne(i => i.Order)
                .WithMany()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(i => i.Lines)
                .WithOne(l => l.Invoice)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StockMovement>(b =>
        {
            b.HasIndex(s => new { s.TenantId, s.ProductId, s.CreatedAtUtc });

            b.HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder builder) where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e =>
            !_currentTenant.TenantId.HasValue || e.TenantId == _currentTenant.TenantId.Value);
    }
}
