using System.Text;
using DealerPortal.Api.Data;
using DealerPortal.Api.Middleware;
using DealerPortal.Api.Services;
using DealerPortal.Application.Interfaces;
using DealerPortal.Application.Validators;
using DealerPortal.Infrastructure.Data;
using DealerPortal.Infrastructure.Data.Interceptors;
using DealerPortal.Infrastructure.Identity;
using DealerPortal.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Database & Multi-tenancy
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenant, CurrentTenant>();
builder.Services.AddScoped<TenantSaveChangesInterceptor>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<TenantSaveChangesInterceptor>();
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost,1433;Database=DealerPortalDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true";

    options.UseSqlServer(connectionString)
           .AddInterceptors(interceptor);
});

// 2. Identity
builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddRoles<AppRole>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3. JWT Authentication
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? "EnterpriseSecretKeyForDealerPortalMultiTenantJwtAuth2026!";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "DealerPortalApi",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "DealerPortalClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Application Services & Validators
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IOrderInsightService, FakeOrderInsightService>();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

// 5. Controllers & JSON serialization
builder.Services.AddControllers();

// 6. OpenAPI / Swagger
builder.Services.AddOpenApi();

// 7. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed initial data
try
{
    await DataSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
#pragma warning disable CA1848
    app.Logger.LogWarning(ex, "Initial database seeding skipped or deferred (database might not be online yet).");
#pragma warning restore CA1848
}

// Global Exception Handling Middleware (RFC 7807)
app.UseMiddleware<ProblemDetailsMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Bayi Sipariş Portalı API")
               .WithTheme(Scalar.AspNetCore.ScalarTheme.Moon);
    });
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
