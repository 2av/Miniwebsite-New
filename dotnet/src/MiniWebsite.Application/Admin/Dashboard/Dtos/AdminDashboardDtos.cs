namespace MiniWebsite.Application.Admin.Dashboard.Dtos;

public record AdminDashboardDto(
    string AsOf,
    PeriodStatsDto Today,
    PeriodStatsDto Month,
    decimal CommissionEarnedThisMonth,
    decimal PayoutOutstanding,
    DashboardQueueDto Queue,
    UserSplitDto Users,
    ProfileSplitDto Profiles,
    IReadOnlyList<PartnerCountDto> Partners,
    IReadOnlyList<RecentOrderDto> RecentOrders,
    IReadOnlyList<RecentProfileDto> RecentProfiles);

public record PeriodStatsDto(int Orders, decimal Collected, int NewProfiles);

public record DashboardQueueDto(
    int PartnerInvoices,
    int EligiblePayouts,
    decimal EligiblePayoutAmount,
    int DocumentsPending,
    int SuspendedProfiles,
    int ExpiringProfiles);

public record UserSplitDto(int Active, int InActive);

public record ProfileSplitDto(int Active, int Suspended, int Expired);

public record PartnerCountDto(string Slug, string Label, int Count);

public record RecentOrderDto(
    int InvoiceId,
    string OrderId,
    string User,
    decimal Amount,
    string PaymentStatus,
    string When);

public record RecentProfileDto(
    int CardId,
    string DbId,
    string Company,
    string Status,
    string Created);
