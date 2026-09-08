
using MediatR;

namespace KanoMarketplace.Application.Orders;

public record PlaceOrderItemDto(Guid ProductVariantId, int Quantity);

public record PlaceOrderCommand(
    Guid CustomerId,
    Guid MerchantId,
    Guid DeliveryAddressId,
    string IdempotencyKey,
    List<PlaceOrderItemDto> Items) : IRequest<Guid>;