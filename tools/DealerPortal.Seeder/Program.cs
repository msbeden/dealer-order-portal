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
    : "Server=localhost,14333;Database=DealerPortalDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true";

var dummyTenant = new SeederTenantProvider();
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(connectionString)
    .Options;

using var context = new AppDbContext(options, dummyTenant);
context.ChangeTracker.AutoDetectChangesEnabled = false;

Console.WriteLine("Connecting to database and ensuring schema...");
await context.Database.EnsureCreatedAsync();

var stopwatch = Stopwatch.StartNew();

// 1. Ensure Default & Benchmark Tenants
Console.WriteLine("Generating tenants...");
var tenantsToSeed = new List<Tenant>
{
    new("TR B2B Network (Demo)", "DEF"),
    new("Anadolu Dağıtım A.Ş.", "AND"),
    new("Marmara B2B Tedarik", "MRM"),
    new("Ege Perakende & Toptan", "EGE")
};

foreach (var t in tenantsToSeed)
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
var categoryNames = new[] { "Bilgisayar & Donanım", "Ofis & Çevre Birimleri", "Ağ & Güvenlik", "Sunucu & Depolama", "Yazılım & Lisans", "Görüntüleme & Ses", "Güç Sistemleri & UPS" };
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
var allCategories = await context.Categories.IgnoreQueryFilters().ToListAsync();

// 3. Find demo SalesRep user to link dealers
var salesRepUser = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "salesrep@dealerportal.com");
var salesRepId = salesRepUser?.Id;

// 4. Seed Dealers using Bogus
Console.WriteLine("Seeding 100+ dealers with Bogus...");
var turkishCities = new[] { "İstanbul", "Ankara", "İzmir", "Bursa", "Antalya", "Kocaeli", "Adana", "Gaziantep", "Konya", "Kayseri", "Eskişehir", "Samsun", "Denizli" };
var dealerFaker = new Faker<Dealer>("tr")
    .CustomInstantiator(f =>
    {
        var tenant = f.PickRandom(savedTenants);
        return new Dealer(
            tenant.Id,
            $"DLR-{f.Random.AlphaNumeric(6).ToUpperInvariant()}",
            f.Company.CompanyName(),
            f.PickRandom(turkishCities),
            f.Random.Decimal(250000m, 2500000m),
            salesRepId);
    });

var newDealers = dealerFaker.Generate(120);
context.Dealers.AddRange(newDealers);
await context.SaveChangesAsync();
var allDealers = await context.Dealers.IgnoreQueryFilters().ToListAsync();

// 5. Seed 5,000 Products in batches
Console.WriteLine("Seeding 5,000 products in batches...");
var prefixes = new Dictionary<string, string>
{
    { "Bilgisayar & Donanım", "PC" },
    { "Ofis & Çevre Birimleri", "OFC" },
    { "Ağ & Güvenlik", "NET" },
    { "Sunucu & Depolama", "SRV" },
    { "Yazılım & Lisans", "SFT" },
    { "Görüntüleme & Ses", "DSP" },
    { "Güç Sistemleri & UPS", "PWR" }
};

var productFaker = new Faker<Product>("tr")
    .CustomInstantiator(f =>
    {
        var cat = f.PickRandom(allCategories);
        var prefix = prefixes.TryGetValue(cat.Name, out var p) ? p : "PRD";
        var sku = $"{prefix}-{f.Random.Number(100, 999)}-{f.Random.AlphaNumeric(4).ToUpperInvariant()}";
        var name = $"{f.Commerce.ProductAdjective()} {f.Commerce.ProductName()} {f.Random.Word()}";
        var price = f.Random.Decimal(250m, 65000m);
        var vat = f.Random.Bool() ? 10m : 20m;
        var stock = f.Random.Int(10, 500);

        return new Product(cat.TenantId, sku, name, cat.Id, price, vat, stock);
    });

const int totalProducts = 5000;
const int batchSize = 1000;

for (var i = 0; i < totalProducts; i += batchSize)
{
    var batch = productFaker.Generate(batchSize);
    context.Products.AddRange(batch);
    await context.SaveChangesAsync();
    Console.WriteLine($"  Inserted {i + batchSize}/{totalProducts} products...");
}

