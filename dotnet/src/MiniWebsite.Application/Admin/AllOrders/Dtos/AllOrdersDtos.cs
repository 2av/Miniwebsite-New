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
    string UserIdDisplay,
    string? MwIdDisplay,
    string PaymentStatusLabel,
    string PaymentStatusTone,
    string? PaidOnDisplay,
    decimal TotalAmount,
    string TotalAmountDisplay);
