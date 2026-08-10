using MiniWebsite.Application.Admin.AllOrders.Dtos;
using MiniWebsite.Application.Common.Models;

namespace MiniWebsite.Application.Admin.AllOrders;

public interface IAdminAllOrdersService
{
    Task<ApiResult<AllOrdersPageDto>> ListAsync(AllOrdersQuery query, CancellationToken ct = default);
}
