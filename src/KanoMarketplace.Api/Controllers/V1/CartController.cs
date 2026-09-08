// Api/Controllers/V1/CartController.cs
using Asp.Versioning;
using KanoMarketplace.Application.Carts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;
    private Guid RiderId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    public CartController(IMediator mediator) => _mediator = mediator;

    // TODO: once claims-based identity is wired through auth middleware end to end, pull CustomerId
    // from User.FindFirst(ClaimTypes.NameIdentifier) instead of trusting the caller — same flag as
    // CreateProductCommand earlier. Left explicit here so the handlers are easy to test meanwhile.
    private Guid CustomerId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken ct)
        => Ok(await _mediator.Send(new GetCartQuery(CustomerId), ct));

    public record AddItemBody(Guid ProductVariantId, int Quantity);

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddItemBody body, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new AddCartItemCommand(CustomerId, body.ProductVariantId, body.Quantity), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    public record UpdateItemBody(int Quantity);

    [HttpPut("items/{itemId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid itemId, UpdateItemBody body, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UpdateCartItemCommand(CustomerId, itemId, body.Quantity), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid itemId, CancellationToken ct)
    {
        await _mediator.Send(new RemoveCartItemCommand(itemId), ct);
        return NoContent();
    }

    public record CheckoutBody(Guid DeliveryAddressId, string IdempotencyKey);

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CheckoutBody body, CancellationToken ct)
    {
        try
        {
            var orderId = await _mediator.Send(
                new CheckoutCommand(CustomerId, body.DeliveryAddressId, body.IdempotencyKey), ct);
            return Ok(new { orderId });
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }
}