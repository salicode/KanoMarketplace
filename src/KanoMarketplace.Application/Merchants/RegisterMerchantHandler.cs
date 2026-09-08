// Application/Merchants/RegisterMerchantHandler.cs
using KanoMarketplace.Domain.Entities;
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Merchants;

public class RegisterMerchantHandler : IRequestHandler<RegisterMerchantCommand, Guid>
{
     private readonly IAppDbContext _db;
    public RegisterMerchantHandler(IAppDbContext db) => _db = db;

    public async Task<Guid> Handle(RegisterMerchantCommand cmd, CancellationToken ct)
    {
        var alreadyExists = await _db.Merchants.AnyAsync(m => m.OwnerUserId == cmd.OwnerUserId, ct);
        if (alreadyExists)
            throw new InvalidOperationException("This account already has a merchant profile.");

        var merchant = new Merchant
        {
            OwnerUserId = cmd.OwnerUserId,
            BusinessName = cmd.BusinessName,
            OwnerContactName = cmd.OwnerContactName,
            OwnerContactPhone = cmd.OwnerContactPhone,
            CategoryId = cmd.CategoryId,
            BankAccountNumber = cmd.BankAccountNumber,
            BankName = cmd.BankName,
            BankAccountName = cmd.BankAccountName,
            Status = Domain.Entities.MerchantStatus.PendingVerification
        };

        _db.Merchants.Add(merchant);
        await _db.SaveChangesAsync(ct);
        return merchant.Id;
        // Verification documents/logo upload: handled as a separate multipart endpoint against
        // object storage (see §27 in the first guide) — not part of this command.
    }
}