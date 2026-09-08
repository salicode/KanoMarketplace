using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CategoriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List() => Ok();

    [HttpPost, Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Create() => Ok();

    [HttpPut("{id:guid}"), Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> Update(Guid id) => Ok();
}