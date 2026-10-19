
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;

namespace KanoMarketplace.Application.Users;

public record UpdateUserProfileCommand(Guid UserId, string? FullName, string? Email) : IRequest;

public class UpdateUserProfileHandler : IRequestHandler<UpdateUserProfileCommand>
{
    private readonly IAppDbContext _db;
    public UpdateUserProfileHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateUserProfileCommand cmd, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync([cmd.UserId], ct)
            ?? throw new InvalidOperationException("User not found.");

        if (cmd.FullName is not null) user.FullName = cmd.FullName;
        if (cmd.Email is not null) user.Email = cmd.Email;

        await _db.SaveChangesAsync(ct);
    }
}