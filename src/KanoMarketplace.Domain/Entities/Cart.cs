
using KanoMarketplace.Domain.Common;

namespace KanoMarketplace.Domain.Entities;

public class Cart : BaseEntity
{
    public required Guid CustomerId { get; init; }
    public Guid? MerchantId { get; set; } // set on first item added; PRD §4.6 — one store per cart

    private readonly List<CartItem> _items = new();
    public IReadOnlyCollection<CartItem> Items => _items;
    public void AddItem(CartItem item) => _items.Add(item);
    public void RemoveItem(CartItem item) => _items.Remove(item);
}

public class CartItem : BaseEntity
{
    public required Guid CartId { get; init; }
    public required Guid ProductId { get; init; }
    public required Guid ProductVariantId { get; init; }
    public int Quantity { get; set; }
}