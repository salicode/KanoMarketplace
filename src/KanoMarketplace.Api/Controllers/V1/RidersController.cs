
using Asp.Versioning;
using KanoMarketplace.Application.Riders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/riders")]
[Authorize(Policy = "RiderOnly")]
public class RidersController : ControllerBase
{
    private readonly IMediator _mediator;
    public RidersController(IMediator mediator) => _mediator = mediator;

    private Guid RiderId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Register() => Ok();

    [HttpPut("status")]
    public async Task<IActionResult> GoOnlineOffline([FromBody] bool isOnline, CancellationToken ct)
    {
        await _mediator.Send(new GoOnlineOfflineCommand(RiderId, isOnline), ct);
        return NoContent();
    }

    public record LocationBody(double Latitude, double Longitude);

    [HttpPost("location")]
    public async Task<IActionResult> UpdateLocation([FromBody] LocationBody body, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UpdateLocationCommand(RiderId, body.Latitude, body.Longitude), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpGet("earnings")]
    public async Task<IActionResult> Earnings() => Ok();

    [HttpGet("jobs/available")]
    public async Task<IActionResult> AvailableJobs() => Ok();

    [HttpPost("jobs/{orderId:guid}/accept")]
    public async Task<IActionResult> AcceptJob(Guid orderId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new AcceptJobCommand(RiderId, orderId), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }
}