
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Carts;

public record CartItemDto(Guid Id, Guid ProductId, Guid ProductVariantId, int Quantity, decimal UnitPrice, decimal LineTotal);
public record CartDto(Guid Id, Guid? MerchantId, List<CartItemDto> Items, decimal Subtotal);

public record GetCartQuery(Guid CustomerId) : IRequest<CartDto>;

public class GetCartHandler : IRequestHandler<GetCartQuery, CartDto>
{
    private readonly IAppDbContext _db;
    public GetCartHandler(IAppDbContext db) => _db = db;

    public async Task<CartDto> Handle(GetCartQuery q, CancellationToken ct)
    {
        var cart = await _db.Carts.FirstOrDefaultAsync(c => c.CustomerId == q.CustomerId, ct);
        if (cart is null) return new CartDto(Guid.Empty, null, new(), 0);

        var items = await _db.CartItems.Where(i => i.CartId == cart.Id).ToListAsync(ct);
        var variantIds = items.Select(i => i.ProductVariantId).ToList();
        var variants = await _db.ProductVariants.Where(v => variantIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, ct);
        var productIds = variants.Values.Select(v => v.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var dtoItems = items.Select(i =>
        {
            var variant = variants[i.ProductVariantId];
            var product = products[variant.ProductId];
            var price = variant.PriceOverride > 0 ? variant.PriceOverride : product.BasePrice;
            return new CartItemDto(i.Id, i.ProductId, i.ProductVariantId, i.Quantity, price, price * i.Quantity);
        }).ToList();

        return new CartDto(cart.Id, cart.MerchantId, dtoItems, dtoItems.Sum(i => i.LineTotal));
    }
}