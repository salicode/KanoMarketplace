
using KanoMarketplace.Domain.Common;
using KanoMarketplace.Domain.Enums;

namespace KanoMarketplace.Domain.Entities;

public class Order : BaseEntity
{
    public required Guid CustomerId { get; init; }
    public required Guid MerchantId { get; init; }
    public string OrderNumber { get; init; } = default!; // human-readable, e.g. KN-2026-000123
    public OrderStatus Status { get; private set; } = OrderStatus.Cart;
    public decimal ItemsSubtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal Total { get; set; }
    public string IdempotencyKey { get; init; } = default!; // PRD §26 — prevents duplicate orders
    public Guid? DeliveryAddressId { get; set; }
    public string? CancellationReason { get; set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items;

    private readonly List<OrderStatusHistory> _history = new();
    public IReadOnlyCollection<OrderStatusHistory> History => _history;

    // Allowed transitions — the actual state machine from PRD §6
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.Cart] = new[] { OrderStatus.Checkout },
        [OrderStatus.Checkout] = new[] { OrderStatus.PaymentPending, OrderStatus.Cancelled },
        [OrderStatus.PaymentPending] = new[] { OrderStatus.Paid, OrderStatus.Failed },
        [OrderStatus.Paid] = new[] { OrderStatus.OrderPlaced },
        [OrderStatus.OrderPlaced] = new[] { OrderStatus.MerchantAccepted, OrderStatus.Cancelled },
        [OrderStatus.MerchantAccepted] = new[] { OrderStatus.Preparing, OrderStatus.Cancelled },
        [OrderStatus.Preparing] = new[] { OrderStatus.ReadyForPickup },
        [OrderStatus.ReadyForPickup] = new[] { OrderStatus.RiderAssigned },
        [OrderStatus.RiderAssigned] = new[] { OrderStatus.RiderArrivingPickup, OrderStatus.Cancelled },
        [OrderStatus.RiderArrivingPickup] = new[] { OrderStatus.PickedUp },
        [OrderStatus.PickedUp] = new[] { OrderStatus.InTransit },
        [OrderStatus.InTransit] = new[] { OrderStatus.Arriving },
        [OrderStatus.Arriving] = new[] { OrderStatus.Delivered },
        [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = new[] { OrderStatus.RefundPending },
        [OrderStatus.RefundPending] = new[] { OrderStatus.Refunded },
    };

    public void TransitionTo(OrderStatus next, string? actor = null, string? reason = null)
    {
        if (!Transitions.TryGetValue(Status, out var allowed) || !allowed.Contains(next))
            throw new InvalidOperationException($"Cannot move order from {Status} to {next}.");

        _history.Add(new OrderStatusHistory
        {
            OrderId = Id,
            FromStatus = Status,
            ToStatus = next,
            ChangedBy = actor,
            Reason = reason
        });
        Status = next;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddItem(OrderItem item) => _items.Add(item);
}

public class OrderItem : BaseEntity
{
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public Guid? ProductVariantId { get; init; }
    public string ProductNameSnapshot { get; init; } = default!; // don't join back to Product for history
    public decimal UnitPriceSnapshot { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}

public class OrderStatusHistory : BaseEntity
{
    public required Guid OrderId { get; init; }
    public OrderStatus FromStatus { get; init; }
    public OrderStatus ToStatus { get; init; }
    public string? ChangedBy { get; init; }
    public string? Reason { get; init; }
}