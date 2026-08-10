namespace MiniWebsite.Application.Admin.FranchiseDistributors.Dtos;

public class FranchiseDistributorQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
}

public record FranchiseDistributorPageDto(
    IReadOnlyList<FranchiseDistributorRowDto> Users,
    IReadOnlyList<DealOptionLiteDto> MwDeals,
    IReadOnlyList<DealOptionLiteDto> FranchiseDeals,
    int TotalCount,
    int Page,
    int PageSize);

public record DealOptionLiteDto(int Id, string DealName, string? CouponCode, string PlanType);

public record FranchiseDistributorRowDto(
    int Id,
    string Email,
    string Name,
    string? Phone,
    DateTime CreatedAt,
    string ReferralSourceDisplay,
    string CompanyName,
    string CollaborationEnabled,
    string Influencer,
    string FrdStatusLabel,
    string CardPaymentStatus,
    string FrdFeeDisplay,
    int? FranchiseInvoiceId,
    int? JoiningDealInvoiceId,
    int WebsiteCount,
    decimal PendingReferralAmount,
    MappedDealLiteDto? MwDeal,
    MappedDealLiteDto? FranchiseDeal);

public record MappedDealLiteDto(int MappingId, int DealId, string DealName, string? CouponCode);

public record SetInfluencerRequest(string Email, string Status);
