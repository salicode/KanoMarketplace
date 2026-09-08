
using KanoMarketplace.Domain.Entities;
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;

namespace KanoMarketplace.Application.Merchants;

public record OperatingHoursDto(DayOfWeek DayOfWeek, TimeSpan? OpenTime, TimeSpan? CloseTime);

public record UpdateOperatingHoursCommand(Guid MerchantId, List<OperatingHoursDto> Hours) : IRequest;

public class UpdateOperatingHoursHandler : IRequestHandler<UpdateOperatingHoursCommand>
{
     private readonly IAppDbContext _db;
    public UpdateOperatingHoursHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateOperatingHoursCommand cmd, CancellationToken ct)
    {
        var merchant = await _db.Merchants.FindAsync([cmd.MerchantId], ct)
            ?? throw new InvalidOperationException("Merchant not found.");

        merchant.SetHours(cmd.Hours.Select(h => new MerchantOperatingHours
        {
            MerchantId = cmd.MerchantId,
            DayOfWeek = h.DayOfWeek,
            OpenTime = h.OpenTime,
            CloseTime = h.CloseTime
        }));

        await _db.SaveChangesAsync(ct);
    }
}