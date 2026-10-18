namespace MiniWebsite.Application.Admin.AllOrders.Dtos;

public class AllOrdersQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
}

public record AllOrdersPageDto(
    IReadOnlyList<AllOrderRowDto> Orders,
    int TotalCount,
    int Page,
    int PageSize);

public record AllOrderRowDto(
    int InvoiceId,
    string OrderIdDisplay,
    string OrderDateDisplay,
    string UserIdDisplay,
    string UserEmail,
    int? CardId,
    string DbIdDisplay,
    string ReferralSource,
    string OrderDescription,
    string ListPriceDisplay,
    string PromoDiscountDisplay,
    string NetPriceDisplay,
    string CgstDisplay,
    string SgstDisplay,
    string IgstDisplay,
    decimal TotalAmount,
    string TotalAmountDisplay,
    string InvoiceNumber,
    string Comment);

public record UpdateOrderCommentRequest(string? Comment);
