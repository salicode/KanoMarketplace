
using KanoMarketplace.Application.Common.Interfaces;
using KanoMarketplace.Domain.Entities;
using KanoMarketplace.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

 namespace KanoMarketplace.Application.PaymentProcessing;

public record HandlePaymentWebhookCommand(string RawBody, string SignatureHeader) : IRequest;

public class HandlePaymentWebhookHandler : IRequestHandler<HandlePaymentWebhookCommand>
{
    private readonly IAppDbContext _db;
    private readonly IPaymentSignatureVerifier _verifier;
    public HandlePaymentWebhookHandler(IAppDbContext db, IPaymentSignatureVerifier verifier)
    { _db = db; _verifier = verifier; }

    public async Task Handle(HandlePaymentWebhookCommand cmd, CancellationToken ct)
    {
        if (!_verifier.Verify(cmd.RawBody, cmd.SignatureHeader))
            throw new UnauthorizedAccessException("Invalid webhook signature.");

        using var doc = JsonDocument.Parse(cmd.RawBody);
        var root = doc.RootElement;
        var eventType = root.GetProperty("event").GetString();
        if (eventType != "charge.success") return; // ignore everything else for now

        var reference = root.GetProperty("data").GetProperty("reference").GetString();
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.ProviderReference == reference, ct)
            ?? throw new InvalidOperationException($"No payment found for reference {reference}.");

        if (payment.Status == PaymentStatus.Successful) return; // webhook can be delivered more than once

        payment.Status = PaymentStatus.Successful;

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId, ct)
            ?? throw new InvalidOperationException("Order not found for this payment.");

        order.TransitionTo(Domain.Enums.OrderStatus.Paid, actor: "payment-webhook");
        order.TransitionTo(Domain.Enums.OrderStatus.OrderPlaced, actor: "payment-webhook");

        // Illustrative double-entry — get this reviewed against real commission/fee rules before going live.
        const decimal commissionRate = 0.15m; // 15%, placeholder — PRD §11 says this must be configurable
        var commission = Math.Round(order.ItemsSubtotal * commissionRate, 2);
        var merchantPayable = order.ItemsSubtotal - commission;

        _db.LedgerEntries.AddRange(
            new LedgerEntry { OrderId = order.Id, Account = LedgerAccount.CustomerPayment, Credit = order.Total, Debit = 0, Description = "Customer payment received" },
            new LedgerEntry { OrderId = order.Id, Account = LedgerAccount.MerchantPayable, Debit = merchantPayable, Credit = 0, Description = "Amount owed to merchant" },
            new LedgerEntry { OrderId = order.Id, Account = LedgerAccount.PlatformRevenue, Debit = commission, Credit = 0, Description = "Platform commission" }
        );

        await _db.SaveChangesAsync(ct);
    }
}