// Api/Controllers/V1/OrdersController.cs
using Asp.Versioning;
using KanoMarketplace.Application.Orders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    public OrdersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(PlaceOrderCommand cmd, CancellationToken ct)
    {
        try
        {
            var orderId = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(PlaceOrder), new { id = orderId }, new { orderId });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}