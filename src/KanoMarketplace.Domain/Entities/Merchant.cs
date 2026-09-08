// Domain/Entities/Merchant.cs
using KanoMarketplace.Domain.Common;

namespace KanoMarketplace.Domain.Entities;

public enum MerchantStatus { PendingVerification, Approved, Suspended, Rejected }

public class Merchant : BaseEntity
{
    public required Guid OwnerUserId { get; init; }        // FK -> User
    public string BusinessName { get; set; } = default!;
    public string? OwnerContactName { get; set; }
    public string? OwnerContactPhone { get; set; }
    public Guid? CategoryId { get; set; }                  // primary category, e.g. Food, Fashion
    public string? LogoUrl { get; set; }
    public string BankAccountNumber { get; set; } = default!;
    public string BankName { get; set; } = default!;
    public string BankAccountName { get; set; } = default!;
    public MerchantStatus Status { get; set; } = MerchantStatus.PendingVerification;
    public bool IsTemporarilyClosed { get; set; }

    private readonly List<MerchantOperatingHours> _hours = new();
    public IReadOnlyCollection<MerchantOperatingHours> OperatingHours => _hours;
    public void SetHours(IEnumerable<MerchantOperatingHours> hours)
    {
        _hours.Clear();
        _hours.AddRange(hours);
    }
}

// One row per day-of-week, per PRD §10 "Opening/closing time, Days open"
public class MerchantOperatingHours : BaseEntity
{
    public required Guid MerchantId { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeSpan? OpenTime { get; init; }   // null = closed that day
    public TimeSpan? CloseTime { get; init; }
}