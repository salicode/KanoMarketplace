
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Products;

public record UpdateStockCommand(Guid ProductVariantId, int NewStockQuantity, bool IsAvailable) : IRequest;

public class UpdateStockHandler : IRequestHandler<UpdateStockCommand>
{
     private readonly IAppDbContext _db;
    public UpdateStockHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateStockCommand cmd, CancellationToken ct)
    {
        var inventory = await _db.Inventories
            .FirstOrDefaultAsync(i => i.ProductVariantId == cmd.ProductVariantId, ct)
            ?? throw new InvalidOperationException("Inventory record not found.");

        inventory.StockQuantity = cmd.NewStockQuantity;
        inventory.IsAvailable = cmd.IsAvailable;

        await _db.SaveChangesAsync(ct);
        // Note: this can race with a concurrent Order decrement — that's exactly what RowVersion
        // on Inventory protects against. A conflict here surfaces as DbUpdateConcurrencyException;
        // let it bubble to a 409 same as PlaceOrderHandler does.
    }
}