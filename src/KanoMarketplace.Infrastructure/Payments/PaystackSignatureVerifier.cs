// Infrastructure/Payments/PaystackSignatureVerifier.cs
using KanoMarketplace.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace KanoMarketplace.Infrastructure.Payments;

public class PaystackSignatureVerifier : IPaymentSignatureVerifier
{
    private readonly string _secretKey;
    public PaystackSignatureVerifier(IConfiguration config) => _secretKey = config["Payments:PaystackSecretKey"]!;

    public bool Verify(string rawBody, string signatureHeader)
    {
        if (string.IsNullOrEmpty(signatureHeader)) return false;

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        var computed = Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signatureHeader.ToLowerInvariant()));
    }
}