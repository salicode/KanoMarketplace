// Domain/Entities/FinancialLedger.cs — double-entry, per PRD §11
using KanoMarketplace.Domain.Common;

namespace KanoMarketplace.Domain.Entities;

public enum LedgerAccount { CustomerPayment, MerchantPayable, RiderPayable, PlatformRevenue, ProviderFees, Refunds }

public class LedgerEntry : BaseEntity
{
    public required Guid OrderId { get; init; }
    public required LedgerAccount Account { get; init; }
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public string? Description { get; init; }
}