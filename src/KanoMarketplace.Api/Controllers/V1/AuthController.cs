// Api/Controllers/V1/AuthController.cs
using Asp.Versioning;
using KanoMarketplace.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    public record RequestOtpBody(string PhoneNumber);
    public record VerifyOtpBody(string PhoneNumber, string Code, string DeviceId);
    public record RefreshBody(string RefreshToken, string DeviceId);

    [HttpPost("otp/request")]
    public async Task<IActionResult> RequestOtp(RequestOtpBody body, CancellationToken ct)
    {
        await _mediator.Send(new RequestOtpCommand(body.PhoneNumber), ct);
        return Ok(new { message = "OTP sent." });
    }

    [HttpPost("otp/verify")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpBody body, CancellationToken ct)
    {
        var (access, refresh) = await _mediator.Send(
            new VerifyOtpCommand(body.PhoneNumber, body.Code, body.DeviceId), ct);
        return Ok(new { accessToken = access, refreshToken = refresh });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshBody body, CancellationToken ct)
    {
        var access = await _mediator.Send(new RefreshTokenCommand(body.RefreshToken, body.DeviceId), ct);
        return Ok(new { accessToken = access });
    }
}