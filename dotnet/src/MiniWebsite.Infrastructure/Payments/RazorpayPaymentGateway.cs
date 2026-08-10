using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Infrastructure.Options;

namespace MiniWebsite.Infrastructure.Payments;

/// <summary>
/// Scaffold Razorpay gateway. CreateOrder is stubbed until SDK is added in Phase 3.
/// Verify uses HMAC SHA256 (same algorithm Razorpay uses).
/// </summary>
public class RazorpayPaymentGateway : IPaymentGateway
{
    private readonly RazorpayOptions _options;
    private readonly ILogger<RazorpayPaymentGateway> _logger;

    public RazorpayPaymentGateway(IOptions<RazorpayOptions> options, ILogger<RazorpayPaymentGateway> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<CreatePaymentOrderResult> CreateOrderAsync(decimal amountInRupees, string receipt, CancellationToken cancellationToken = default)
    {
        // Phase 3: call Razorpay Orders API. For scaffold we return a placeholder order id.
        var stubOrderId = "order_scaffold_" + Guid.NewGuid().ToString("N")[..12];
        _logger.LogInformation("Scaffold CreateOrder: receipt={Receipt}, amount={Amount}", receipt, amountInRupees);
        return Task.FromResult(new CreatePaymentOrderResult(stubOrderId, amountInRupees, "INR", _options.KeyId));
    }

    public Task<bool> VerifyPaymentAsync(string orderId, string paymentId, string signature, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.KeySecret))
        {
            _logger.LogWarning("Razorpay KeySecret empty — verification skipped (scaffold).");
            return Task.FromResult(false);
        }

        var payload = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.KeySecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        var actual = signature.Trim().ToLowerInvariant();
        return Task.FromResult(CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(actual)));
    }
}
