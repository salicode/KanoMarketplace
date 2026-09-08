using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/refunds")]
[Authorize]
public class RefundsController : ControllerBase
{
    private readonly IMediator _mediator;
    public RefundsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Request() => Ok();

    [HttpPost("{id:guid}/approve"), Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Approve(Guid id) => Ok();
}