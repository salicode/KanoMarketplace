
using Asp.Versioning;
using KanoMarketplace.Application.Merchants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/merchants")]
public class MerchantsController : ControllerBase
{
    private readonly IMediator _mediator;
    public MerchantsController(IMediator mediator) => _mediator = mediator;

    [HttpPost, Authorize]
    public async Task<IActionResult> Register(RegisterMerchantCommand cmd, CancellationToken ct)
    {
        try
        {
            var id = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetProfile), new { id }, new { merchantId = id });
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProfile(Guid id, CancellationToken ct)
    {
        try { return Ok(await _mediator.Send(new GetMerchantProfileQuery(id), ct)); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}/hours"), Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> UpdateHours(Guid id, List<OperatingHoursDto> hours, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UpdateOperatingHoursCommand(id, hours), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}/approve"), Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new ApproveMerchantCommand(id), ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/suspend"), Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] string reason, CancellationToken ct)
    {
        await _mediator.Send(new SuspendMerchantCommand(id, reason), ct);
        return NoContent();
    }
}