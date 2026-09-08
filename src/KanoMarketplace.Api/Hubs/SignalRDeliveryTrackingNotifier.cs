
using KanoMarketplace.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace KanoMarketplace.Api.Hubs;

public class SignalRDeliveryTrackingNotifier : IDeliveryTrackingNotifier
{
    private readonly IHubContext<DeliveryTrackingHub> _hub;
    public SignalRDeliveryTrackingNotifier(IHubContext<DeliveryTrackingHub> hub) => _hub = hub;

    public async Task BroadcastRiderLocation(Guid orderId, double latitude, double longitude)
    {
        await _hub.Clients.Group(DeliveryTrackingHub.GroupName(orderId.ToString()))
            .SendAsync("riderLocationUpdated", new { latitude, longitude, at = DateTimeOffset.UtcNow });
    }
}