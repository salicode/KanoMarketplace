// Domain/Entities/Delivery.cs, RiderLocation.cs
using KanoMarketplace.Domain.Common;
using NetTopologySuite.Geometries;

namespace KanoMarketplace.Domain.Entities;

public class Delivery : BaseEntity
{
    public required Guid OrderId { get; init; }
    public Guid? RiderId { get; set; }
    public Point? PickupLocation { get; set; }   // PostGIS geography(Point)
    public Point? DropoffLocation { get; set; }
    public string? DropoffLandmark { get; set; }
    public string? DropoffInstructions { get; set; }
    public string? DeliveryOtp { get; set; }
    public DateTimeOffset? PickedUpAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
}

// High-write, low-retention table — see §15 partitioning note
public class RiderLocation : BaseEntity
{
    public required Guid RiderId { get; init; }
    public Point Location { get; init; } = default!;
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;
}