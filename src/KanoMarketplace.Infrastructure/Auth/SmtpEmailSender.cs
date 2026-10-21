using System.Net;
using System.Net.Mail;
using KanoMarketplace.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KanoMarketplace.Infrastructure.Auth;


public class SmtpEmailSender : IOtpEmailSender
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<SmtpEmailSender> _log;

    public SmtpEmailSender(IConfiguration cfg, ILogger<SmtpEmailSender> log)
    {
        _cfg = cfg;
        _log = log;
    }

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct)
    {
        var host = _cfg["Email:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _log.LogWarning("[EMAIL to {Email}] {Subject}: {Body}", toEmail, subject, body);
            return;
        }

        var port = int.TryParse(_cfg["Email:Port"], out var p) ? p : 587;
        var user = _cfg["Email:User"];
        var pass = _cfg["Email:Password"];
        var from = _cfg["Email:From"] ?? user ?? "no-reply@kano.local";

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(user, pass)
            };
            using var msg = new MailMessage(from, toEmail, subject, body);
            await client.SendMailAsync(msg, ct);
        }
        catch (Exception ex)
        {
            // Never block login because email failed; SMS/log still carries the code.
            _log.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }
}