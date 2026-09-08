
using MediatR;

namespace KanoMarketplace.Application.Merchants;

public record RegisterMerchantCommand(
    Guid OwnerUserId,
    string BusinessName,
    string? OwnerContactName,
    string? OwnerContactPhone,
    Guid? CategoryId,
    string BankAccountNumber,
    string BankName,
    string BankAccountName) : IRequest<Guid>;