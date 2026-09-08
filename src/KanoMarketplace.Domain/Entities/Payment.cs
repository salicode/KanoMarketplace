
using KanoMarketplace.Domain.Common;
using KanoMarketplace.Domain.Enums;

namespace KanoMarketplace.Domain.Entities;

public class Payment : BaseEntity
{
    public required Guid OrderId { get; init; }
    public required string Provider { get; init; } // e.g. "paystack"
    public string? ProviderReference { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;
}