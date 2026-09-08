
using KanoMarketplace.Domain.Common;
using NetTopologySuite.Geometries;

namespace KanoMarketplace.Domain.Entities;

public class RiderProfile : BaseEntity
{
    public required Guid UserId { get; init; }
    public bool IsOnline { get; set; }
    public string? VehicleType { get; set; }
    public Point? CurrentLocation { get; set; }
    public DateTimeOffset? LastLocationAt { get; set; }
}