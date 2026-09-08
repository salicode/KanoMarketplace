
namespace KanoMarketplace.Application.Common.Interfaces;

public interface IPaymentSignatureVerifier
{
    bool Verify(string rawBody, string signatureHeader);
}