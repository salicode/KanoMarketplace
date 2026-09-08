// Application/Auth/RefreshTokenCommand.cs
using KanoMarketplace.Application.Common.Interfaces;

using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using KanoMarketplace.Application.Common.Interfaces;
namespace KanoMarketplace.Application.Auth;

public record RefreshTokenCommand(string RefreshToken, string DeviceId) : IRequest<string>;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, string>
{
       private readonly IAppDbContext _db;
    private readonly ITokenService _tokens;
    public RefreshTokenHandler(IAppDbContext db, ITokenService tokens) { _db = db; _tokens = tokens; }

    public async Task<string> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cmd.RefreshToken)));
        var stored = await _db.RefreshTokens.Include(r => r.UserId)
            .FirstOrDefaultAsync(r => r.TokenHash == hash && r.DeviceId == cmd.DeviceId, ct);

        if (stored is null || stored.Revoked || stored.ExpiresAt < DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Refresh token invalid or expired.");

        var user = await _db.Users.FindAsync([stored.UserId], ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        return _tokens.GenerateAccessToken(user.Id, user.Role.ToString());
    }
}