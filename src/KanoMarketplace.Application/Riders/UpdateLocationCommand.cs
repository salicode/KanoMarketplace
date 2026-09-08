
using KanoMarketplace.Application.Common.Interfaces;
using KanoMarketplace.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace KanoMarketplace.Application.Riders;

public record UpdateLocationCommand(Guid RiderId, double Latitude, double Longitude) : IRequest;

public class UpdateLocationHandler : IRequestHandler<UpdateLocationCommand>
{
    private readonly IAppDbContext _db;
    private readonly IDeliveryTrackingNotifier _notifier;
    private static readonly GeometryFactory Gf = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public UpdateLocationHandler(IAppDbContext db, IDeliveryTrackingNotifier notifier)
    { _db = db; _notifier = notifier; }

    public async Task Handle(UpdateLocationCommand cmd, CancellationToken ct)
    {
        var point = Gf.CreatePoint(new Coordinate(cmd.Longitude, cmd.Latitude));

        var profile = await _db.RiderProfiles.FirstOrDefaultAsync(r => r.UserId == cmd.RiderId, ct)
            ?? throw new InvalidOperationException("Rider must go online before sending location updates.");

        profile.CurrentLocation = point;
        profile.LastLocationAt = DateTimeOffset.UtcNow;

        _db.RiderLocations.Add(new RiderLocation
        {
            RiderId = cmd.RiderId,
            Location = point,
            RecordedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        var activeDelivery = await _db.Deliveries
            .Where(d => d.RiderId == cmd.RiderId && d.DeliveredAt == null && d.PickedUpAt != null)
            .Select(d => d.OrderId)
            .FirstOrDefaultAsync(ct);

        if (activeDelivery != Guid.Empty)
            await _notifier.BroadcastRiderLocation(activeDelivery, cmd.Latitude, cmd.Longitude);
    }
}