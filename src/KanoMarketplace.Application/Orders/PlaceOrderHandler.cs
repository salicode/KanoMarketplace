using KanoMarketplace.Application.Common.Interfaces;
using KanoMarketplace.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Orders;

public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Guid>
{
    private readonly IAppDbContext _db;
    public PlaceOrderHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(PlaceOrderCommand cmd, CancellationToken ct)
    {
        var existing = await _db.Orders.FirstOrDefaultAsync(o => o.IdempotencyKey == cmd.IdempotencyKey, ct);
        if (existing is not null) return existing.Id;

        var order = new Order
        {
            CustomerId = cmd.CustomerId,
            MerchantId = cmd.MerchantId,
            DeliveryAddressId = cmd.DeliveryAddressId,
            IdempotencyKey = cmd.IdempotencyKey,
            OrderNumber = $"KN-{DateTime.UtcNow:yyyyMM}-{Random.Shared.Next(100000, 999999)}"
        };

        decimal subtotal = 0;
        foreach (var line in cmd.Items)
        {
            var inventory = await _db.Inventories
                .FirstOrDefaultAsync(i => i.ProductVariantId == line.ProductVariantId, ct)
                ?? throw new InvalidOperationException("Product variant not found.");

            inventory.Decrement(line.Quantity);

            var variant = await _db.ProductVariants.FindAsync([line.ProductVariantId], ct)
                ?? throw new InvalidOperationException("Variant not found.");
            var product = await _db.Products.FindAsync([variant.ProductId], ct)
                ?? throw new InvalidOperationException("Product not found.");
            var unitPrice = variant.PriceOverride > 0 ? variant.PriceOverride : product.BasePrice;

            order.AddItem(new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ProductVariantId = variant.Id,
                ProductNameSnapshot = product.Name,
                UnitPriceSnapshot = unitPrice,
                Quantity = line.Quantity,
                LineTotal = unitPrice * line.Quantity
            });
            subtotal += unitPrice * line.Quantity;
        }

        order.ItemsSubtotal = subtotal;
        order.Total = subtotal;
        order.TransitionTo(Domain.Enums.OrderStatus.Checkout, actor: "customer");

        _db.Orders.Add(order);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Item went out of stock while placing your order.");
        }

        return order.Id;
    }
}