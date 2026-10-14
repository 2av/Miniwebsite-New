using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.WalletRecharge.Dtos;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Admin.WalletRecharge;

public class AdminWalletRechargeService : IAdminWalletRechargeService
{
    private const decimal MinAmount = 10m;
    private const decimal MaxAmount = 1000m;

    private readonly IApplicationDbContext _db;

    public AdminWalletRechargeService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult<FranchiseeWalletLookupDto>> LookupAsync(
        string? email,
        int? userId,
        CancellationToken ct = default)
    {
        var user = await FindFranchiseeAsync(email, userId, ct);
        if (user == null)
            return ApiResult<FranchiseeWalletLookupDto>.Fail("Sorry! User Not available");

        var balance = await GetLatestBalanceAsync(user.Id, ct);
        return ApiResult<FranchiseeWalletLookupDto>.Ok(new FranchiseeWalletLookupDto(
            user.Id,
            user.Email,
            user.Name,
            user.Phone,
            balance,
            FormatMoney(balance)));
    }

    public async Task<ApiResult<WalletRechargeResultDto>> RechargeAsync(
        WalletRechargeRequest request,
        CancellationToken ct = default)
    {
        var email = Normalize(request.UserEmail);
        if (string.IsNullOrWhiteSpace(email))
            return ApiResult<WalletRechargeResultDto>.Fail("Franchisee email is required");

        if (request.Amount < MinAmount || request.Amount > MaxAmount)
            return ApiResult<WalletRechargeResultDto>.Fail($"Amount must be between ₹{MinAmount:0} and ₹{MaxAmount:0}");

        var user = await FindFranchiseeAsync(email, null, ct);
        if (user == null)
            return ApiResult<WalletRechargeResultDto>.Fail("Sorry! User Not available");

        var current = await GetLatestBalanceAsync(user.Id, ct);
        var amount = Math.Round(request.Amount, 2);
        var newBalance = current + amount;
        var txnMsg = string.IsNullOrWhiteSpace(request.TxnMsg) ? "Promotional Amount" : request.TxnMsg.Trim();
        var now = AppClock.Now;

        try
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == user.Id, ct);
            if (wallet == null)
            {
                wallet = new Wallet
                {
                    UserId = user.Id,
                    UserEmail = user.Email,
                    Balance = 0,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                _db.Wallets.Add(wallet);
                await _db.SaveChangesAsync(ct);
            }

            wallet.Balance = newBalance;
            wallet.UpdatedAt = now;

            _db.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = wallet.Id,
                UserId = user.Id,
                Type = "deposit",
                Amount = amount,
                BalanceAfter = newBalance,
                OrderId = "Promotional",
                Message = txnMsg,
                CreatedAt = now,
            });

            await _db.SaveChangesAsync(ct);

            return ApiResult<WalletRechargeResultDto>.Ok(
                new WalletRechargeResultDto(
                    user.Email,
                    user.Name,
                    amount,
                    newBalance,
                    FormatMoney(amount),
                    FormatMoney(newBalance),
                    $"Amount of {FormatMoney(amount)} credited in user's account."),
                "Wallet recharged successfully");
        }
        catch (Exception ex)
        {
            return ApiResult<WalletRechargeResultDto>.Fail(
                $"Failed! Amount of {FormatMoney(amount)} could not be added. " + ex.Message);
        }
    }

    private async Task<FranchiseeUser?> FindFranchiseeAsync(string? email, int? userId, CancellationToken ct)
    {
        var q = _db.Users.AsNoTracking().WhereRole(UserRole.Franchisee);

        if (userId is > 0)
            q = q.Where(u => u.Id == userId.Value);
        else if (!string.IsNullOrWhiteSpace(email))
        {
            var e = Normalize(email);
            q = q.Where(u => u.Email.ToLower() == e);
        }
        else
            return null;

        var row = await q.Select(u => new { u.Id, u.Email, Name = (u.FirstName + " " + u.LastName).Trim(), u.Phone }).FirstOrDefaultAsync(ct);
        return row == null ? null : new FranchiseeUser(row.Id, row.Email, row.Name, row.Phone);
    }

    private async Task<decimal> GetLatestBalanceAsync(int userId, CancellationToken ct)
    {
        try
        {
            var wallet = await _db.Wallets.AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId, ct);
            return wallet?.Balance ?? 0m;
        }
        catch
        {
            return 0m;
        }
    }

    private static string Normalize(string? value) => (value ?? "").Trim().ToLowerInvariant();

    private static string FormatMoney(decimal v) =>
        "₹" + v.ToString("N2", CultureInfo.InvariantCulture);

    private sealed record FranchiseeUser(int Id, string Email, string Name, string? Phone);
}
