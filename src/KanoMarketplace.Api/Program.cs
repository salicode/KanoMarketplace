using Asp.Versioning;
using KanoMarketplace.Api.Hubs;
using KanoMarketplace.Application.Common.Interfaces;
using KanoMarketplace.Infrastructure.Auth;
using KanoMarketplace.Infrastructure.Payments;
using KanoMarketplace.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Text;
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .WriteTo.Console()
    .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .Enrich.FromLogContext());

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"),
        o => o.UseNetTopologySuite()));

builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(KanoMarketplace.Application.Orders.PlaceOrderCommand).Assembly));

builder.Services.AddStackExchangeRedisCache(opt =>
    opt.Configuration = builder.Configuration.GetConnectionString("Redis"));

builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ReportApiVersions = true;
})
.AddApiExplorer(o =>
{
    o.GroupNameFormat = "'v'VVV";
    o.SubstituteApiVersionInUrl = true;
});
builder.Services.AddScoped<IPaymentSignatureVerifier, PaystackSignatureVerifier>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("MerchantOnly", p => p.RequireRole("Merchant"));
    opt.AddPolicy("RiderOnly", p => p.RequireRole("Rider"));
    opt.AddPolicy("SuperAdmin", p => p.RequireRole("SuperAdmin"));
    opt.AddPolicy("SupportStaff", p => p.RequireRole("CustomerSupport", "SuperAdmin"));
    opt.AddPolicy("OperationsStaff", p => p.RequireRole("OperationsManager", "SuperAdmin"));
    opt.AddPolicy("FinanceStaff", p => p.RequireRole("FinanceAdmin", "SuperAdmin"));
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the access token only, without the word Bearer."
    });
    c.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});
builder.Services.AddHealthChecks().AddNpgSql(builder.Configuration.GetConnectionString("Default")!);

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ISmsSender, TermiiSmsSender>();
builder.Services.AddScoped<IDeliveryTrackingNotifier, SignalRDeliveryTrackingNotifier>();


var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();

app.UseExceptionHandler(a => a.Run(async ctx =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    ctx.Response.StatusCode = ex is InvalidOperationException ? 409 : 500;
    await ctx.Response.WriteAsJsonAsync(new { error = ex?.Message ?? "Unexpected error" });
}));

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<DeliveryTrackingHub>("/hubs/delivery");
app.MapHealthChecks("/health");

// Seed the first SuperAdmin from configuration (Admin:PhoneNumber)
try
{
    var adminPhone = app.Configuration["Admin:PhoneNumber"];
    if (!string.IsNullOrWhiteSpace(adminPhone))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""Users"" (""Id"", ""PhoneNumber"", ""Role"", ""IsActive"", ""CreatedAt"")
            SELECT gen_random_uuid(), {adminPhone}, 6, true, now()
            WHERE NOT EXISTS (SELECT 1 FROM ""Users"" WHERE ""PhoneNumber"" = {adminPhone})");
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Users"" SET ""Role"" = 6 WHERE ""PhoneNumber"" = {adminPhone}");
    }
}
catch (Exception ex)
{
    Log.Warning(ex, "Admin seeding skipped (database not ready or table missing)");
}

app.Run();
