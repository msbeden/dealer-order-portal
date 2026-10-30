using DealerPortal.Domain.Entities;
using DealerPortal.Domain.Enums;
using DealerPortal.Infrastructure.Data;
using DealerPortal.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DealerPortal.Api.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

        // Ensure database schema is created
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        string[] roles = ["Admin", "SalesRep", "Dealer"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new AppRole(role));
            }
        }

        // 2. Seed Default Tenant
        var tenant = await context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Code == "DEF");
        if (tenant is null)
        {
            tenant = new Tenant("TR B2B Network", "DEF");
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
        }

        // 3. Seed Categories
        var catHardware = await context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Name == "Bilgisayar & Donanım");
        if (catHardware is null)
        {
            catHardware = new Category(tenant.Id, "Bilgisayar & Donanım");
            context.Categories.Add(catHardware);
            await context.SaveChangesAsync();
        }

        var catOffice = await context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.Name == "Ofis & Çevre Birimleri");
        if (catOffice is null)
        {
            catOffice = new Category(tenant.Id, "Ofis & Çevre Birimleri");
            context.Categories.Add(catOffice);
            await context.SaveChangesAsync();
        }

        // 4. Seed Products
        if (!await context.Products.IgnoreQueryFilters().AnyAsync(p => p.TenantId == tenant.Id))
        {
            var products = new List<Product>
            {
                new(tenant.Id, "LAP-001", "Kurumsal Ultrabook 14\" i7 32GB", catHardware.Id, 38500m, 20m, 45),
                new(tenant.Id, "SRV-002", "Rackmount Sunucu 2U Xeon 64GB", catHardware.Id, 115000m, 20m, 12),
                new(tenant.Id, "MON-003", "27\" 4K IPS Ergonomik Monitör", catOffice.Id, 12400m, 20m, 80),
                new(tenant.Id, "PRN-004", "Çok Fonksiyonlu Lazer Yazıcı", catOffice.Id, 18900m, 20m, 25),
                new(tenant.Id, "NET-005", "24 Port Gigabit Yönetilebilir Switch", catHardware.Id, 14200m, 20m, 35)
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }

        // 5. Seed SalesRep User
        var salesRepEmail = "salesrep@dealerportal.com";
        var salesRepUser = await userManager.FindByEmailAsync(salesRepEmail);
        if (salesRepUser is null)
        {
            salesRepUser = new AppUser
            {
                UserName = salesRepEmail,
                Email = salesRepEmail,
                FullName = "Ahmet Yılmaz (Plasiyer)",
                TenantId = tenant.Id,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(salesRepUser, "SalesRep123!");
            await userManager.AddToRoleAsync(salesRepUser, "SalesRep");
        }

        // 6. Seed Dealer
        var dealer = await context.Dealers.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.TenantId == tenant.Id && d.Code == "BAYI-01");
        if (dealer is null)
        {
            dealer = new Dealer(tenant.Id, "BAYI-01", "Mega Bilişim Ltd. Şti.", "İstanbul", 250000m, salesRepUser.Id);
            context.Dealers.Add(dealer);
            await context.SaveChangesAsync();
        }

        // 7. Seed Admin User
        var adminEmail = "admin@dealerportal.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Sistem Yöneticisi",
                TenantId = tenant.Id,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(adminUser, "Admin123!");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // 8. Seed Dealer User
        var dealerEmails = new[] { "dealer@dealerportal.com", "dealer1@dealerportal.com" };
        foreach (var email in dealerEmails)
        {
            var dealerUser = await userManager.FindByEmailAsync(email);
            if (dealerUser is null)
            {
                dealerUser = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FullName = "Mehmet Bayi Yetkilisi",
                    TenantId = tenant.Id,
                    DealerId = dealer.Id,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(dealerUser, "Dealer123!");
                await userManager.AddToRoleAsync(dealerUser, "Dealer");
            }
        }
    }
}
