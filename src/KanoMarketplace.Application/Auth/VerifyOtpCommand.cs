
using KanoMarketplace.Application.Common.Interfaces;
using KanoMarketplace.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Auth;

public record VerifyOtpCommand(string? PhoneNumber, string Code, string DeviceId, string? Email = null)
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
        var phone = cmd.PhoneNumber?.Trim();
        var email = cmd.Email?.Trim().ToLowerInvariant();

        User? user = null;
        if (string.IsNullOrEmpty(phone))
        {
            if (string.IsNullOrEmpty(email))
                throw new InvalidOperationException("Provide a phone number or an email.");
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct)
                   ?? throw new InvalidOperationException("Invalid or expired code.");
            phone = user.PhoneNumber;
        }

        if (await _otp.IsLockedOutAsync(phone, ct))
            throw new InvalidOperationException("Too many attempts. Try again later.");

        var ok = await _otp.VerifyAsync(phone, cmd.Code, ct);
        if (!ok)
        {
            await _otp.RegisterFailedAttemptAsync(phone, ct);
            throw new InvalidOperationException("Invalid or expired code.");
        }

        user ??= await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, ct);
        if (user is null)
        {
            user = new User { PhoneNumber = phone, Role = Domain.Entities.UserRole.Customer };
            _db.Users.Add(user);
        }

        if (!user.IsActive)
            throw new InvalidOperationException("This account is deactivated.");

        // Save the email the code was sent to (only if the account has none yet)
        if (!string.IsNullOrEmpty(email) && string.IsNullOrEmpty(user.Email))
            user.Email = email;

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