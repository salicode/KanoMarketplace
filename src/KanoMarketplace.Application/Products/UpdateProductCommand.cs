
using KanoMarketplace.Application.Common.Interfaces;
using MediatR;

namespace KanoMarketplace.Application.Products;

public record UpdateProductCommand(Guid ProductId, string Name, string? Description, decimal BasePrice, bool IsActive)
    : IRequest;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IAppDbContext _db;
    public UpdateProductHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateProductCommand cmd, CancellationToken ct)
    {
        var product = await _db.Products.FindAsync([cmd.ProductId], ct)
            ?? throw new InvalidOperationException("Product not found.");

        product.Name = cmd.Name;
        product.Description = cmd.Description;
        product.BasePrice = cmd.BasePrice;
        product.IsActive = cmd.IsActive;

        await _db.SaveChangesAsync(ct);
    }
}