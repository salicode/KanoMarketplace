// Domain/Entities/RefreshToken.cs
using KanoMarketplace.Domain.Common;

namespace KanoMarketplace.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public required Guid UserId { get; init; }
    public required string TokenHash { get; init; }
    public required string DeviceId { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public bool Revoked { get; set; }
}