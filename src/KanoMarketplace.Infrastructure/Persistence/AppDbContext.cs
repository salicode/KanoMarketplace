// Infrastructure/Persistence/AppDbContext.cs
using KanoMarketplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using KanoMarketplace.Application.Common.Interfaces;

namespace KanoMarketplace.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<RiderLocation> RiderLocations => Set<RiderLocation>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<MerchantOperatingHours> MerchantOperatingHours => Set<MerchantOperatingHours>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<RiderProfile> RiderProfiles => Set<RiderProfile>();

    public DbSet<Payment> Payments => Set<Payment>();


    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("postgis");

        b.Entity<Order>(e =>
        {
            e.HasIndex(o => o.IdempotencyKey).IsUnique(); // PRD §26 duplicate-order protection
            e.HasIndex(o => new { o.CustomerId, o.Status });
            e.Property(o => o.RowVersion).IsRowVersion();
            e.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
            e.HasMany(o => o.History).WithOne().HasForeignKey(h => h.OrderId);
        });

        b.Entity<Inventory>(e =>
        {
            e.Property(i => i.RowVersion).IsRowVersion(); // optimistic concurrency = no overselling
            e.HasIndex(i => i.ProductVariantId).IsUnique();
        });

        b.Entity<ProductVariant>(e =>
        {
            e.Property(v => v.OptionsJson).HasColumnType("jsonb");
        });

        b.Entity<RiderLocation>(e =>
        {
            e.HasIndex(r => new { r.RiderId, r.RecordedAt });
            e.Property(r => r.Location).HasColumnType("geography (point)");
        });

        b.Entity<Delivery>(e =>
        {
            e.Property(d => d.PickupLocation).HasColumnType("geography (point)");
            e.Property(d => d.DropoffLocation).HasColumnType("geography (point)");
        });

        b.Entity<Merchant>(e =>
        {
            e.HasIndex(m => m.OwnerUserId);
            e.HasMany(m => m.OperatingHours).WithOne().HasForeignKey(h => h.MerchantId);
        });

        b.Entity<Product>(e =>
        {
            e.HasIndex(p => new { p.MerchantId, p.IsActive });
            e.HasIndex(p => p.CategoryId);
            e.HasMany(p => p.Variants).WithOne().HasForeignKey(v => v.ProductId);
        });

        b.Entity<Cart>(e =>
        {
            e.HasIndex(c => c.CustomerId).IsUnique(); // one active cart per customer
            e.HasMany(c => c.Items).WithOne().HasForeignKey(i => i.CartId);
        });

        b.Entity<CartItem>(e =>
        {
            e.HasIndex(i => i.CartId);
        });

        b.Entity<RiderProfile>(e =>
    {
        e.HasIndex(r => r.UserId).IsUnique();
        e.Property(r => r.CurrentLocation).HasColumnType("geography (point)");
    });

        base.OnModelCreating(b);
    }



    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
        return base.SaveChangesAsync(ct);
    }




}