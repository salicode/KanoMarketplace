
using KanoMarketplace.Domain.Entities;
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Products;

public record AddVariantCommand(Guid ProductId, string Sku, decimal PriceOverride, string OptionsJson, int InitialStock)
    : IRequest<Guid>;

public class AddVariantHandler : IRequestHandler<AddVariantCommand, Guid>
{
    private readonly IAppDbContext _db;
    public AddVariantHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(AddVariantCommand cmd, CancellationToken ct)
    {
        var exists = await _db.Products.AnyAsync(p => p.Id == cmd.ProductId, ct);
        if (!exists) throw new InvalidOperationException("Product not found.");

        var variant = new ProductVariant
        {
            ProductId = cmd.ProductId,
            Sku = cmd.Sku,
            PriceOverride = cmd.PriceOverride,
            OptionsJson = cmd.OptionsJson
        };
        _db.ProductVariants.Add(variant);
        _db.Inventories.Add(new Inventory { ProductVariantId = variant.Id, StockQuantity = cmd.InitialStock });

        await _db.SaveChangesAsync(ct);
        return variant.Id;
    }
}