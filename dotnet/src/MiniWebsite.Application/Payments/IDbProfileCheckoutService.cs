using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Payments.Dtos;

namespace MiniWebsite.Application.Payments;

public interface IDbProfileCheckoutService
{
    Task<ApiResult<DbProfileCheckoutPageDto>> GetPageAsync(string email, int cardId, CancellationToken ct = default);

    Task<ApiResult<DbProfilePromoResultDto>> ApplyPromoAsync(DbProfilePromoRequest request, CancellationToken ct = default);

    Task<ApiResult<DbProfileCheckoutResultDto>> CheckoutAsync(DbProfileCheckoutRequest request, CancellationToken ct = default);

    Task<ApiResult> VerifyAsync(DbProfileVerifyRequest request, CancellationToken ct = default);
}
