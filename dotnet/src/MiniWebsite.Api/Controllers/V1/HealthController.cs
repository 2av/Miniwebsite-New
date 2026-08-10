using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResult<object>> Get() =>
        Ok(ApiResult<object>.Ok(new
        {
            status = "Healthy",
            service = "MiniWebsite.Api",
            version = "v1",
            utc = DateTime.UtcNow
        }));
}
