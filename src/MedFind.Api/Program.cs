using FluentValidation;
using MedFind.Application.DTOs;
using MedFind.Application.Interfaces;
using MedFind.Application.Validators;
using MedFind.Api.Middleware;
using MedFind.Infrastructure.Caching;
using MedFind.Infrastructure.Data;
using MedFind.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Text;

// ── Serilog bootstrap ───────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ──────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}"));

    // ── EF Core + PostGIS ────────────────────────────────────────────────────
    var connStr = builder.Configuration.GetConnectionString("Default")
        ?? "Host=localhost;Port=5432;Database=medfind;Username=medfind;Password=medfind";
    builder.Services.AddDbContext<MedFindDbContext>(opt =>
        opt.UseNpgsql(connStr, npgsql => npgsql.UseNetTopologySuite())
           .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));

    // ── Redis ────────────────────────────────────────────────────────────────
    var redisConn = builder.Configuration["Redis:Connection"] ?? "localhost:6379";
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConn));
    builder.Services.AddScoped<ICacheService, RedisCacheService>();

    // ── Application services ─────────────────────────────────────────────────
    builder.Services.AddScoped<IAuthService, AuthService>();

    // NearbyService with caching decorator (DI wires inner → outer)
    builder.Services.AddScoped<NearbyService>();
    builder.Services.AddScoped<INearbyService>(sp =>
        new CachedNearbyService(
            sp.GetRequiredService<NearbyService>(),
            sp.GetRequiredService<ICacheService>(),
            sp.GetRequiredService<ILogger<CachedNearbyService>>()));

    builder.Services.AddScoped<IInventoryService, InventoryService>();
    builder.Services.AddScoped<IMedicineService, MedicineService>();

    // ── FluentValidation ─────────────────────────────────────────────────────
    builder.Services.AddScoped<IValidator<AuthRegisterRequest>, RegisterRequestValidator>();
    builder.Services.AddScoped<IValidator<AuthLoginRequest>, LoginRequestValidator>();
    builder.Services.AddScoped<IValidator<NearbyQuery>, NearbyQueryValidator>();
    builder.Services.AddScoped<IValidator<UpdateStockRequest>, UpdateStockRequestValidator>();
    builder.Services.AddScoped<IValidator<AddInventoryRequest>, AddInventoryRequestValidator>();

    // ── JWT Auth ─────────────────────────────────────────────────────────────
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Jwt:Key must be configured");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "medfind",
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"] ?? "medfind",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });
    builder.Services.AddAuthorization();

    // ── Health checks ─────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddNpgSql(connStr, name: "postgres")
        .AddRedis(redisConn, name: "redis");

    // ── Controllers + CORS ──────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

    // ── Build ────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Migrate + Seed ───────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MedFindDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db, logger);
    }

    // ── Middleware pipeline ──────────────────────────────────────────────────
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseSerilogRequestLogging(opt =>
    {
        opt.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0}ms";
        opt.EnrichDiagnosticContext = (diag, ctx) =>
        {
            diag.Set("RequestHost", ctx.Request.Host.Value);
        };
    });
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program partial for WebApplicationFactory in integration tests
public partial class Program { }
