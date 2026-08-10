using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Website.Dtos;

namespace MiniWebsite.Application.Website;

public interface IWebsiteService
{
    Task<ApiResult<BusinessNameDto>> GetBusinessNameAsync(int cardId, string ownerEmail, CancellationToken ct = default);
    Task<ApiResult<BusinessNameDto>> CreateBusinessNameAsync(UpsertBusinessNameRequest request, CancellationToken ct = default);
    Task<ApiResult<BusinessNameDto>> UpdateBusinessNameAsync(int cardId, UpsertBusinessNameRequest request, CancellationToken ct = default);
    Task<ApiResult<CheckNameResponse>> CheckNameAsync(string compName, int? excludeId, CancellationToken ct = default);

    Task<ApiResult<ThemeDto>> GetThemeAsync(int cardId, string ownerEmail, CancellationToken ct = default);
    Task<ApiResult<ThemeDto>> UpdateThemeAsync(int cardId, string ownerEmail, UpdateThemeRequest request, CancellationToken ct = default);
    Task<ApiResult<IReadOnlyList<ThemeCatalogItemDto>>> ListThemesAsync(CancellationToken ct = default);

    Task<ApiResult<CompanyDetailsDto>> GetCompanyDetailsAsync(int cardId, string ownerEmail, CancellationToken ct = default);
    Task<ApiResult<CompanyDetailsDto>> UpdateCompanyDetailsAsync(int cardId, string ownerEmail, UpdateCompanyDetailsRequest request, CancellationToken ct = default);

    Task<ApiResult<SocialLinksDto>> GetSocialAsync(int cardId, string ownerEmail, CancellationToken ct = default);
    Task<ApiResult<SocialLinksDto>> UpdateSocialAsync(int cardId, string ownerEmail, UpdateSocialLinksRequest request, CancellationToken ct = default);

    Task<ApiResult<PaymentDetailsDto>> GetPaymentAsync(int cardId, string ownerEmail, CancellationToken ct = default);
    Task<ApiResult<PaymentDetailsDto>> UpdatePaymentAsync(int cardId, string ownerEmail, UpdatePaymentDetailsRequest request, CancellationToken ct = default);

    Task<ApiResult<VideosDto>> GetVideosAsync(int cardId, string ownerEmail, CancellationToken ct = default);
    Task<ApiResult<VideosDto>> UpdateVideosAsync(int cardId, string ownerEmail, UpdateVideosRequest request, CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<ProductDto>>> ListProductsAsync(int cardId, string ownerEmail, CancellationToken ct = default);
    Task<ApiResult<ProductDto>> CreateProductAsync(int cardId, string ownerEmail, UpsertProductRequest request, CancellationToken ct = default);
    Task<ApiResult<ProductDto>> UpdateProductAsync(int cardId, string ownerEmail, int productId, UpsertProductRequest request, CancellationToken ct = default);
    Task<ApiResult> DeleteProductAsync(int cardId, string ownerEmail, int productId, CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<ServiceItemDto>>> ListServicesAsync(int cardId, string ownerEmail, CancellationToken ct = default);
    Task<ApiResult<ServiceItemDto>> CreateServiceAsync(int cardId, string ownerEmail, UpsertServiceRequest request, CancellationToken ct = default);
    Task<ApiResult<ServiceItemDto>> UpdateServiceAsync(int cardId, string ownerEmail, int serviceId, UpsertServiceRequest request, CancellationToken ct = default);
    Task<ApiResult> DeleteServiceAsync(int cardId, string ownerEmail, int serviceId, CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<GalleryItemDto>>> ListGalleryAsync(int cardId, string ownerEmail, CancellationToken ct = default);
    Task<ApiResult<GalleryItemDto>> CreateGalleryAsync(int cardId, string ownerEmail, UpsertGalleryRequest request, CancellationToken ct = default);
    Task<ApiResult> DeleteGalleryAsync(int cardId, string ownerEmail, int imageId, CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<SpecialOfferDto>>> ListOffersAsync(int cardId, string ownerEmail, CancellationToken ct = default);
    Task<ApiResult<SpecialOfferDto>> CreateOfferAsync(int cardId, string ownerEmail, UpsertSpecialOfferRequest request, CancellationToken ct = default);
    Task<ApiResult<SpecialOfferDto>> UpdateOfferAsync(int cardId, string ownerEmail, int offerId, UpsertSpecialOfferRequest request, CancellationToken ct = default);
    Task<ApiResult> DeleteOfferAsync(int cardId, string ownerEmail, int offerId, CancellationToken ct = default);
}
