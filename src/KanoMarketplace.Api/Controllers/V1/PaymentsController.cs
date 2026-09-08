
using Asp.Versioning;
using KanoMarketplace.Application.PaymentProcessing;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentsController(IMediator mediator) => _mediator = mediator;

    public record InitiateBody(Guid OrderId);

    [HttpPost("initiate"), Authorize]
    public async Task<IActionResult> Initiate(InitiateBody body, CancellationToken ct)
    {
        try
        {
            var reference = await _mediator.Send(new InitiatePaymentCommand(body.OrderId), ct);
            return Ok(new { reference });
        }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("webhook"), AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);
        Request.Body.Position = 0;

        var signature = Request.Headers["x-paystack-signature"].ToString();

        try
        {
            await _mediator.Send(new HandlePaymentWebhookCommand(rawBody, signature), ct);
            return Ok(); // provider just needs a 200 — don't leak internal details in the body
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (InvalidOperationException) { return Ok(); } // ack anyway so provider doesn't retry-storm on a bad reference
    }
}