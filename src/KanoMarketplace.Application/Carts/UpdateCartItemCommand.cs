
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;

namespace KanoMarketplace.Application.Carts;

public record UpdateCartItemCommand(Guid CustomerId, Guid CartItemId, int Quantity) : IRequest;

public class UpdateCartItemHandler : IRequestHandler<UpdateCartItemCommand>
{
    private readonly IAppDbContext _db;
    public UpdateCartItemHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateCartItemCommand cmd, CancellationToken ct)
    {
        var item = await _db.CartItems.FindAsync([cmd.CartItemId], ct)
            ?? throw new InvalidOperationException("Cart item not found.");

        if (cmd.Quantity <= 0)
            _db.CartItems.Remove(item);
        else
            item.Quantity = cmd.Quantity;

        await _db.SaveChangesAsync(ct);
    }
}