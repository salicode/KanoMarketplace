
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;

namespace KanoMarketplace.Application.Carts;

public record RemoveCartItemCommand(Guid CartItemId) : IRequest;

public class RemoveCartItemHandler : IRequestHandler<RemoveCartItemCommand>
{
    private readonly IAppDbContext _db;
    public RemoveCartItemHandler(IAppDbContext db) => _db = db;

    public async Task Handle(RemoveCartItemCommand cmd, CancellationToken ct)
    {
        var item = await _db.CartItems.FindAsync([cmd.CartItemId], ct);
        if (item is null) return; // already gone — idempotent
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync(ct);
    }
}