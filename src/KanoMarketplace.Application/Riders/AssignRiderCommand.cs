
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Riders;

public record AssignRiderCommand(Guid OrderId) : IRequest<Guid>; // returns assigned RiderId

public class AssignRiderHandler : IRequestHandler<AssignRiderCommand, Guid>
{
    private readonly IAppDbContext _db;
    public AssignRiderHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(AssignRiderCommand cmd, CancellationToken ct)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == cmd.OrderId, ct)
            ?? throw new InvalidOperationException("Order not found.");
        var delivery = await _db.Deliveries.FirstOrDefaultAsync(d => d.OrderId == cmd.OrderId, ct)
            ?? throw new InvalidOperationException("No delivery record exists for this order yet.");
        if (delivery.PickupLocation is null)
            throw new InvalidOperationException(
                "Pickup location isn't set for this delivery — merchant location isn't modeled yet (see build notes).");

        // Nearest-suitable-rider — PRD §7: start simple, evolve toward route optimization later.
        // ST_DWithin/ST_Distance run server-side in Postgres via PostGIS; EF Core translates these
        // NetTopologySuite calls directly into the SQL, no raw string SQL needed.
        const double radiusMeters = 5000; // 5km — make this configurable per PRD's "admin-configurable" spirit later

        var nearestRiderId = await _db.RiderProfiles
            .Where(r => r.IsOnline && r.CurrentLocation != null)
            .Where(r => r.CurrentLocation!.IsWithinDistance(delivery.PickupLocation, radiusMeters))
            .OrderBy(r => r.CurrentLocation!.Distance(delivery.PickupLocation))
            .Select(r => r.UserId)
            .FirstOrDefaultAsync(ct);

        if (nearestRiderId == Guid.Empty)
            throw new InvalidOperationException("No available riders within range right now.");

        delivery.RiderId = nearestRiderId;
        order.TransitionTo(Domain.Enums.OrderStatus.RiderAssigned, actor: "assignment-engine");

        await _db.SaveChangesAsync(ct);
        return nearestRiderId;

        // Not built yet, intentionally: 30-second accept window + retry-to-next-candidate on timeout/reject
        // (PRD §7). That needs a background job (Hangfire is already installed) to re-check unaccepted
        // offers — a reasonable next slice once this synchronous "just assign nearest" version is proven out.
    }
}