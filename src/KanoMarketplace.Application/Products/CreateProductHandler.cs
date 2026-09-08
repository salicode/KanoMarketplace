
using KanoMarketplace.Domain.Entities;
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Products;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid>
{
   private readonly IAppDbContext _db;
    public CreateProductHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        var merchant = await _db.Merchants.FindAsync([cmd.MerchantId], ct)
            ?? throw new InvalidOperationException("Merchant not found.");
        if (merchant.Status != MerchantStatus.Approved)
            throw new InvalidOperationException("Merchant is not approved to list products.");

        if (!cmd.Variants.Any())
            throw new InvalidOperationException("A product needs at least one variant (even a default one).");

        var product = new Product
        {
            MerchantId = cmd.MerchantId,
            CategoryId = cmd.CategoryId,
            Name = cmd.Name,
            Description = cmd.Description,
            BasePrice = cmd.BasePrice
        };

        foreach (var v in cmd.Variants)
        {
            var variant = new ProductVariant
            {
                ProductId = product.Id,
                Sku = v.Sku,
                PriceOverride = v.PriceOverride,
                OptionsJson = v.OptionsJson
            };
            product.AddVariant(variant);

            _db.Inventories.Add(new Inventory
            {
                ProductVariantId = variant.Id,
                StockQuantity = v.InitialStock
            });
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return product.Id;
    }
}