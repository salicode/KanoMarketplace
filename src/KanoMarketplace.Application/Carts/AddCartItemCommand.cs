
using KanoMarketplace.Application.Common.Interfaces;
using KanoMarketplace.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Carts;
public record AddCartItemCommand(Guid CustomerId, Guid ProductVariantId, int Quantity) : IRequest;

public class AddCartItemHandler : IRequestHandler<AddCartItemCommand>
{
    private readonly IAppDbContext _db;
    public AddCartItemHandler(IAppDbContext db) => _db = db;

    public async Task Handle(AddCartItemCommand cmd, CancellationToken ct)
    {
        var variant = await _db.ProductVariants.FindAsync([cmd.ProductVariantId], ct)
            ?? throw new InvalidOperationException("Product variant not found.");
        var product = await _db.Products.FindAsync([variant.ProductId], ct)
            ?? throw new InvalidOperationException("Product not found.");

        var cart = await _db.Carts.FirstOrDefaultAsync(c => c.CustomerId == cmd.CustomerId, ct);
        if (cart is null)
        {
            cart = new Domain.Entities.Cart { CustomerId = cmd.CustomerId, MerchantId = product.MerchantId };
            _db.Carts.Add(cart);
        }
        else if (cart.MerchantId is not null && cart.MerchantId != product.MerchantId)
        {
            // PRD §4.6 — MVP is one store per cart
            throw new InvalidOperationException(
                "Your cart already has items from a different store. Clear your cart before ordering from a new store.");
        }
        else if (cart.MerchantId is null)
        {
            cart.MerchantId = product.MerchantId;
        }

        var existing = await _db.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductVariantId == cmd.ProductVariantId, ct);

        if (existing is not null)
        {
            existing.Quantity += cmd.Quantity;
        }
        else
        {
            _db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                ProductVariantId = cmd.ProductVariantId,
                Quantity = cmd.Quantity
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}