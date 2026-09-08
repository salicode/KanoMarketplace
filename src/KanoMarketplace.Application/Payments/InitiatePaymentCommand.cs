
using KanoMarketplace.Application.Common.Interfaces;
using KanoMarketplace.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

 namespace KanoMarketplace.Application.PaymentProcessing;

public record InitiatePaymentCommand(Guid OrderId) : IRequest<string>; // returns a provider reference

public class InitiatePaymentHandler : IRequestHandler<InitiatePaymentCommand, string>
{
    private readonly IAppDbContext _db;
    public InitiatePaymentHandler(IAppDbContext db) => _db = db;

    public async Task<string> Handle(InitiatePaymentCommand cmd, CancellationToken ct)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == cmd.OrderId, ct)
            ?? throw new InvalidOperationException("Order not found.");

        var reference = $"KN-PAY-{Guid.NewGuid():N}"[..20];

        _db.Payments.Add(new Payment
        {
            OrderId = order.Id,
            Provider = "paystack",
            ProviderReference = reference,
            Amount = order.Total,
            Status = Domain.Enums.PaymentStatus.Initiated
        });
        await _db.SaveChangesAsync(ct);

        // Real integration: call Paystack's /transaction/initialize here with `reference`,
        // return the authorization_url it gives back for the client to redirect to.
        // Left as a reference-only stub since no provider credentials exist yet.
        return reference;
    }
}