using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/support")]
[Authorize]
public class SupportController : ControllerBase
{
    private readonly IMediator _mediator;
    public SupportController(IMediator mediator) => _mediator = mediator;

    [HttpPost("tickets")]
    public async Task<IActionResult> CreateTicket() => Ok();

    [HttpGet("tickets/{id:guid}")]
    public async Task<IActionResult> GetTicket(Guid id) => Ok();

    [HttpPost("tickets/{id:guid}/messages")]
    public async Task<IActionResult> AddMessage(Guid id) => Ok();
}