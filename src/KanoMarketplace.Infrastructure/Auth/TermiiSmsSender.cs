// Infrastructure/Auth/TermiiSmsSender.cs — placeholder, wire up real provider credentials when you pick one
using KanoMarketplace.Application.Common.Interfaces;

namespace KanoMarketplace.Infrastructure.Auth;

public class TermiiSmsSender : ISmsSender
{
    public Task SendAsync(string phoneNumber, string message, CancellationToken ct)
    {
        // TODO: call actual SMS provider HTTP API here
        Console.WriteLine($"[SMS to {phoneNumber}]: {message}");
        return Task.CompletedTask;
    }
}