
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Riders;

public record AcceptJobCommand(Guid RiderId, Guid OrderId) : IRequest;

public class AcceptJobHandler : IRequestHandler<AcceptJobCommand>
{
    private readonly IAppDbContext _db;
    public AcceptJobHandler(IAppDbContext db) => _db = db;

    public async Task Handle(AcceptJobCommand cmd, CancellationToken ct)
    {
        var delivery = await _db.Deliveries.FirstOrDefaultAsync(d => d.OrderId == cmd.OrderId, ct)
            ?? throw new InvalidOperationException("Delivery not found.");
        if (delivery.RiderId != cmd.RiderId)
            throw new InvalidOperationException("This job wasn't offered to you.");

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == cmd.OrderId, ct)
            ?? throw new InvalidOperationException("Order not found.");

        order.TransitionTo(Domain.Enums.OrderStatus.RiderArrivingPickup, actor: "rider");
        await _db.SaveChangesAsync(ct);
    }
}