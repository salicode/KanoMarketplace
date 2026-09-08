
using KanoMarketplace.Domain.Common;

namespace KanoMarketplace.Domain.Entities;

public class Product : BaseEntity
{
    public required Guid MerchantId { get; init; }
    public required Guid CategoryId { get; init; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;

    private readonly List<ProductVariant> _variants = new();
    public IReadOnlyCollection<ProductVariant> Variants => _variants;
    public void AddVariant(ProductVariant v) => _variants.Add(v);
}

// Flexible attribute system per PRD §4.4 — one variant table handles size, colour, weight, add-ons
public class ProductVariant : BaseEntity
{
    public required Guid ProductId { get; init; }
    public string Sku { get; init; } = default!;
    public decimal PriceOverride { get; set; } // 0 = use Product.BasePrice
    public string OptionsJson { get; set; } = "{}"; // {"size":"L","colour":"Red"} — jsonb column
}