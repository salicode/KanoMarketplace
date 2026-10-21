namespace KanoMarketplace.Application.Common.Interfaces;

public interface IOtpEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct);
}
