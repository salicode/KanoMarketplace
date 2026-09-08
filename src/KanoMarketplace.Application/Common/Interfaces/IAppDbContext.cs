
using KanoMarketplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderStatusHistory> OrderStatusHistories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<Inventory> Inventories { get; }
    DbSet<Delivery> Deliveries { get; }
    DbSet<RiderLocation> RiderLocations { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Merchant> Merchants { get; }
    DbSet<MerchantOperatingHours> MerchantOperatingHours { get; }
    DbSet<Category> Categories { get; }
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<RiderProfile> RiderProfiles { get; }

   DbSet<Payment> Payments { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}