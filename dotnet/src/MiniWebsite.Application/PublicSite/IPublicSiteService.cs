using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.PublicSite.Dtos;

namespace MiniWebsite.Application.PublicSite;

public interface IPublicSiteService
{
    Task<ApiResult<IReadOnlyList<PublicFaqItemDto>>> GetFaqsAsync(string pageType, CancellationToken ct = default);
    Task<ApiResult<PublicContentDto>> GetContentAsync(string contentType, CancellationToken ct = default);
    CaptchaChallengeDto CreateCaptcha();
    Task<ApiResult> SubmitFranchiseEnquiryAsync(FranchiseEnquiryRequest request, CancellationToken ct = default);
}
