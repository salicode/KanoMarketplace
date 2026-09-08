// Application/Common/Interfaces/IOtpService.cs
namespace KanoMarketplace.Application.Common.Interfaces;

public interface IOtpService
{
    Task<string> GenerateAndStoreAsync(string phoneNumber, CancellationToken ct);
    Task<bool> VerifyAsync(string phoneNumber, string code, CancellationToken ct);
    Task<bool> IsLockedOutAsync(string phoneNumber, CancellationToken ct);
    Task RegisterFailedAttemptAsync(string phoneNumber, CancellationToken ct);
}

public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken ct);
}

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string role);
    (string token, string hash) GenerateRefreshToken();
}