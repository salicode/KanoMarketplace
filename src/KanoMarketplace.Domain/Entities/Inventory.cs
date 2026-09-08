//  — this is where overselling gets prevented
using KanoMarketplace.Domain.Common;

namespace KanoMarketplace.Domain.Entities;

public class Inventory : BaseEntity
{
    public required Guid ProductVariantId { get; init; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsAvailable { get; set; } = true;

    public void Decrement(int quantity)
    {
        if (StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock."); // caught + surfaced as 409 in API
        StockQuantity -= quantity;
    }
}