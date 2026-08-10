using MiniWebsite.Application.Admin.ManageCards.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.ManageCards;

public interface IAdminManageCardsService
{
    Task<ApiResult<ManageCardsPageDto>> ListAsync(ManageCardsQuery query, CancellationToken ct = default);
    Task<ApiResult> SetComplimentaryAsync(int cardId, string status, CancellationToken ct = default);
}
