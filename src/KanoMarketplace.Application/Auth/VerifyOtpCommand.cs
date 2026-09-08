// Application/Auth/VerifyOtpCommand.cs
using KanoMarketplace.Application.Common.Interfaces;
using KanoMarketplace.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace KanoMarketplace.Application.Auth;

public record VerifyOtpCommand(string PhoneNumber, string Code, string DeviceId)
    : IRequest<(string AccessToken, string RefreshToken)>;

public class VerifyOtpHandler : IRequestHandler<VerifyOtpCommand, (string, string)>
{
    private readonly IOtpService _otp;
    private readonly ITokenService _tokens;
    private readonly IAppDbContext _db;

    public VerifyOtpHandler(IOtpService otp, ITokenService tokens, IAppDbContext db)
    { _otp = otp; _tokens = tokens; _db = db; }

    public async Task<(string, string)> Handle(VerifyOtpCommand cmd, CancellationToken ct)
    {
        if (await _otp.IsLockedOutAsync(cmd.PhoneNumber, ct))
            throw new InvalidOperationException("Too many attempts. Try again later.");

        var ok = await _otp.VerifyAsync(cmd.PhoneNumber, cmd.Code, ct);
        if (!ok)
        {
            await _otp.RegisterFailedAttemptAsync(cmd.PhoneNumber, ct);
            throw new InvalidOperationException("Invalid or expired code.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == cmd.PhoneNumber, ct);
        if (user is null)
        {
            user = new User { PhoneNumber = cmd.PhoneNumber, Role = Domain.Entities.UserRole.Customer };
            _db.Users.Add(user);
        }

        var access = _tokens.GenerateAccessToken(user.Id, user.Role.ToString());
        var (refreshPlain, refreshHash) = _tokens.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            DeviceId = cmd.DeviceId,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });

        await _db.SaveChangesAsync(ct);
        return (access, refreshPlain);
    }
}