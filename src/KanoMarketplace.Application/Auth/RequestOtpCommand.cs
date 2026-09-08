// Application/Auth/RequestOtpCommand.cs
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;

namespace KanoMarketplace.Application.Auth;

public record RequestOtpCommand(string PhoneNumber) : IRequest;

public class RequestOtpHandler : IRequestHandler<RequestOtpCommand>
{
    private readonly IOtpService _otp;
    private readonly ISmsSender _sms;
    public RequestOtpHandler(IOtpService otp, ISmsSender sms) { _otp = otp; _sms = sms; }

    public async Task Handle(RequestOtpCommand cmd, CancellationToken ct)
    {
        if (await _otp.IsLockedOutAsync(cmd.PhoneNumber, ct))
            throw new InvalidOperationException("Too many attempts. Try again later.");

        var code = await _otp.GenerateAndStoreAsync(cmd.PhoneNumber, ct);
        await _sms.SendAsync(cmd.PhoneNumber, $"Your Kano Marketplace code is {code}. Expires in 5 minutes.", ct);
    }
}