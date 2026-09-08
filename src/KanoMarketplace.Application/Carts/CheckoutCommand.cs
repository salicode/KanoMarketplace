
using KanoMarketplace.Application.Common.Interfaces;
using KanoMarketplace.Application.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Carts;

public record CheckoutCommand(Guid CustomerId, Guid DeliveryAddressId, string IdempotencyKey) : IRequest<Guid>;

public class CheckoutHandler : IRequestHandler<CheckoutCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly IMediator _mediator;
    public CheckoutHandler(IAppDbContext db, IMediator mediator) { _db = db; _mediator = mediator; }

    public async Task<Guid> Handle(CheckoutCommand cmd, CancellationToken ct)
    {
        var cart = await _db.Carts.FirstOrDefaultAsync(c => c.CustomerId == cmd.CustomerId, ct)
            ?? throw new InvalidOperationException("Cart is empty.");
        var items = await _db.CartItems.Where(i => i.CartId == cart.Id).ToListAsync(ct);
        if (!items.Any() || cart.MerchantId is null)
            throw new InvalidOperationException("Cart is empty.");

        var orderId = await _mediator.Send(new PlaceOrderCommand(
            cmd.CustomerId,
            cart.MerchantId.Value,
            cmd.DeliveryAddressId,
            cmd.IdempotencyKey,
            items.Select(i => new PlaceOrderItemDto(i.ProductVariantId, i.Quantity)).ToList()
        ), ct);

        // Clear the cart only after the order is successfully placed
        _db.CartItems.RemoveRange(items);
        cart.MerchantId = null;
        await _db.SaveChangesAsync(ct);

        return orderId;
    }
}