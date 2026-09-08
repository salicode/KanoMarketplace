
using MediatR;

namespace KanoMarketplace.Application.Products;

public record CreateProductVariantDto(string Sku, decimal PriceOverride, string OptionsJson, int InitialStock);

public record CreateProductCommand(
    Guid MerchantId,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal BasePrice,
    List<CreateProductVariantDto> Variants) : IRequest<Guid>;