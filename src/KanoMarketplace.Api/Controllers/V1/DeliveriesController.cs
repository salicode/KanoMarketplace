using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/deliveries")]
[Authorize]
public class DeliveriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public DeliveriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{orderId:guid}/track")]
    public async Task<IActionResult> Track(Guid orderId) => Ok();

   [HttpPost("{orderId:guid}/assign-rider"), Authorize(Policy = "SuperAdmin")]
public async Task<IActionResult> AssignRider(Guid orderId, CancellationToken ct)
{
    try
    {
        var riderId = await _mediator.Send(new KanoMarketplace.Application.Riders.AssignRiderCommand(orderId), ct);
        return Ok(new { riderId });
    }
    catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
}
    public async Task<IActionResult> AssignRider(Guid orderId) => Ok();

    [HttpPost("{orderId:guid}/confirm-pickup"), Authorize(Policy = "RiderOnly")]
    public async Task<IActionResult> ConfirmPickup(Guid orderId) => Ok();

    [HttpPost("{orderId:guid}/confirm-delivery"), Authorize(Policy = "RiderOnly")]
    public async Task<IActionResult> ConfirmDelivery(Guid orderId, [FromBody] string otp) => Ok();
}