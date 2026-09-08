
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;

namespace KanoMarketplace.Application.Merchants;

public record ApproveMerchantCommand(Guid MerchantId) : IRequest;
public record SuspendMerchantCommand(Guid MerchantId, string Reason) : IRequest;

public class ApproveMerchantHandler : IRequestHandler<ApproveMerchantCommand>
{

    private readonly IAppDbContext _db;
   
    public ApproveMerchantHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ApproveMerchantCommand cmd, CancellationToken ct)
    {
        var m = await _db.Merchants.FindAsync([cmd.MerchantId], ct)
            ?? throw new InvalidOperationException("Merchant not found.");
        m.Status = Domain.Entities.MerchantStatus.Approved;
        await _db.SaveChangesAsync(ct);
    }
}

public class SuspendMerchantHandler : IRequestHandler<SuspendMerchantCommand>
{
    private readonly IAppDbContext _db;
    public SuspendMerchantHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SuspendMerchantCommand cmd, CancellationToken ct)
    {
        var m = await _db.Merchants.FindAsync([cmd.MerchantId], ct)
            ?? throw new InvalidOperationException("Merchant not found.");
        m.Status = Domain.Entities.MerchantStatus.Suspended;
        await _db.SaveChangesAsync(ct);
        // Audit log entry (§28) belongs here too — write once AuditLog entity exists.
    }
}