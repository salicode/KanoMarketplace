
using KanoMarketplace.Application.Common.Interfaces;
using KanoMarketplace.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Riders;

public record GoOnlineOfflineCommand(Guid RiderId, bool IsOnline) : IRequest;

public class GoOnlineOfflineHandler : IRequestHandler<GoOnlineOfflineCommand>
{
    private readonly IAppDbContext _db;
    public GoOnlineOfflineHandler(IAppDbContext db) => _db = db;

    public async Task Handle(GoOnlineOfflineCommand cmd, CancellationToken ct)
    {
        var profile = await _db.RiderProfiles.FirstOrDefaultAsync(r => r.UserId == cmd.RiderId, ct);
        if (profile is null)
        {
            profile = new RiderProfile { UserId = cmd.RiderId, IsOnline = cmd.IsOnline };
            _db.RiderProfiles.Add(profile);
        }
        else
        {
            profile.IsOnline = cmd.IsOnline;
        }
        await _db.SaveChangesAsync(ct);
        // PRD §9 — "do not continuously track riders when offline": going offline stops future
        // UpdateLocationCommand calls from this rider being meaningful; nothing further to do here.
    }
}