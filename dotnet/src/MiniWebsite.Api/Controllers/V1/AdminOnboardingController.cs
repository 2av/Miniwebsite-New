using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.Onboarding;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/admin/onboarding")]
[Authorize(Roles = ApiConstants.AdminRole)]
public class AdminOnboardingController : ControllerBase
{
    private readonly IDocumentReviewService _review;
    private readonly IFieldSalesAdminService _fieldSales;

    public AdminOnboardingController(IDocumentReviewService review, IFieldSalesAdminService fieldSales)
    {
        _review = review;
        _fieldSales = fieldSales;
    }

    [HttpPost("{userId:int}/review")]
    public async Task<ActionResult<ApiResult>> Review(
        int userId, [FromBody] ReviewDocumentsRequest request, CancellationToken ct)
    {
        var reviewer = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "";
        var result = await _review.ReviewAsync(userId, reviewer, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("field-sales/{userId:int}")]
    public async Task<ActionResult<ApiResult<FieldSalesSummaryDto>>> FieldSales(int userId, CancellationToken ct)
    {
        var result = await _fieldSales.SummaryAsync(userId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("field-sales/{userId:int}/deals")]
    public async Task<ActionResult<ApiResult>> AddDeal(
        int userId, [FromBody] AddFieldSalesDealRequest request, CancellationToken ct)
    {
        var result = await _fieldSales.AddDealAsync(userId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("field-sales/{userId:int}/documents")]
    public async Task<ActionResult<ApiResult>> Documents(
        int userId, [FromBody] FieldSalesDocumentsRequest request, CancellationToken ct)
    {
        var result = await _fieldSales.SaveDocumentsAsync(userId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
