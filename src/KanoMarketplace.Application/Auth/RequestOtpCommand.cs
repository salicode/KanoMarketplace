
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KanoMarketplace.Application.Auth;

public record RequestOtpCommand(string? PhoneNumber, string? Email = null) : IRequest;

public class RequestOtpHandler : IRequestHandler<RequestOtpCommand>
{
    private readonly IOtpService _otp;
    private readonly ISmsSender _sms;
    private readonly IOtpEmailSender _email;
    private readonly IAppDbContext _db;

    public RequestOtpHandler(IOtpService otp, ISmsSender sms, IOtpEmailSender email, IAppDbContext db)
    { _otp = otp; _sms = sms; _email = email; _db = db; }

    public async Task Handle(RequestOtpCommand cmd, CancellationToken ct)
    {
        var phone = cmd.PhoneNumber?.Trim();
        var email = cmd.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(email))
            throw new InvalidOperationException("Provide a phone number or an email.");

        // Find existing user by phone, or by email (login with email only)
        var user = !string.IsNullOrEmpty(phone)
            ? await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, ct)
            : await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (string.IsNullOrEmpty(phone))
        {
            if (user is null)
                throw new InvalidOperationException("No account with that email. Register with your phone number first.");
            phone = user.PhoneNumber;
        }

        // Send to the email given now, otherwise the one saved on the account
        email = string.IsNullOrEmpty(email) ? user?.Email?.ToLowerInvariant() : email;

        if (await _otp.IsLockedOutAsync(phone, ct))
            throw new InvalidOperationException("Too many attempts. Try again later.");

        var code = await _otp.GenerateAndStoreAsync(phone, ct);
        var msg = $"Your Kano Marketplace code is {code}. Expires in 5 minutes.";

        await _sms.SendAsync(phone, msg, ct);
        if (!string.IsNullOrEmpty(email))
            await _email.SendAsync(email, "Your Kano Marketplace login code", msg, ct);
    }
}