var allProducts = await context.Products.IgnoreQueryFilters().ToListAsync();

// 6. Seed Orders, OrderLines, Invoices and StockMovements
Console.WriteLine("Seeding 400+ realistic orders and invoices...");
var ordersList = new List<Order>();
var invoicesList = new List<Invoice>();
var movementsList = new List<StockMovement>();

var orderCounter = await context.Orders.IgnoreQueryFilters().CountAsync() + 1000;
var invoiceCounter = await context.Invoices.IgnoreQueryFilters().CountAsync() + 1000;

var faker = new Faker("tr");

foreach (var dealer in allDealers.Take(80))
{
    var ordersCount = faker.Random.Int(3, 8);
    for (var o = 0; o < ordersCount; o++)
    {
        orderCounter++;
        var orderNo = $"ORD-{DateTime.UtcNow:yyyyMM}-{orderCounter:D5}";
        var order = new Order(dealer.TenantId, orderNo, dealer.Id);

        // Pick 2-5 products for this tenant
        var tenantProducts = allProducts.Where(p => p.TenantId == dealer.TenantId).ToList();
        if (tenantProducts.Count == 0)
        {
            continue;
        }

        var lineCount = faker.Random.Int(2, 5);
        var selectedProducts = faker.PickRandom(tenantProducts, lineCount).ToList();

        foreach (var prod in selectedProducts)
        {
            var qty = faker.Random.Int(1, 10);
            order.AddLine(prod.Id, prod.Sku, prod.Name, qty, prod.UnitPrice, prod.VatRate);
        }

        if (dealer.CreditLimit < dealer.CurrentBalance + order.TotalGross)
        {
            dealer.CreditLimit = dealer.CurrentBalance + order.TotalGross + 500000m;
        }

        // Random status distribution: 30% Submitted (pending), 60% Invoiced, 10% Approved
        var roll = faker.Random.Int(1, 100);
        if (roll <= 30)
        {
            order.Submit(dealer.CurrentBalance, dealer.CreditLimit);
        }
        else
        {
            order.Submit(dealer.CurrentBalance, dealer.CreditLimit);
            order.Approve("SalesRep");
            order.MarkInvoiced();

            invoiceCounter++;
            var invNo = $"INV-{DateTime.UtcNow:yyyyMM}-{invoiceCounter:D5}";
            var invoice = new Invoice(dealer.TenantId, invNo, order.Id);

            foreach (var line in order.Lines)
            {
                invoice.AddLine(line.ProductId, line.Sku, line.ProductName, line.Quantity, line.UnitPrice, line.VatRate);

                var movement = new StockMovement(
                    dealer.TenantId,
                    line.ProductId,
                    -line.Quantity,
                    StockMovementType.Sale,
                    order.Id,
                    $"Seeded Sale invoice {invNo}");
                movementsList.Add(movement);
            }

            invoicesList.Add(invoice);
            dealer.CurrentBalance += order.TotalGross;
        }

        ordersList.Add(order);
    }
}

context.Orders.AddRange(ordersList);
context.Invoices.AddRange(invoicesList);
context.StockMovements.AddRange(movementsList);
await context.SaveChangesAsync();

stopwatch.Stop();
Console.WriteLine($"\n=================================================");
Console.WriteLine($"[SUCCESS] High-Volume Benchmark Seeding Complete!");
Console.WriteLine($"  - Tenants: {savedTenants.Count}");
Console.WriteLine($"  - Categories: {allCategories.Count}");
Console.WriteLine($"  - Dealers: {allDealers.Count}");
Console.WriteLine($"  - Products: {await context.Products.IgnoreQueryFilters().CountAsync()}");
Console.WriteLine($"  - Orders: {await context.Orders.IgnoreQueryFilters().CountAsync()}");
Console.WriteLine($"  - Invoices: {await context.Invoices.IgnoreQueryFilters().CountAsync()}");
Console.WriteLine($"  - Total Execution Time: {stopwatch.ElapsedMilliseconds} ms");
Console.WriteLine($"=================================================");

namespace DealerPortal.Seeder
{
    public class SeederTenantProvider : ICurrentTenant
    {
        public Guid? TenantId => null;
        public string? TenantCode => null;
    }
}
