namespace MiniWebsite.Application.Common.Interfaces;

public interface IPaymentGateway
{
    Task<CreatePaymentOrderResult> CreateOrderAsync(decimal amountInRupees, string receipt, CancellationToken cancellationToken = default);
    Task<bool> VerifyPaymentAsync(string orderId, string paymentId, string signature, CancellationToken cancellationToken = default);
}

public record CreatePaymentOrderResult(string OrderId, decimal AmountInRupees, string Currency, string KeyId);
