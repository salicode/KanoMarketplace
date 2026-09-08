
namespace KanoMarketplace.Application.Common.Interfaces;

public interface IDeliveryTrackingNotifier
{
    Task BroadcastRiderLocation(Guid orderId, double latitude, double longitude);
}