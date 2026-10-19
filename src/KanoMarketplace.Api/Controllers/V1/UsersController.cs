
using Asp.Versioning;
using KanoMarketplace.Application.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        try { return Ok(await _mediator.Send(new GetCurrentUserQuery(CurrentUserId), ct)); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    public record UpdateProfileBody(string? FullName, string? Email);

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateProfileBody body, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UpdateUserProfileCommand(CurrentUserId, body.FullName, body.Email), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }
}