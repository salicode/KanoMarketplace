
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Users;

public record UserProfileDto(Guid Id, string PhoneNumber, string? Email, string? FullName, string Role);

public record GetCurrentUserQuery(Guid UserId) : IRequest<UserProfileDto>;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, UserProfileDto>
{
    private readonly IAppDbContext _db;
    public GetCurrentUserHandler(IAppDbContext db) => _db = db;

    public async Task<UserProfileDto> Handle(GetCurrentUserQuery q, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == q.UserId, ct)
            ?? throw new InvalidOperationException("User not found.");

        return new UserProfileDto(user.Id, user.PhoneNumber, user.Email, user.FullName, user.Role.ToString());
    }
}