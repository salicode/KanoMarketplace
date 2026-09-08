
using Asp.Versioning;
using KanoMarketplace.Application.Products;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace KanoMarketplace.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProductsController(IMediator mediator) => _mediator = mediator;

    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _mediator.Send(new SearchProductsQuery(q, categoryId, page, pageSize), ct));

    [HttpGet("{id:guid}"), AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try { return Ok(await _mediator.Send(new GetProductByIdQuery(id), ct)); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost, Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> Create(CreateProductCommand cmd, CancellationToken ct)
    {
        try
        {
            var id = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetById), new { id }, new { productId = id });
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}"), Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> Update(Guid id, UpdateProductCommand cmd, CancellationToken ct)
    {
        if (id != cmd.ProductId) return BadRequest(new { error = "Route id and body id must match." });
        try { await _mediator.Send(cmd, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/variants"), Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> AddVariant(Guid id, AddVariantCommand cmd, CancellationToken ct)
    {
        if (id != cmd.ProductId) return BadRequest(new { error = "Route id and body id must match." });
        try
        {
            var variantId = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(GetById), new { id }, new { variantId });
        }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}/inventory"), Authorize(Policy = "MerchantOnly")]
    public async Task<IActionResult> UpdateStock(Guid id, UpdateStockCommand cmd, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(cmd, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { error = "Stock was changed concurrently — retry." }); }
    }
}