namespace MiniWebsite.Application.Admin.WalletRecharge.Dtos;

public record FranchiseeWalletLookupDto(
    int UserId,
    string Email,
    string Name,
    string? Phone,
    decimal CurrentBalance,
    string CurrentBalanceDisplay);

public record WalletRechargeRequest(
    string UserEmail,
    decimal Amount,
    string? TxnMsg);

public record WalletRechargeResultDto(
    string UserEmail,
    string UserName,
    decimal AmountCredited,
    decimal NewBalance,
    string AmountDisplay,
    string NewBalanceDisplay,
    string Message);
