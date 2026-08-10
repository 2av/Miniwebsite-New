using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.AllOrders;
using MiniWebsite.Application.Admin.AllOrders.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/all-orders")]
[AllowAnonymous]
public class AdminAllOrdersController : ControllerBase
{
    private readonly IAdminAllOrdersService _service;

    public AdminAllOrdersController(IAdminAllOrdersService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<AllOrdersPageDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(new AllOrdersQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        }, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
