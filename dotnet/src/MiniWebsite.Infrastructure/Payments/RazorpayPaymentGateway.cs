using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Infrastructure.Options;

namespace MiniWebsite.Infrastructure.Payments;

/// <summary>
/// Creates a Razorpay order when key id and secret are set.
/// Empty keys keep the local bypass order id used by checkout.
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

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.KeyId) && !string.IsNullOrWhiteSpace(_options.KeySecret);

    public async Task<CreatePaymentOrderResult> CreateOrderAsync(
        decimal amountInRupees, string receipt, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            var stubOrderId = "order_scaffold_" + Guid.NewGuid().ToString("N")[..12];
            _logger.LogInformation("Razorpay keys are empty. Local order {OrderId} for {Receipt}.", stubOrderId, receipt);
            return new CreatePaymentOrderResult(stubOrderId, amountInRupees, "INR", _options.KeyId);
        }

        var paise = (int)Math.Round(amountInRupees * 100m, MidpointRounding.AwayFromZero);
        if (paise < 100) throw new InvalidOperationException("Razorpay requires an amount of at least ₹1.");

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders");
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(_options.KeyId + ":" + _options.KeySecret));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        req.Content = JsonContent.Create(new { amount = paise, currency = "INR", receipt });

        using var res = await http.SendAsync(req, cancellationToken);
        var body = await res.Content.ReadAsStringAsync(cancellationToken);
        if (!res.IsSuccessStatusCode)
        {
            _logger.LogWarning("Razorpay order failed ({Status}): {Body}", (int)res.StatusCode, body);
            throw new InvalidOperationException("Razorpay could not start this payment.");
        }

        using var doc = JsonDocument.Parse(body);
        var id = doc.RootElement.GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(id))
            throw new InvalidOperationException("Razorpay did not return an order id.");

        return new CreatePaymentOrderResult(id, amountInRupees, "INR", _options.KeyId);
    }

    public Task<bool> VerifyPaymentAsync(
        string orderId, string paymentId, string signature, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.KeySecret))
        {
            _logger.LogWarning("Razorpay KeySecret is empty. Payment verification was skipped.");
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
