
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Merchants;

public record MerchantProfileDto(
    Guid Id, string BusinessName, string Status, bool IsTemporarilyClosed,
    Guid? CategoryId, List<OperatingHoursDto> Hours);

public record GetMerchantProfileQuery(Guid MerchantId) : IRequest<MerchantProfileDto>;

public class GetMerchantProfileHandler : IRequestHandler<GetMerchantProfileQuery, MerchantProfileDto>
{
  
    private readonly IAppDbContext _db;
    public GetMerchantProfileHandler(IAppDbContext db) => _db = db;

    public async Task<MerchantProfileDto> Handle(GetMerchantProfileQuery q, CancellationToken ct)
    {
        var m = await _db.Merchants.Include(x => x.OperatingHours)
            .FirstOrDefaultAsync(x => x.Id == q.MerchantId, ct)
            ?? throw new InvalidOperationException("Merchant not found.");

        return new MerchantProfileDto(
            m.Id, m.BusinessName, m.Status.ToString(), m.IsTemporarilyClosed, m.CategoryId,
            m.OperatingHours.Select(h => new OperatingHoursDto(h.DayOfWeek, h.OpenTime, h.CloseTime)).ToList());
    }
}