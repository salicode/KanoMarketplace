// Domain/Entities/User.cs
using KanoMarketplace.Domain.Common;

namespace KanoMarketplace.Domain.Entities;

public enum UserRole { Customer, Merchant, Rider, CustomerSupport, OperationsManager, FinanceAdmin, SuperAdmin }

public class User : BaseEntity
{
    public required string PhoneNumber { get; init; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public UserRole Role { get; init; }
    public bool IsActive { get; set; } = true;
}