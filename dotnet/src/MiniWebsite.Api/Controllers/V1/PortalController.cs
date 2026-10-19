using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniWebsite.Application.Admin.Invoices;
using MiniWebsite.Application.Admin.KitManagement.Dtos;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Portal;
using MiniWebsite.Application.Portal.Dtos;
using MiniWebsite.Shared.Constants;

namespace MiniWebsite.Api.Controllers.V1;

/// <summary>Logged-in user portal APIs (email-scoped for now; JWT hardening later).</summary>
[ApiController]
[Route(ApiConstants.ApiRoutePrefix + "/portal")]
[AllowAnonymous]
public class PortalController : ControllerBase
{
    private readonly IPortalService _portal;

    public PortalController(IPortalService portal)
    {
        _portal = portal;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResult<PortalMeDto>>> Me([FromQuery] string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return BadRequest(ApiResult<PortalMeDto>.Fail("email is required."));
        var result = await _portal.GetMeAsync(email, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("referral")]
    public async Task<ActionResult<ApiResult<ReferralPageDto>>> Referral([FromQuery] string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return BadRequest(ApiResult<ReferralPageDto>.Fail("email is required."));
        var result = await _portal.GetReferralAsync(email, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("bank-details")]
    public async Task<ActionResult<ApiResult>> SaveBank([FromBody] SaveBankDetailsRequest request, CancellationToken ct)
    {
        var result = await _portal.SaveBankDetailsAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<InvoiceListItemDto>>>> Invoices(
        [FromQuery] string email, [FromQuery] int cardId, CancellationToken ct)
    {
        var result = await _portal.ListInvoicesForCardAsync(email, cardId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("invoices/{id:int}")]
    public async Task<ActionResult<ApiResult<InvoiceDetailDto>>> Invoice(
        int id, [FromQuery] string email, CancellationToken ct)
    {
        var result = await _portal.GetInvoiceAsync(email, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("invoices/{id:int}/html")]
    public async Task<ActionResult> InvoiceHtml(int id, [FromQuery] string email, CancellationToken ct)
    {
        var result = await _portal.GetInvoiceHtmlAsync(email, id, ct);
        if (!result.Success || result.Data == null) return BadRequest(result);
        return Content(result.Data.Html, "text/html");
    }

    [HttpGet("franchise-invoice/html")]
    public async Task<ActionResult> FranchiseInvoiceHtml([FromQuery] string email, CancellationToken ct)
    {
        var result = await _portal.GetFranchiseInvoiceHtmlAsync(email, ct);
        if (!result.Success || result.Data == null) return BadRequest(result);
        return Content(result.Data.Html, "text/html");
    }

    [HttpGet("kit")]
    public async Task<ActionResult<ApiResult<KitExplorerDto>>> Kit(
        [FromQuery] string email,
        [FromQuery] string category = "marketing",
        [FromQuery] int folderId = 0,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(ApiResult<KitExplorerDto>.Fail("email is required."));
        var result = await _portal.GetKitExplorerAsync(email, category, folderId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("verification")]
    public async Task<ActionResult<ApiResult<VerificationStatusDto>>> Verification(
        [FromQuery] string email, CancellationToken ct)
    {
        var result = await _portal.GetVerificationAsync(email, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("verification")]
    [RequestSizeLimit(8_000_000)]
    public async Task<ActionResult<ApiResult<VerificationStatusDto>>> SubmitVerification(
        [FromBody] SubmitVerificationRequest request, CancellationToken ct)
    {
        var result = await _portal.SubmitVerificationAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("id-card")]
    public async Task<ActionResult<ApiResult<IdCardProfileDto>>> IdCard(
        [FromQuery] string email, CancellationToken ct)
    {
        var result = await _portal.GetIdCardAsync(email, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Upload portal avatar — same folder as PHP <c>common/upload_profile.php</c>.</summary>
    [HttpPost("profile-image")]
    [RequestSizeLimit(2_500_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 2_500_000)]
    public async Task<ActionResult<ApiResult<ProfileImageUploadDto>>> UploadProfileImage(
        [FromForm] string email,
        IFormFile? profile_image,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(ApiResult<ProfileImageUploadDto>.Fail("email is required."));
        if (profile_image == null || profile_image.Length <= 0)
            return BadRequest(ApiResult<ProfileImageUploadDto>.Fail("No image file selected. Please choose an image to upload."));

        await using var stream = profile_image.OpenReadStream();
        var result = await _portal.UploadProfileImageAsync(
            email,
            stream,
            profile_image.FileName,
            profile_image.ContentType ?? "",
            profile_image.Length,
            ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("customers")]
    public async Task<ActionResult<ApiResult<CustomerTrackerPageDto>>> Customers(
        [FromQuery] string email, [FromQuery] string? search, CancellationToken ct)
    {
        var result = await _portal.ListCustomersAsync(email, search, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("customers")]
    public async Task<ActionResult<ApiResult<CustomerManagerRowDto>>> SaveCustomer(
        [FromBody] SaveCustomerRequest request, CancellationToken ct)
    {
        var result = await _portal.SaveCustomerAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("customers/{id:int}")]
    public async Task<ActionResult<ApiResult>> DeleteCustomer(
        int id, [FromQuery] string email, CancellationToken ct)
    {
        var result = await _portal.DeleteCustomerAsync(email, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("customers/{id:int}/followups")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<CustomerFollowupRowDto>>>> Followups(
        int id, [FromQuery] string email, CancellationToken ct)
    {
        var result = await _portal.ListFollowupsAsync(email, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("customers/followups")]
    public async Task<ActionResult<ApiResult>> AddFollowup(
        [FromBody] AddFollowupRequest request, CancellationToken ct)
    {
        var result = await _portal.AddFollowupAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("customers/{id:int}/last-shared")]
    public async Task<ActionResult<ApiResult>> RecordLastShared(
        int id, [FromQuery] string email, CancellationToken ct)
    {
        var result = await _portal.RecordLastSharedAsync(email, id, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
