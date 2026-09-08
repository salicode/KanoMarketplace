// Infrastructure/Auth/OtpService.cs
using KanoMarketplace.Application.Common.Interfaces;
using StackExchange.Redis;
using System.Security.Cryptography;

namespace KanoMarketplace.Infrastructure.Auth;

public class OtpService : IOtpService
{
    private readonly IConnectionMultiplexer _redis;
    private const int MaxAttempts = 5;
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockoutTtl = TimeSpan.FromMinutes(15);

    public OtpService(IConnectionMultiplexer redis) => _redis = redis;

    private IDatabase Db => _redis.GetDatabase();
    private static string OtpKey(string phone) => $"otp:{phone}";
    private static string AttemptsKey(string phone) => $"otp:attempts:{phone}";

    public async Task<string> GenerateAndStoreAsync(string phoneNumber, CancellationToken ct)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        await Db.StringSetAsync(OtpKey(phoneNumber), code, OtpTtl);
        return code;
    }

    public async Task<bool> VerifyAsync(string phoneNumber, string code, CancellationToken ct)
    {
        var stored = await Db.StringGetAsync(OtpKey(phoneNumber));
        if (!stored.HasValue || stored != code) return false;
        await Db.KeyDeleteAsync(OtpKey(phoneNumber));
        await Db.KeyDeleteAsync(AttemptsKey(phoneNumber));
        return true;
    }

    public async Task<bool> IsLockedOutAsync(string phoneNumber, CancellationToken ct)
    {
        var attempts = await Db.StringGetAsync(AttemptsKey(phoneNumber));
        return attempts.HasValue && (int)attempts >= MaxAttempts;
    }

    public async Task RegisterFailedAttemptAsync(string phoneNumber, CancellationToken ct)
    {
        var key = AttemptsKey(phoneNumber);
        var count = await Db.StringIncrementAsync(key);
        if (count == 1) await Db.KeyExpireAsync(key, LockoutTtl);
    }
}