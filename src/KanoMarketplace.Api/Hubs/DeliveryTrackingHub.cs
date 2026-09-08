
using Microsoft.AspNetCore.SignalR;

namespace KanoMarketplace.Api.Hubs;

public class DeliveryTrackingHub : Hub
{
    // Customer's app calls this once it opens the order-tracking screen
    public async Task JoinOrderGroup(string orderId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(orderId));

    public async Task LeaveOrderGroup(string orderId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(orderId));

    public static string GroupName(string orderId) => $"order-{orderId}";
}