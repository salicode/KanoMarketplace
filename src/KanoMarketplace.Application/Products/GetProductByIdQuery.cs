
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Products;

public record ProductVariantDto(Guid Id, string Sku, decimal Price, string OptionsJson, int StockQuantity, bool IsAvailable);
public record ProductDetailDto(Guid Id, string Name, string? Description, decimal BasePrice, List<ProductVariantDto> Variants);

public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDetailDto>;

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDetailDto>
{
     private readonly IAppDbContext _db;
    public GetProductByIdHandler(IAppDbContext db) => _db = db;

    public async Task<ProductDetailDto> Handle(GetProductByIdQuery q, CancellationToken ct)
    {
        var product = await _db.Products.Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == q.ProductId, ct)
            ?? throw new InvalidOperationException("Product not found.");

        var variantIds = product.Variants.Select(v => v.Id).ToList();
        var inventories = await _db.Inventories
            .Where(i => variantIds.Contains(i.ProductVariantId))
            .ToDictionaryAsync(i => i.ProductVariantId, ct);

        var variantDtos = product.Variants.Select(v =>
        {
            var inv = inventories.GetValueOrDefault(v.Id);
            var price = v.PriceOverride > 0 ? v.PriceOverride : product.BasePrice;
            return new ProductVariantDto(v.Id, v.Sku, price, v.OptionsJson,
                inv?.StockQuantity ?? 0, inv?.IsAvailable ?? false);
        }).ToList();

        return new ProductDetailDto(product.Id, product.Name, product.Description, product.BasePrice, variantDtos);
    }
}