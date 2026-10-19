using KanoMarketplace.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace KanoMarketplace.Infrastructure.Auth;

public class TermiiSmsSender : ISmsSender
{
    private readonly ILogger<TermiiSmsSender> _logger;

    public TermiiSmsSender(ILogger<TermiiSmsSender> logger) => _logger = logger;

    public Task SendAsync(string phoneNumber, string message, CancellationToken ct)
    {
        // TODO: call actual SMS provider HTTP API here
        _logger.LogWarning("[SMS to {Phone}]: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}