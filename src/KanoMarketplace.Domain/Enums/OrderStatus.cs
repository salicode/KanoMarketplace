
namespace KanoMarketplace.Domain.Enums;

public enum OrderStatus
{
    Cart, Checkout, PaymentPending, Paid, OrderPlaced, MerchantAccepted,
    Preparing, ReadyForPickup, RiderAssigned, RiderArrivingPickup,
    PickedUp, InTransit, Arriving, Delivered,
    Cancelled, RefundPending, Refunded, Failed, Disputed
}

public enum PaymentStatus
{
    Initiated, Pending, Successful, Failed, Expired, Refunded, PartiallyRefunded
}