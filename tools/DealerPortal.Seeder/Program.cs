using System.Diagnostics;
using Bogus;
using DealerPortal.Application.Interfaces;
using DealerPortal.Domain.Entities;
using DealerPortal.Domain.Enums;
using DealerPortal.Infrastructure.Data;
using DealerPortal.Seeder;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("=================================================");
Console.WriteLine("  DealerPortal High-Volume Benchmark Data Seeder ");
Console.WriteLine("=================================================");

var connectionString = args.Length > 0
    ? args[0]
    : "Server=localhost,1433;Database=DealerPortalDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true";

var dummyTenant = new SeederTenantProvider();
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(connectionString)
    .Options;

using var context = new AppDbContext(options, dummyTenant);
context.ChangeTracker.AutoDetectChangesEnabled = false;

Console.WriteLine("Connecting to database and ensuring schema...");
await context.Database.EnsureCreatedAsync();

var stopwatch = Stopwatch.StartNew();

// 1. Seed Tenants
Console.WriteLine("Generating tenants...");
var tenants = new List<Tenant>
{
    new("Anadolu Dağıtım A.Ş.", "AND"),
    new("Marmara B2B Tedarik", "MRM"),
    new("Ege Perakende & Toptan", "EGE")
};

foreach (var t in tenants)
{
    if (!await context.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Code == t.Code))
    {
        context.Tenants.Add(t);
    }
}
await context.SaveChangesAsync();
var savedTenants = await context.Tenants.IgnoreQueryFilters().ToListAsync();

// 2. Seed Categories per tenant
Console.WriteLine("Generating categories...");
var categoryNames = new[] { "Dizüstü Bilgisayar", "Masaüstü PC", "Ağ Cihazları", "Sunucular", "Yazıcılar", "Monitörler", "Ofis Mobilyaları", "Güç Kaynakları" };
var categories = new List<Category>();

foreach (var tenant in savedTenants)
{
    foreach (var catName in categoryNames)
    {
        var existing = await context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Name == catName);
        if (existing is null)
        {
            var cat = new Category(tenant.Id, catName);
            context.Categories.Add(cat);
            categories.Add(cat);
        }
        else
        {
            categories.Add(existing);
        }
    }
}
await context.SaveChangesAsync();

// 3. Seed Dealers using Bogus
Console.WriteLine("Seeding dealers with Bogus...");
var turkishCities = new[] { "İstanbul", "Ankara", "İzmir", "Bursa", "Antalya", "Kocaeli", "Adana", "Gaziantep", "Konya", "Kayseri" };
var dealerFaker = new Faker<Dealer>("tr")
    .CustomInstantiator(f =>
    {
        var tenant = f.PickRandom(savedTenants);
        return new Dealer(
            tenant.Id,
            $"DLR-{f.Random.AlphaNumeric(6).ToUpperInvariant()}",
            f.Company.CompanyName(),
            f.PickRandom(turkishCities),
            f.Random.Decimal(50000m, 500000m));
    });

var newDealers = dealerFaker.Generate(200);
context.Dealers.AddRange(newDealers);
await context.SaveChangesAsync();

// 4. Seed Products in batches using Bogus
Console.WriteLine("Seeding benchmark products in batches...");
var allCategories = await context.Categories.IgnoreQueryFilters().ToListAsync();

var productFaker = new Faker<Product>("tr")
    .CustomInstantiator(f =>
    {
        var cat = f.PickRandom(allCategories);
        return new Product(
            cat.TenantId,
            $"SKU-{f.Random.AlphaNumeric(8).ToUpperInvariant()}",
            $"{f.Commerce.ProductAdjective()} {f.Commerce.ProductName()}",
            cat.Id,
            f.Random.Decimal(100m, 45000m),
            20m,
            f.Random.Int(5, 500));
    });

const int totalProducts = 2000;
const int batchSize = 500;

for (var i = 0; i < totalProducts; i += batchSize)
{
    var batch = productFaker.Generate(batchSize);
    context.Products.AddRange(batch);
    await context.SaveChangesAsync();
    Console.WriteLine($"  Inserted {i + batchSize}/{totalProducts} products...");
}

stopwatch.Stop();
Console.WriteLine($"\n[SUCCESS] Seed data completed in {stopwatch.ElapsedMilliseconds} ms.");

namespace DealerPortal.Seeder
{
    public class SeederTenantProvider : ICurrentTenant
    {
        public Guid? TenantId => null;
        public string? TenantCode => null;
    }
}
