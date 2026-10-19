// Api/Controllers/V1/AdminController.cs — thin wrapper; most logic is queries over existing entities
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Policy = "SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() => Ok(); 

    [HttpGet("operations-map")]
    public async Task<IActionResult> OperationsMap() => Ok();

    [HttpPut("merchants/{id:guid}/approve")]
    public async Task<IActionResult> ApproveMerchant(Guid id) => Ok();

    [HttpPut("merchants/{id:guid}/suspend")]
    public async Task<IActionResult> SuspendMerchant(Guid id) => Ok();

    [HttpPut("riders/{id:guid}/approve")]
    public async Task<IActionResult> ApproveRider(Guid id) => Ok();

    [HttpGet("audit-logs")]
    public async Task<IActionResult> AuditLogs() => Ok();
}