using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Admin.Invoices;
using MiniWebsite.Application.Admin.KitManagement;
using MiniWebsite.Application.Admin.KitManagement.Dtos;
using MiniWebsite.Application.Common.Extensions;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Application.Portal.Dtos;
using MiniWebsite.Domain.Enums;
using MiniWebsite.Domain.Common;

namespace MiniWebsite.Application.Portal;

public class PortalService : IPortalService
{
    private static readonly HashSet<string> AllowedProfileContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp",
    };

    private readonly IApplicationDbContext _db;
    private readonly IWalletBalanceLookup _wallet;
    private readonly IAdminKitManagementService _kits;
    private readonly IAdminInvoiceAccessService _invoices;
    private readonly AppOptions _app;

    public PortalService(
        IApplicationDbContext db,
        IWalletBalanceLookup wallet,
        IAdminKitManagementService kits,
        IAdminInvoiceAccessService invoices,
        IOptions<AppOptions> app)
    {
        _db = db;
        _wallet = wallet;
        _kits = kits;
        _invoices = invoices;
        _app = app.Value;
    }

    public async Task<ApiResult<PortalMeDto>> GetMeAsync(string email, CancellationToken ct = default)
    {
        var user = await FindUserAsync(email, ct);
        if (user == null) return ApiResult<PortalMeDto>.Fail("User not found.");

        var mwPaid = await _db.Cards.AsNoTracking()
            .AnyAsync(c =>
                c.UserId == user.Id
                && c.PaymentStatus != null
                && c.PaymentStatus.ToLower() == "success", ct);

        var agreementPaid = false;
        var verified = false;
        var franchiseInvoiceId = (int?)null;
        if (user.LoginRole == UserRole.Franchisee)
        {
            agreementPaid = await IsFranchiseAgreementPaidAsync(user.Id, email, ct);
            franchiseInvoiceId = await FindFranchiseInvoiceIdAsync(user.Id, email, ct);

            var v = await _db.FranchiseVerifications.AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.Id)
                .Select(x => x.Status)
                .FirstOrDefaultAsync(ct);
            verified = string.Equals(v, "approved", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(v, "verified", StringComparison.OrdinalIgnoreCase);
        }

        return ApiResult<PortalMeDto>.Ok(new PortalMeDto(
            user.Id,
            user.Email,
            user.FullName,
            RoleApi(user.LoginRole),
            user.Referral.ReferralCode,
            user.Referral.CollaborationEnabled ? "YES" : "NO",
            user.Referral.SaleskitEnabled ? "YES" : "NO",
            user.Referral.Influencer ? "YES" : "NO",
            mwPaid,
            agreementPaid,
            verified,
            ToPublicProfileUrl(user.ProfileImage),
            franchiseInvoiceId.HasValue,
            franchiseInvoiceId,
            user.AssignedRole?.RoleName));
    }

    public async Task<ApiResult<ReferralPageDto>> GetReferralAsync(string email, CancellationToken ct = default)
    {
        var user = await FindUserAsync(email, ct);
        if (user == null) return ApiResult<ReferralPageDto>.Fail("User not found.");

        var isTeam = user.LoginRole == UserRole.Team;

        var earnings = await _db.ReferralEarnings.AsNoTracking()
            .Where(re => re.ReferrerUserId == user.Id)
            .ToListAsync(ct);

        var pendingAmount = earnings.Where(e => e.Status == "Pending").Sum(e => e.Amount);
        var totalEarning = earnings.Where(e => e.Status == "Paid").Sum(e => e.Amount);
        var referralCount = earnings.Select(e => e.Id).Distinct().Count();

        var referredIdsFromUsers = await _db.Users.AsNoTracking()
            .Where(u => u.Referral.ReferrerUserId == user.Id)
            .Select(u => u.Id)
            .ToListAsync(ct);

        var totalReferrals = Math.Max(referralCount, referredIdsFromUsers.Count);

        var allReferredKeys = earnings
            .Select(e => ReferredKey.From(e.ReferredUserId, e.ReferredEmail))
            .Where(k => k.UserId != null || k.Email.Length > 0)
            .Concat(referredIdsFromUsers.Select(id => ReferredKey.From(id, null)))
            .Distinct()
            .Take(500)
            .ToList();

        var allReferredIds = allReferredKeys
            .Where(k => k.UserId != null)
            .Select(k => k.UserId!.Value)
            .ToList();

        var userById = allReferredIds.Count == 0
            ? new Dictionary<int, Domain.Entities.User>()
            : await _db.Users.AsNoTracking()
                .Where(u => allReferredIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, ct);

        var cards = allReferredIds.Count == 0
            ? new List<Domain.Entities.Card>()
            : await _db.Cards.AsNoTracking()
                .Where(c => c.UserId != null && allReferredIds.Contains(c.UserId.Value))
                .OrderByDescending(c => c.Id)
                .ToListAsync(ct);

        var cardByUser = new Dictionary<int, Domain.Entities.Card>();
        foreach (var c in cards)
            cardByUser.TryAdd(c.UserId!.Value, c);

        var earningByReferred = earnings
            .GroupBy(e => ReferredKey.From(e.ReferredUserId, e.ReferredEmail))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First());

        var referredRows = new List<ReferredUserSql>();
        foreach (var key in allReferredKeys)
        {
            Domain.Entities.User? ud = null;
            Domain.Entities.Card? dc = null;
            if (key.UserId is int referredId)
            {
                userById.TryGetValue(referredId, out ud);
                cardByUser.TryGetValue(referredId, out dc);
            }
            earningByReferred.TryGetValue(key, out var re);

            referredRows.Add(new ReferredUserSql
            {
                ReferralId = re?.Id ?? 0,
                ReferredEmail = re?.ReferredEmail ?? ud?.Email,
                ReferralDate = (re?.ReferralDate ?? ud?.CreatedAt)?.ToString("o", CultureInfo.InvariantCulture),
                Amount = re?.Amount ?? 0,
                Status = re?.Status ?? "Pending",
                PaymentDate = re?.PaymentDate?.ToString("o", CultureInfo.InvariantCulture),
                IsCollaboration = re?.IsCollaboration ?? "NO",
                UserId = ud?.Id,
                ReferredRole = ud == null ? null : RoleApi(ud.LoginRole),
                UserEmail = ud?.Email,
                UserContact = ud?.Phone,
                UserName = ud?.FullName,
                CardId = dc?.Id,
                CardUploadedDate = dc?.CreatedAt?.ToString("o", CultureInfo.InvariantCulture),
                CardValidityDate = dc?.ValidityDate?.ToString("o", CultureInfo.InvariantCulture),
                ComplimentaryEnabled = dc?.ComplimentaryEnabled,
                DPaymentStatus = dc?.PaymentStatus,
                DPaymentDate = dc?.PaymentDate?.ToString("o", CultureInfo.InvariantCulture),
                FUserEmail = dc?.FranchiseeEmail,
            });
        }

        referredRows = referredRows
            .OrderByDescending(r => r.ReferralId)
            .ThenByDescending(r => TryParseDate(r.ReferralDate) ?? DateTime.MinValue)
            .ToList();

        var totalSales = 0;
        var totalMwCreated = 0;
        if (isTeam)
        {
            totalSales = referredRows
                .Where(r => !string.IsNullOrWhiteSpace(r.UserEmail)
                            && string.Equals(r.DPaymentStatus, "Success", StringComparison.OrdinalIgnoreCase))
                .Select(r => r.UserEmail!.Trim().ToLowerInvariant())
                .Distinct()
                .Count();

            totalMwCreated = referredRows.Count(r => r.CardId is > 0);
        }

        var bankEntity = await _db.UserBankDetails.AsNoTracking()
            .FirstOrDefaultAsync(b => b.UserId == user.Id, ct);

        var bank = bankEntity == null
            ? null
            : new BankSql
            {
                BankName = bankEntity.BankName,
                AccountHolder = bankEntity.AccountHolderName,
                AccountNumber = bankEntity.AccountNumber,
                IfscCode = bankEntity.IfscCode,
                UpiId = bankEntity.UpiId,
                UpiName = bankEntity.UpiName,
            };

        var bankComplete = !string.IsNullOrWhiteSpace(bank?.BankName)
            && !string.IsNullOrWhiteSpace(bank?.AccountHolder)
            && !string.IsNullOrWhiteSpace(bank?.AccountNumber)
            && !string.IsNullOrWhiteSpace(bank?.IfscCode)
            && !string.IsNullOrWhiteSpace(bank?.UpiId)
            && !string.IsNullOrWhiteSpace(bank?.UpiName);

        var code = user.Referral.ReferralCode ?? "";
        var link = string.IsNullOrWhiteSpace(code) ? "" : $"/registration/customer?ref={code}";
        var role = RoleApi(user.LoginRole);
        var pageTitle = isTeam ? "Sales Details" : "Referral Details";

        var referredUsers = referredRows.Select(MapReferredUserRow).ToList();
        var legacyEarnings = referredRows
            .Where(r => r.ReferralId > 0)
            .Select(e => new ReferralEarningRowDto(
                e.ReferralId,
                e.ReferredEmail,
                e.Status,
                e.Amount,
                FormatDate(e.ReferralDate),
                e.Status))
            .ToList();

        return ApiResult<ReferralPageDto>.Ok(new ReferralPageDto(
            role,
            isTeam,
            pageTitle,
            code,
            link,
            pendingAmount,
            totalEarning,
            totalReferrals,
            totalSales,
            totalMwCreated,
            bankComplete,
            new BankDetailsDto(
                bank?.BankName, bank?.AccountHolder, bank?.AccountNumber,
                bank?.IfscCode, bank?.UpiId, bank?.UpiName),
            referredUsers,
            legacyEarnings));
    }

    public async Task<ApiResult> SaveBankDetailsAsync(SaveBankDetailsRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(request.BankName) || string.IsNullOrWhiteSpace(request.AccountHolder)
            || string.IsNullOrWhiteSpace(request.AccountNumber) || string.IsNullOrWhiteSpace(request.IfscCode)
            || string.IsNullOrWhiteSpace(request.UpiId) || string.IsNullOrWhiteSpace(request.UpiName))
            return ApiResult.Fail("All fields are mandatory. Please fill all the required fields.");

        var userId = await _db.Users.AsNoTracking()
            .Where(u => u.Email == email)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(ct);
        if (userId == null)
            return ApiResult.Fail("No account found for this email.");

        var entity = await _db.UserBankDetails.FirstOrDefaultAsync(b => b.UserId == userId, ct);
        var now = AppClock.Now;

        if (entity != null)
        {
            entity.BankName = request.BankName.Trim();
            entity.AccountHolderName = request.AccountHolder.Trim();
            entity.AccountNumber = request.AccountNumber.Trim();
            entity.IfscCode = request.IfscCode.Trim();
            entity.UpiId = request.UpiId.Trim();
            entity.UpiName = request.UpiName.Trim();
            entity.UpdatedAt = now;
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Bank details updated successfully!");
        }

        _db.UserBankDetails.Add(new Domain.Entities.UserBankDetails
        {
            UserId = userId.Value,
            UserEmail = email,
            BankName = request.BankName.Trim(),
            AccountHolderName = request.AccountHolder.Trim(),
            AccountNumber = request.AccountNumber.Trim(),
            IfscCode = request.IfscCode.Trim(),
            UpiId = request.UpiId.Trim(),
            UpiName = request.UpiName.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        });
        await _db.SaveChangesAsync(ct);
        return ApiResult.Ok("Bank details saved successfully!");
    }

    public async Task<ApiResult<WalletPageDto>> GetWalletAsync(string email, CancellationToken ct = default)
    {
        var userId = await _db.FindUserIdAsync(email, ct);
        var balance = await _wallet.GetLatestBalanceAsync(userId, ct);

        var wallet = userId == null
            ? null
            : await _db.Wallets.AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId, ct);

        var txns = wallet == null
            ? []
            : await _db.WalletTransactions.AsNoTracking()
                .Where(t => t.WalletId == wallet.Id)
                .OrderByDescending(t => t.Id)
                .Take(10)
                .ToListAsync(ct);

        return ApiResult<WalletPageDto>.Ok(new WalletPageDto(
            balance,
            balance >= 413,
            txns.Select(r => new WalletTxnDto(
                r.Id,
                string.Equals(r.Type, "deposit", StringComparison.OrdinalIgnoreCase)
                    ? r.Amount.ToString(CultureInfo.InvariantCulture)
                    : null,
                string.Equals(r.Type, "withdraw", StringComparison.OrdinalIgnoreCase)
                    ? r.Amount.ToString(CultureInfo.InvariantCulture)
                    : null,
                r.OrderId,
                r.BalanceAfter,
                FormatDate(r.CreatedAt),
                r.Message)).ToList()));
    }

    public async Task<ApiResult<IReadOnlyList<InvoiceListItemDto>>> ListInvoicesForCardAsync(
        string email, int cardId, CancellationToken ct = default)
    {
        if (cardId <= 0) return ApiResult<IReadOnlyList<InvoiceListItemDto>>.Fail("Invalid card id.");
        var userId = await _db.FindUserIdAsync(email, ct);

        var owns = userId != null && await _db.Cards.AsNoTracking()
            .AnyAsync(c =>
                c.Id == cardId
                && (c.UserId == userId || c.FranchiseeUserId == userId), ct);
        if (!owns) return ApiResult<IReadOnlyList<InvoiceListItemDto>>.Fail("Access denied.");

        var rows = await _db.Invoices.AsNoTracking()
            .Where(i => i.CardId == cardId)
            .OrderByDescending(i => i.Id)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.InvoiceDate,
                Amount = i.TotalAmount,
                Status = i.PaymentType ?? "",
            })
            .ToListAsync(ct);

        IReadOnlyList<InvoiceListItemDto> list = rows.Select(r => new InvoiceListItemDto(
            r.Id, r.InvoiceNumber, FormatDate(r.InvoiceDate), r.Amount, r.Status)).ToList();
        return ApiResult<IReadOnlyList<InvoiceListItemDto>>.Ok(list);
    }

    public async Task<ApiResult<InvoiceDetailDto>> GetInvoiceAsync(string email, int invoiceId, CancellationToken ct = default)
    {
        var row = await _db.Invoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (row == null) return ApiResult<InvoiceDetailDto>.Fail("Invoice not found.");

        var userId = await _db.FindUserIdAsync(email, ct);
        var allowed = userId != null && row.UserId == userId;
        if (!allowed)
        {
            var cardOk = userId != null && row.CardId is > 0 && await _db.Cards.AsNoTracking()
                .AnyAsync(c =>
                    c.Id == row.CardId.Value
                    && (c.UserId == userId || c.FranchiseeUserId == userId), ct);
            if (!cardOk) return ApiResult<InvoiceDetailDto>.Fail("Access denied.");
        }

        return ApiResult<InvoiceDetailDto>.Ok(new InvoiceDetailDto(
            row.Id, row.InvoiceNumber, FormatDate(row.InvoiceDate),
            row.BillingName, row.UserEmail, row.BillingContact, row.ServiceName,
            row.TotalAmount, row.BillingGstNumber, "Use /html endpoint to download printable invoice."));
    }

    public async Task<ApiResult<InvoiceHtmlResult>> GetInvoiceHtmlAsync(string email, int invoiceId, CancellationToken ct = default)
    {
        var detail = await GetInvoiceAsync(email, invoiceId, ct);
        if (!detail.Success) return ApiResult<InvoiceHtmlResult>.Fail(detail.Message ?? "Access denied.");
        return await _invoices.GetInvoiceHtmlByIdAsync(invoiceId, ct);
    }

    public async Task<ApiResult<InvoiceHtmlResult>> GetFranchiseInvoiceHtmlAsync(string email, CancellationToken ct = default)
    {
        var user = await FindUserAsync(email, ct);
        if (user == null) return ApiResult<InvoiceHtmlResult>.Fail("User not found.");
        if (user.LoginRole != UserRole.Franchisee)
            return ApiResult<InvoiceHtmlResult>.Fail("Franchisee invoice is only available for franchisee accounts.");

        var invoiceId = await FindFranchiseInvoiceIdAsync(user.Id, email, ct);
        if (!invoiceId.HasValue)
            return ApiResult<InvoiceHtmlResult>.Fail("Invoice will be available only after franchise agreement payment verification.");

        return await _invoices.GetInvoiceHtmlByIdAsync(invoiceId.Value, ct);
    }

    public Task<ApiResult<KitExplorerDto>> GetKitExplorerAsync(string category, int folderId, CancellationToken ct = default) =>
        _kits.GetExplorerAsync(category, folderId, ct);

    public async Task<ApiResult<VerificationStatusDto>> GetVerificationAsync(string email, CancellationToken ct = default)
    {
        var me = await GetMeAsync(email, ct);
        if (!me.Success || me.Data == null) return ApiResult<VerificationStatusDto>.Fail(me.Message ?? "User not found.");

        var agreementPaid = me.Data.FranchiseAgreementPaid;
        // Non-franchisee: page may still be opened; treat agreement as N/A (true) unless franchisee
        if (!string.Equals(me.Data.Role, "FRANCHISEE", StringComparison.OrdinalIgnoreCase))
            agreementPaid = true;

        var status = "pending";
        string? message = null;
        string? pan = null, front = null, back = null, remarks = null, reviewedAt = null, reviewedBy = null;
        var isLocked = false;

        try
        {
            var row = await _db.FranchiseVerifications.AsNoTracking()
                .Where(v => v.UserId == me.Data.UserId)
                .OrderByDescending(v => v.Id)
                .FirstOrDefaultAsync(ct);

            if (row != null)
            {
                status = string.IsNullOrWhiteSpace(row.Status) ? "pending" : row.Status.Trim();
                var (p, f, b) = ParseVerificationDocs(row.DocumentPath);
                pan = p;
                front = f;
                back = b;
                remarks = row.AdminRemarks;
                reviewedAt = FormatDateTime(row.ReviewedAt);
                reviewedBy = row.ReviewedBy;
                var allDocs = !string.IsNullOrWhiteSpace(pan) && !string.IsNullOrWhiteSpace(front) && !string.IsNullOrWhiteSpace(back);
                isLocked = string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase)
                           || (string.Equals(status, "submitted", StringComparison.OrdinalIgnoreCase) && allDocs);
                if (string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase))
                    isLocked = false;
            }
            else
            {
                status = "pending";
            }
        }
        catch
        {
            status = "pending";
        }

        message = status.ToLowerInvariant() switch
        {
            "approved" or "verified" => "Congratulations! Your documents have been approved successfully.",
            "rejected" => "Your documents have been rejected. Please upload new documents.",
            "submitted" => "Thanks for sharing the Documents. Our team will verify it and confirm within 48 hrs.",
            "pending" => "Upload your PAN Card and Aadhaar Card (Front & Back) for verification.",
            _ => "Please submit your KYC documents."
        };

        var canSubmit = agreementPaid && !isLocked
            && !string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "verified", StringComparison.OrdinalIgnoreCase);

        return ApiResult<VerificationStatusDto>.Ok(new VerificationStatusDto(
            status,
            message,
            canSubmit,
            agreementPaid,
            isLocked,
            ToVerificationPublicUrl(pan),
            ToVerificationPublicUrl(front),
            ToVerificationPublicUrl(back),
            remarks,
            reviewedAt,
            reviewedBy));
    }

    public async Task<ApiResult<VerificationStatusDto>> SubmitVerificationAsync(
        SubmitVerificationRequest request, CancellationToken ct = default)
    {
        var email = (request.Email ?? "").Trim();
        if (string.IsNullOrWhiteSpace(email))
            return ApiResult<VerificationStatusDto>.Fail("Email is required.");

        var current = await GetVerificationAsync(email, ct);
        if (!current.Success || current.Data == null)
            return ApiResult<VerificationStatusDto>.Fail(current.Message ?? "Unable to load verification.");
        if (!current.Data.AgreementPaid)
            return ApiResult<VerificationStatusDto>.Fail("Complete franchise agreement payment first.");
        if (current.Data.IsLocked)
            return ApiResult<VerificationStatusDto>.Fail("Documents are locked and cannot be changed.");

        var fsDir = ResolveVerificationFsDir();
        if (string.IsNullOrWhiteSpace(fsDir))
            return ApiResult<VerificationStatusDto>.Fail("Verification upload path is not configured (App:VerificationUploadsFsPath).");

        try
        {
            Directory.CreateDirectory(fsDir);
        }
        catch (Exception ex)
        {
            return ApiResult<VerificationStatusDto>.Fail("Cannot create upload folder: " + ex.Message);
        }

        string? panFile = null, frontFile = null, backFile = null;
        try
        {
            panFile = await SaveVerificationImageAsync(fsDir, request.PanCardBase64, "pan", ct);
            frontFile = await SaveVerificationImageAsync(fsDir, request.AadhaarFrontBase64, "aadhaar_front", ct);
            backFile = await SaveVerificationImageAsync(fsDir, request.AadhaarBackBase64, "aadhaar_back", ct);
        }
        catch (Exception ex)
        {
            return ApiResult<VerificationStatusDto>.Fail(ex.Message);
        }

        // Merge with existing filenames when a slot was not re-uploaded
        var existingPan = Path.GetFileName(current.Data.PanCardUrl ?? "");
        var existingFront = Path.GetFileName(current.Data.AadhaarFrontUrl ?? "");
        var existingBack = Path.GetFileName(current.Data.AadhaarBackUrl ?? "");
        var finalPan = panFile ?? (string.IsNullOrWhiteSpace(existingPan) ? null : existingPan);
        var finalFront = frontFile ?? (string.IsNullOrWhiteSpace(existingFront) ? null : existingFront);
        var finalBack = backFile ?? (string.IsNullOrWhiteSpace(existingBack) ? null : existingBack);

        if (string.IsNullOrWhiteSpace(finalPan) || string.IsNullOrWhiteSpace(finalFront) || string.IsNullOrWhiteSpace(finalBack))
            return ApiResult<VerificationStatusDto>.Fail("All three documents are mandatory (PAN, Aadhaar Front, Aadhaar Back).");

        try
        {
            var userId = await _db.Users.AsNoTracking()
                .Where(u => u.Email == email)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(ct);
            if (userId == null)
                return ApiResult<VerificationStatusDto>.Fail("Unable to save documents: no account found for this email.");

            var entity = await _db.FranchiseVerifications
                .FirstOrDefaultAsync(v => v.UserId == userId, ct);
            var now = AppClock.Now;
            var docPath = EncodeVerificationDocs(finalPan!, finalFront!, finalBack!);

            if (entity != null)
            {
                entity.DocumentPath = docPath;
                entity.Status = "submitted";
                entity.SubmittedAt = now;
                entity.UpdatedAt = now;
            }
            else
            {
                _db.FranchiseVerifications.Add(new Domain.Entities.FranchiseVerification
                {
                    UserId = userId.Value,
                    UserEmail = email.Trim(),
                    DocumentPath = docPath,
                    Status = "submitted",
                    SubmittedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            return ApiResult<VerificationStatusDto>.Fail("Unable to save documents: " + ex.Message);
        }

        var refreshed = await GetVerificationAsync(email, ct);
        if (!refreshed.Success || refreshed.Data == null)
            return ApiResult<VerificationStatusDto>.Ok(
                current.Data with { Status = "submitted", Message = "Documents submitted successfully.", CanSubmit = false, IsLocked = true },
                "Documents submitted successfully.");

        return ApiResult<VerificationStatusDto>.Ok(refreshed.Data, "Documents submitted successfully.");
    }

    private string ResolveVerificationFsDir()
    {
        if (!string.IsNullOrWhiteSpace(_app.VerificationUploadsFsPath))
            return _app.VerificationUploadsFsPath.Trim();
        return "";
    }

    private string? ToVerificationPublicUrl(string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename)) return null;
        var name = Path.GetFileName(filename.Trim().Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(name)) return null;
        var prefix = string.IsNullOrWhiteSpace(_app.VerificationUploadsRelativePath)
            ? "assets/upload/verification"
            : _app.VerificationUploadsRelativePath.Trim().Trim('/').Replace('\\', '/');
        var baseUrl = (_app.SiteBaseUrl ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return "/" + prefix + "/" + name;
        return baseUrl + "/" + prefix + "/" + name;
    }

    private static async Task<string?> SaveVerificationImageAsync(
        string fsDir, string? base64OrDataUrl, string kind, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(base64OrDataUrl)) return null;
        var raw = base64OrDataUrl.Trim();
        var comma = raw.IndexOf(',');
        if (raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
            raw = raw[(comma + 1)..];

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(raw);
        }
        catch
        {
            throw new InvalidOperationException($"Invalid image data for {kind}.");
        }

        if (bytes.Length == 0) return null;
        if (bytes.Length > 2_500_000)
            throw new InvalidOperationException($"{kind} image is too large (max ~2.5MB).");

        var stamp = AppClock.Now.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture);
        var fileName = $"{stamp}_{kind}.jpg";
        var fullPath = Path.Combine(fsDir, fileName);
        await File.WriteAllBytesAsync(fullPath, bytes, ct);
        return fileName;
    }

    public async Task<ApiResult<IdCardProfileDto>> GetIdCardAsync(string email, CancellationToken ct = default)
    {
        var user = await FindUserAsync(email, ct);
        if (user == null) return ApiResult<IdCardProfileDto>.Fail("User not found.");

        var isFranchisee = user.LoginRole == UserRole.Franchisee;
        var employeeId = isFranchisee
            ? user.Id.ToString("D3", CultureInfo.InvariantCulture)
            : user.Id.ToString("D6", CultureInfo.InvariantCulture);
        var idPrefix = isFranchisee ? "FR ID: " : "ID: ";
        var designation = isFranchisee ? "Franchise Partner" : "Sales Executive";

        // PHP idcard: if phone empty, fall back to any user_details row with same email
        var phone = user.Phone?.Trim() ?? "";
        if (string.IsNullOrEmpty(phone) && !string.IsNullOrWhiteSpace(user.Email))
        {
            var phoneRow = await _db.Users.AsNoTracking()
                .Where(u => u.Email == user.Email && u.Phone != null && u.Phone != "")
                .OrderByDescending(u => u.Id)
                .Select(u => u.Phone)
                .FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(phoneRow))
                phone = phoneRow.Trim();
        }

        return ApiResult<IdCardProfileDto>.Ok(new IdCardProfileDto(
            user.FullName,
            user.Email,
            string.IsNullOrEmpty(phone) ? null : phone,
            user.Department,
            ToPublicIdCardImageUrl(user.ProfileImage),
            RoleApi(user.LoginRole),
            employeeId,
            idPrefix,
            designation));
    }

    public async Task<ApiResult<ProfileImageUploadDto>> UploadProfileImageAsync(
        string email,
        Stream fileStream,
        string originalFileName,
        string contentType,
        long contentLength,
        CancellationToken ct = default)
    {
        var user = await FindUserAsync(email, ct);
        if (user == null) return ApiResult<ProfileImageUploadDto>.Fail("User not found.");

        if (fileStream == null || contentLength <= 0)
            return ApiResult<ProfileImageUploadDto>.Fail("No image file selected. Please choose an image to upload.");

        // Allow up to ~2MB raw; PHP compresses to ~250KB — we accept a bit more for uncropped photos
        const long maxBytes = 2_000_000;
        if (contentLength > maxBytes)
            return ApiResult<ProfileImageUploadDto>.Fail("Image is too large. Max size is 2MB.");

        var ctNorm = (contentType ?? "").Split(';')[0].Trim();
        if (string.IsNullOrWhiteSpace(ctNorm) || !AllowedProfileContentTypes.Contains(ctNorm))
            return ApiResult<ProfileImageUploadDto>.Fail("Invalid image type. Use JPG, PNG, GIF, or WebP.");

        var fsDir = ResolveProfileFsDir();
        if (string.IsNullOrWhiteSpace(fsDir))
            return ApiResult<ProfileImageUploadDto>.Fail("Profile upload path is not configured (App:ProfileUploadsFsPath).");

        try
        {
            Directory.CreateDirectory(fsDir);
        }
        catch (Exception ex)
        {
            return ApiResult<ProfileImageUploadDto>.Fail("Cannot create upload folder: " + ex.Message);
        }

        var ext = Path.GetExtension(originalFileName ?? "").ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"))
        {
            ext = ctNorm switch
            {
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".jpg",
            };
        }

        var uniqueName = $"{Guid.NewGuid():N}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
        var fullPath = Path.Combine(fsDir, uniqueName);
        var relativePrefix = string.IsNullOrWhiteSpace(_app.ProfileUploadsRelativePath)
            ? "assets/upload/profile_image"
            : _app.ProfileUploadsRelativePath.Trim().Trim('/').Replace('\\', '/');
        var dbPath = $"{relativePrefix}/{uniqueName}";

        try
        {
            await using (var fs = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await fileStream.CopyToAsync(fs, ct);
            }

            // Delete previous file when it lives under our profile folder
            var oldRel = user.ProfileImage?.Trim().Replace('\\', '/');
            if (!string.IsNullOrWhiteSpace(oldRel)
                && oldRel.StartsWith(relativePrefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                var oldName = Path.GetFileName(oldRel);
                if (!string.IsNullOrWhiteSpace(oldName))
                {
                    var oldFull = Path.Combine(fsDir, oldName);
                    if (File.Exists(oldFull))
                    {
                        try { File.Delete(oldFull); } catch { /* ignore */ }
                    }
                }
            }

            user.ProfileImage = dbPath;
            user.UpdatedAt = AppClock.Now;
            await _db.SaveChangesAsync(ct);

            var publicUrl = ToPublicProfileUrl(dbPath) ?? "";
            return ApiResult<ProfileImageUploadDto>.Ok(
                new ProfileImageUploadDto(dbPath, publicUrl),
                "Profile image updated successfully! Your new image will be visible immediately.");
        }
        catch (Exception ex)
        {
            if (File.Exists(fullPath))
            {
                try { File.Delete(fullPath); } catch { /* ignore */ }
            }
            return ApiResult<ProfileImageUploadDto>.Fail("Failed to save profile image: " + ex.Message);
        }
    }

    public async Task<ApiResult<CustomerTrackerPageDto>> ListCustomersAsync(
        string ownerEmail, string? search = null, CancellationToken ct = default)
    {
        var user = await FindUserAsync(ownerEmail, ct);
        if (user == null) return ApiResult<CustomerTrackerPageDto>.Fail("User not found.");

        var role = RoleApi(user.LoginRole);
        var collaboration = user.Referral.CollaborationEnabled;
        var canTeam = user.LoginRole is UserRole.Team or UserRole.Franchisee
            || (user.LoginRole == UserRole.Customer && collaboration);
        var variant = canTeam ? "team" : "customer";
        var showApproachedFor = variant == "team" && user.LoginRole != UserRole.Franchisee;

        try
        {
            var q = search?.Trim() ?? "";
            var customersQuery = _db.Customers.AsNoTracking()
                .Where(c => c.OwnerUserId == user.Id && c.OwnerRole == role);

            if (!string.IsNullOrEmpty(q))
            {
                var like = q.ToLowerInvariant();
                customersQuery = customersQuery.Where(c =>
                    c.CustomerName.ToLower().Contains(like)
                    || c.PhoneNumber.ToLower().Contains(like)
                    || (c.EmailId != null && c.EmailId.ToLower().Contains(like))
                    || (c.CompanyName != null && c.CompanyName.ToLower().Contains(like))
                    || c.Source.ToLower().Contains(like)
                    || (c.Website != null && c.Website.ToLower().Contains(like))
                    || (c.AddressLine1 != null && c.AddressLine1.ToLower().Contains(like))
                    || (c.AreaCity != null && c.AreaCity.ToLower().Contains(like))
                    || (c.Comments != null && c.Comments.ToLower().Contains(like))
                    || (c.LabelTag != null && c.LabelTag.ToLower().Contains(like))
                    || c.Status.ToLower().Contains(like)
                    || (c.BusinessType != null && c.BusinessType.ToLower().Contains(like))
                    || (c.ApproachedFor != null && c.ApproachedFor.ToLower().Contains(like))
                    || (c.FollowupMethod != null && c.FollowupMethod.ToLower().Contains(like)));
            }

            var rows = await customersQuery
                .OrderByDescending(c => c.Id)
                .Take(500)
                .Select(c => new
                {
                    c.Id,
                    c.CustomerName,
                    c.PhoneNumber,
                    c.EmailId,
                    c.CompanyName,
                    c.Website,
                    c.AddressLine1,
                    c.AreaCity,
                    c.Comments,
                    c.LabelTag,
                    c.Source,
                    c.Status,
                    c.BusinessType,
                    c.ApproachedFor,
                    c.FollowupMethod,
                    c.CreatedAt,
                    c.UpdatedAt,
                    c.LastSharedAt,
                    LastFollowupAt = c.Followups
                        .Where(f => f.OwnerUserId == c.OwnerUserId && f.OwnerRole == c.OwnerRole)
                        .Select(f => (DateTime?)f.FollowupDatetime)
                        .Max(),
                })
                .ToListAsync(ct);

            var businessTypes = await LoadBusinessTypeOptionsAsync(ct);

            return ApiResult<CustomerTrackerPageDto>.Ok(new CustomerTrackerPageDto(
                variant,
                showApproachedFor,
                role,
                user.Id,
                businessTypes,
                rows.Select(r => MapCustomerRow(
                    r.Id, r.CustomerName, r.PhoneNumber, r.EmailId, r.CompanyName, r.Website,
                    r.AddressLine1, r.AreaCity, r.Comments, r.LabelTag, r.Source, r.Status,
                    r.BusinessType, r.ApproachedFor, r.FollowupMethod,
                    r.CreatedAt, r.UpdatedAt, r.LastSharedAt, r.LastFollowupAt)).ToList()));
        }
        catch (Exception ex)
        {
            return ApiResult<CustomerTrackerPageDto>.Fail("Unable to load customers: " + ex.Message);
        }
    }

    public async Task<ApiResult<CustomerManagerRowDto>> SaveCustomerAsync(SaveCustomerRequest request, CancellationToken ct = default)
    {
        var user = await FindUserAsync(request.OwnerEmail, ct);
        if (user == null) return ApiResult<CustomerManagerRowDto>.Fail("User not found.");

        var name = (request.CustomerName ?? "").Trim();
        var phone = PhoneDigits(request.PhoneNumber ?? "");
        if (string.IsNullOrWhiteSpace(name))
            return ApiResult<CustomerManagerRowDto>.Fail("Customer name is required.");
        if (string.IsNullOrWhiteSpace(phone) || phone.Length > 12)
            return ApiResult<CustomerManagerRowDto>.Fail("Valid phone number is required (max 12 digits).");

        var role = RoleApi(user.LoginRole);
        var collaboration = user.Referral.CollaborationEnabled;
        var isTeam = user.LoginRole is UserRole.Team or UserRole.Franchisee
            || (user.LoginRole == UserRole.Customer && collaboration);
        var showApproachedFor = isTeam && user.LoginRole != UserRole.Franchisee;

        var label = Limit((request.LabelTag ?? "").Trim(), 18);
        var source = Limit(string.IsNullOrWhiteSpace(request.Source) ? "Direct" : request.Source.Trim(), 18);
        var status = Limit(string.IsNullOrWhiteSpace(request.Status) ? "Followup required" : request.Status.Trim(), 40);
        var businessType = isTeam ? Limit((request.BusinessType ?? "").Trim(), 150) : "";
        var approachedFor = showApproachedFor ? Limit((request.ApproachedFor ?? "").Trim(), 150) : "";
        var followupMethod = isTeam ? Limit((request.FollowupMethod ?? "").Trim(), 150) : "";
        var emailId = (request.EmailId ?? "").Trim();
        var company = (request.CompanyName ?? "").Trim();
        var website = (request.Website ?? "").Trim();
        var line1 = (request.AddressLine1 ?? "").Trim();
        var areaCity = (request.AreaCity ?? "").Trim();
        var comments = (request.Comments ?? "").Trim();

        try
        {
            if (request.Id is > 0)
            {
                var entity = await _db.Customers.FirstOrDefaultAsync(
                    c => c.Id == request.Id.Value && c.OwnerUserId == user.Id && c.OwnerRole == role, ct);
                if (entity == null)
                    return ApiResult<CustomerManagerRowDto>.Fail("Customer not found.");

                if (!isTeam)
                {
                    entity.CustomerName = name;
                    entity.PhoneNumber = phone;
                }

                entity.LabelTag = label;
                entity.EmailId = emailId;
                entity.CompanyName = company;
                entity.Website = website;
                entity.Source = source;
                entity.Status = status;
                entity.AddressLine1 = line1;
                entity.AreaCity = areaCity;
                entity.Comments = comments;
                entity.BusinessType = businessType;
                entity.ApproachedFor = approachedFor;
                entity.FollowupMethod = followupMethod;
                entity.UpdatedAt = AppClock.Now;
            }
            else
            {
                _db.Customers.Add(new Domain.Entities.Customer
                {
                    OwnerUserId = user.Id,
                    OwnerRole = role,
                    CustomerName = name,
                    LabelTag = label,
                    PhoneNumber = phone,
                    EmailId = emailId,
                    CompanyName = company,
                    Website = website,
                    AddressLine1 = line1,
                    AreaCity = areaCity,
                    Comments = comments,
                    Source = source,
                    Status = status,
                    BusinessType = businessType,
                    ApproachedFor = approachedFor,
                    FollowupMethod = followupMethod,
                    CreatedAt = AppClock.Now,
                    UpdatedAt = AppClock.Now,
                });
            }

            await _db.SaveChangesAsync(ct);

            var page = await ListCustomersAsync(request.OwnerEmail, null, ct);
            var row = page.Data?.Customers.FirstOrDefault(c =>
                request.Id is > 0 ? c.Id == request.Id.Value : c.CustomerName == name && c.PhoneNumber == phone);
            return ApiResult<CustomerManagerRowDto>.Ok(
                row ?? new CustomerManagerRowDto(
                    request.Id ?? 0, name, phone, emailId, company, website, line1, areaCity,
                    MergeAddress(line1, areaCity), comments, label is { Length: > 0 } ? label : "Regular",
                    source, status, businessType, approachedFor, followupMethod, null, null, null),
                request.Id is > 0 ? "Customer updated." : "Customer saved.");
        }
        catch (Exception ex)
        {
            return ApiResult<CustomerManagerRowDto>.Fail("Unable to save customer: " + ex.Message);
        }
    }

    public async Task<ApiResult> DeleteCustomerAsync(string ownerEmail, int id, CancellationToken ct = default)
    {
        var user = await FindUserAsync(ownerEmail, ct);
        if (user == null) return ApiResult.Fail("User not found.");
        var role = RoleApi(user.LoginRole);
        try
        {
            var entity = await _db.Customers.FirstOrDefaultAsync(
                c => c.Id == id && c.OwnerUserId == user.Id && c.OwnerRole == role, ct);
            if (entity == null) return ApiResult.Fail("Customer not found.");

            var followups = await _db.CustomerFollowups
                .Where(f => f.CustomerId == id && f.OwnerUserId == user.Id && f.OwnerRole == role)
                .ToListAsync(ct);
            _db.CustomerFollowups.RemoveRange(followups);
            _db.Customers.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Customer deleted.");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Unable to delete: " + ex.Message);
        }
    }

    public async Task<ApiResult<IReadOnlyList<CustomerFollowupRowDto>>> ListFollowupsAsync(
        string ownerEmail, int customerId, CancellationToken ct = default)
    {
        var user = await FindUserAsync(ownerEmail, ct);
        if (user == null) return ApiResult<IReadOnlyList<CustomerFollowupRowDto>>.Fail("User not found.");
        var role = RoleApi(user.LoginRole);
        try
        {
            var rows = await _db.CustomerFollowups.AsNoTracking()
                .Where(f => f.CustomerId == customerId && f.OwnerUserId == user.Id && f.OwnerRole == role)
                .OrderByDescending(f => f.FollowupDatetime)
                .ThenByDescending(f => f.Id)
                .Select(f => new
                {
                    f.Id,
                    f.FollowupDatetime,
                    f.FollowupMethod,
                    f.FollowupStatus,
                    f.Comments,
                })
                .ToListAsync(ct);

            IReadOnlyList<CustomerFollowupRowDto> list = rows.Select(r => new CustomerFollowupRowDto(
                r.Id,
                FormatDateTime(r.FollowupDatetime),
                r.FollowupMethod ?? "",
                r.FollowupStatus ?? "",
                r.Comments)).ToList();
            return ApiResult<IReadOnlyList<CustomerFollowupRowDto>>.Ok(list);
        }
        catch (Exception ex)
        {
            return ApiResult<IReadOnlyList<CustomerFollowupRowDto>>.Fail("Unable to load followups: " + ex.Message);
        }
    }

    public async Task<ApiResult> AddFollowupAsync(AddFollowupRequest request, CancellationToken ct = default)
    {
        var user = await FindUserAsync(request.OwnerEmail, ct);
        if (user == null) return ApiResult.Fail("User not found.");

        var methods = new[] { "Call", "Visited", "WhatsApp", "Email" };
        var statuses = new[] { "Followup required", "Phone Busy/Not Picked", "Important", "Joined", "Interested", "Not Interested" };
        var method = (request.FollowupMethod ?? "").Trim();
        var status = (request.FollowupStatus ?? "").Trim();
        if (string.IsNullOrWhiteSpace(request.FollowupDatetime) || !methods.Contains(method) || !statuses.Contains(status))
            return ApiResult.Fail("Please fill Date & Time, Followup Method, and Status.");

        if (!DateTime.TryParse(request.FollowupDatetime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt)
            && !DateTime.TryParse(request.FollowupDatetime, out dt))
            return ApiResult.Fail("Invalid follow-up date/time.");

        var role = RoleApi(user.LoginRole);
        var comments = (request.FollowupComments ?? "").Trim();
        try
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(
                c => c.Id == request.CustomerId && c.OwnerUserId == user.Id && c.OwnerRole == role, ct);
            if (customer == null) return ApiResult.Fail("Customer not found.");

            _db.CustomerFollowups.Add(new Domain.Entities.CustomerFollowup
            {
                CustomerId = request.CustomerId,
                OwnerUserId = user.Id,
                OwnerRole = role,
                FollowupDatetime = dt,
                FollowupMethod = method,
                FollowupStatus = status,
                Comments = comments,
                CreatedAt = AppClock.Now,
            });

            customer.Status = status;
            customer.FollowupMethod = method;
            customer.Comments = comments;
            customer.UpdatedAt = AppClock.Now;

            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("Followup added successfully.");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("Unable to add followup: " + ex.Message);
        }
    }

    public async Task<ApiResult> RecordLastSharedAsync(string ownerEmail, int customerId, CancellationToken ct = default)
    {
        var user = await FindUserAsync(ownerEmail, ct);
        if (user == null) return ApiResult.Fail("User not found.");
        var role = RoleApi(user.LoginRole);
        try
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(
                c => c.Id == customerId && c.OwnerUserId == user.Id && c.OwnerRole == role, ct);
            if (customer == null) return ApiResult.Fail("Customer not found.");
            customer.LastSharedAt = AppClock.Now;
            await _db.SaveChangesAsync(ct);
            return ApiResult.Ok("ok");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail(ex.Message);
        }
    }

    private async Task<IReadOnlyList<string>> LoadBusinessTypeOptionsAsync(CancellationToken ct)
    {
        var fallback = new List<string> { "Retail", "Wholesale", "Service", "Online", "Manufacturing", "Other" };
        try
        {
            var list = await _db.ProductCategories.AsNoTracking()
                .Where(c => c.IsActive && c.BusinessCategory != null && c.BusinessCategory != "")
                .Select(c => c.BusinessCategory!)
                .Distinct()
                .OrderBy(n => n)
                .Take(200)
                .ToListAsync(ct);
            return list.Count > 0 ? list : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static CustomerManagerRowDto MapCustomerRow(
        int id,
        string? customerName,
        string? phoneNumber,
        string? emailId,
        string? companyName,
        string? website,
        string? addressLine1,
        string? areaCity,
        string? comments,
        string? labelTag,
        string? source,
        string? status,
        string? businessType,
        string? approachedFor,
        string? followupMethod,
        DateTime createdAt,
        DateTime? updatedAt,
        DateTime? lastSharedAt,
        DateTime? lastFollowupAt)
    {
        var a1 = (addressLine1 ?? "").Trim();
        var ac = (areaCity ?? "").Trim();
        var label = string.IsNullOrWhiteSpace(labelTag) ? "Regular" : labelTag.Trim();
        var lastUpdated = lastFollowupAt.HasValue
            ? FormatDateTime(lastFollowupAt)
            : FormatDateTime(updatedAt);
        return new CustomerManagerRowDto(
            id,
            customerName ?? "",
            phoneNumber ?? "",
            string.IsNullOrWhiteSpace(emailId) ? null : emailId,
            string.IsNullOrWhiteSpace(companyName) ? null : companyName,
            string.IsNullOrWhiteSpace(website) ? null : website,
            string.IsNullOrWhiteSpace(a1) ? null : a1,
            string.IsNullOrWhiteSpace(ac) ? null : ac,
            MergeAddress(a1, ac),
            comments,
            label,
            string.IsNullOrWhiteSpace(source) ? "Direct" : source!,
            string.IsNullOrWhiteSpace(status) ? "Followup required" : status!,
            string.IsNullOrWhiteSpace(businessType) ? null : businessType,
            string.IsNullOrWhiteSpace(approachedFor) ? null : approachedFor,
            string.IsNullOrWhiteSpace(followupMethod) ? null : followupMethod,
            FormatDate(createdAt),
            FormatDateTime(lastSharedAt) ?? "-",
            lastUpdated ?? "-");
    }

    private static string? MergeAddress(string? a1, string? ac)
    {
        a1 = (a1 ?? "").Trim();
        ac = (ac ?? "").Trim();
        var merged = (a1 + ((a1 != "" && ac != "") ? ", " : "") + ac).Trim();
        return string.IsNullOrEmpty(merged) ? null : merged;
    }

    private static string PhoneDigits(string v) =>
        new string(v.Where(char.IsDigit).ToArray());

    private static string Limit(string v, int max) =>
        v.Length <= max ? v : v[..max];

    private static string? FormatDateTime(DateTime? value)
    {
        if (value is not { } d || d.Year < 2) return null;
        return d.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    private static string? FormatDateTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("0000", StringComparison.Ordinal)) return null;
        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt)
            ? dt.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)
            : raw;
    }

    /// <summary>Matches PHP isFranchiseeRegistrationAgreementPaid().</summary>
    private async Task<bool> IsFranchiseAgreementPaidAsync(int userId, string email, CancellationToken ct)
    {
        var emailKey = email.Trim().ToLowerInvariant();

        try
        {
            var viaFranchisePayments = await _db.FranchisePayments.AsNoTracking()
                .AnyAsync(p =>
                    p.FranchiseUserId == userId
                    && p.PaymentStatus.ToLower() == "success", ct);
            if (viaFranchisePayments) return true;
        }
        catch
        {
            // franchise_payments may not exist
        }

        try
        {
            return await _db.Invoices.AsNoTracking()
                .AnyAsync(i =>
                    (i.UserId == userId || i.BillingEmail.ToLower() == emailKey)
                    && (i.PaymentStatus.ToLower() == "success" || i.PaymentStatus.ToLower() == "paid")
                    && (i.ServiceName == "Franchisee Registration Fees"
                        || i.ServiceName == "Franchisee Registration"
                        || i.PaymentType.ToLower() == "franchisee"
                        || i.ReferenceNumber.StartsWith("FRAN")), ct);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Matches PHP sidebar download_invoice availability check (invoice row exists).</summary>
    private async Task<int?> FindFranchiseInvoiceIdAsync(int userId, string email, CancellationToken ct)
    {
        var emailKey = email.Trim().ToLowerInvariant();
        try
        {
            return await _db.Invoices.AsNoTracking()
                .Where(i =>
                    (i.UserId == userId || i.BillingEmail.ToLower() == emailKey)
                    && (i.ServiceName == "Franchisee Registration Fees"
                        || i.PaymentType.ToLower() == "franchisee"
                        || i.ReferenceNumber.StartsWith("FRAN")))
                .OrderByDescending(i => i.Id)
                .Select(i => (int?)i.Id)
                .FirstOrDefaultAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    private static (string? Pan, string? Front, string? Back) ParseVerificationDocs(string? documentPath)
    {
        if (string.IsNullOrWhiteSpace(documentPath)) return (null, null, null);
        var parts = documentPath.Split('|', 3, StringSplitOptions.None);
        string? At(int i) => parts.Length > i && !string.IsNullOrWhiteSpace(parts[i]) ? parts[i].Trim() : null;
        return (At(0), At(1), At(2));
    }

    private static string EncodeVerificationDocs(string pan, string front, string back) =>
        $"{pan}|{front}|{back}";

    private async Task<Domain.Entities.User?> FindUserAsync(string email, CancellationToken ct) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLowerInvariant(), ct);
    private string ResolveProfileFsDir()
    {
        if (!string.IsNullOrWhiteSpace(_app.ProfileUploadsFsPath))
            return _app.ProfileUploadsFsPath.Trim();
        return "";
    }

    private string? ToPublicProfileUrl(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return null;
        var path = stored.Trim().Replace('\\', '/');
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return path;

        // Match PHP header.php: only use DB path when the file exists on disk.
        if (!ProfileImageFileExists(path)) return null;

        var baseUrl = (_app.SiteBaseUrl ?? "").TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return "/" + path.TrimStart('/');
        return baseUrl + "/" + path.TrimStart('/');
    }

    /// <summary>
    /// Public URL for ID card photo. PHP idcard stores paths like <c>uploads/…_profile.jpg</c>
    /// relative to <c>user/idcard/</c>; portal uploads use <c>assets/upload/profile_image/…</c>.
    /// </summary>
    private string? ToPublicIdCardImageUrl(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return null;
        var path = stored.Trim().Replace('\\', '/');
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return path;

        // Legacy PHP idcard relative path
        if (path.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("user/idcard/", StringComparison.OrdinalIgnoreCase))
        {
            var rel = path.StartsWith("user/idcard/", StringComparison.OrdinalIgnoreCase)
                ? path
                : "user/idcard/" + path.TrimStart('/');
            var baseUrl = (_app.SiteBaseUrl ?? "").TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl)) return "/" + rel;
            return baseUrl + "/" + rel;
        }

        return ToPublicProfileUrl(stored);
    }

    /// <summary>True when <paramref name="relativePath"/> exists under the PHP site root (or profile upload folder).</summary>
    private bool ProfileImageFileExists(string relativePath)
    {
        var fsDir = ResolveProfileFsDir();
        if (string.IsNullOrWhiteSpace(fsDir)) return true; // cannot verify — keep URL

        try
        {
            var prefix = string.IsNullOrWhiteSpace(_app.ProfileUploadsRelativePath)
                ? "assets/upload/profile_image"
                : _app.ProfileUploadsRelativePath.Trim().Trim('/').Replace('\\', '/');

            if (relativePath.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                var name = Path.GetFileName(relativePath);
                if (string.IsNullOrWhiteSpace(name)) return false;
                return File.Exists(Path.Combine(fsDir, name));
            }

            // e.g. assets/images/… — resolve against site root (3 levels up from profile_image)
            var siteRoot = Path.GetFullPath(Path.Combine(fsDir, "..", "..", ".."));
            var full = Path.GetFullPath(
                Path.Combine(siteRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(siteRoot, StringComparison.OrdinalIgnoreCase)) return false;
            return File.Exists(full);
        }
        catch
        {
            return false;
        }
    }

    private static ReferredUserRowDto MapReferredUserRow(ReferredUserSql row)
    {
        var isFranchise = string.Equals(row.IsCollaboration, "YES", StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.ReferredRole, "FRANCHISEE", StringComparison.OrdinalIgnoreCase);

        string mwFrLabel;
        var mwFrIsLink = false;
        if (isFranchise && row.UserId is > 0)
        {
            mwFrLabel = "FR - " + row.UserId.Value.ToString("D3", CultureInfo.InvariantCulture);
        }
        else if (row.CardId is > 0)
        {
            mwFrLabel = row.CardId.Value.ToString(CultureInfo.InvariantCulture);
            mwFrIsLink = true;
        }
        else
        {
            mwFrLabel = "N/A";
        }

        var uploaded = TryParseDate(row.CardUploadedDate);
        var validity = TryParseDate(row.CardValidityDate);
        var paymentDate = TryParseDate(row.DPaymentDate);
        var referralPayDate = TryParseDate(row.PaymentDate);

        string? validityDisplay = null;
        if (validity.HasValue)
            validityDisplay = validity.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        else if (uploaded.HasValue)
        {
            if (string.Equals(row.ComplimentaryEnabled, "Yes", StringComparison.OrdinalIgnoreCase))
                validityDisplay = uploaded.Value.AddYears(1).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            else if (string.Equals(row.DPaymentStatus, "Success", StringComparison.OrdinalIgnoreCase) && paymentDate.HasValue)
                validityDisplay = paymentDate.Value.AddYears(1).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            else
                validityDisplay = uploaded.Value.AddDays(7).ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        }

        var (mwStatusClass, mwStatus) = ResolveMwShortStatus(
            row.ComplimentaryEnabled, row.DPaymentStatus, validity, uploaded, row.FUserEmail);

        string userPayLabel;
        string userPayClass;
        if (string.Equals(row.DPaymentStatus, "Success", StringComparison.OrdinalIgnoreCase))
        {
            var pd = paymentDate?.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
                     ?? AppClock.Now.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            userPayLabel = "Paid on " + pd;
            userPayClass = "bg-success";
        }
        else
        {
            userPayLabel = "Unpaid";
            userPayClass = "bg-unpaid";
        }

        string refPayLabel;
        string refPayClass;
        var status = row.Status ?? "";
        if (string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            refPayLabel = "Paid on " + (referralPayDate?.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) ?? "N/A");
            refPayClass = "bg-success";
        }
        else if (string.Equals(status, "Partial", StringComparison.OrdinalIgnoreCase)
                 && string.Equals(row.DPaymentStatus, "Success", StringComparison.OrdinalIgnoreCase))
        {
            refPayLabel = "Partial Payment";
            refPayClass = "bg-pending";
        }
        else if (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase)
                 && string.Equals(row.DPaymentStatus, "Success", StringComparison.OrdinalIgnoreCase))
        {
            refPayLabel = "Pending";
            refPayClass = "bg-pending";
        }
        else
        {
            refPayLabel = "Not Eligible";
            refPayClass = "not-eligible";
        }

        return new ReferredUserRowDto(
            row.UserId,
            row.CardId,
            mwFrLabel,
            mwFrIsLink,
            row.ReferredEmail ?? row.UserEmail,
            isFranchise ? "Franchise" : "MW",
            string.IsNullOrWhiteSpace(row.UserName) ? "Unknown" : row.UserName,
            string.IsNullOrWhiteSpace(row.UserContact) ? "N/A" : row.UserContact,
            FormatDate(row.ReferralDate) ?? "-",
            "Referral",
            FormatDate(row.CardUploadedDate) ?? "-",
            validityDisplay ?? "-",
            mwStatus,
            mwStatusClass,
            userPayLabel,
            userPayClass,
            row.Amount,
            refPayLabel,
            refPayClass);
    }

    private static (string Class, string Text) ResolveMwShortStatus(
        string? complimentary,
        string? paymentStatus,
        DateTime? validity,
        DateTime? uploaded,
        string? fUserEmail)
    {
        if (string.Equals(complimentary, "Yes", StringComparison.OrdinalIgnoreCase))
            return ("bg-success", "Active");

        if (string.Equals(paymentStatus, "Success", StringComparison.OrdinalIgnoreCase))
        {
            if (validity.HasValue && validity.Value < AppClock.Now)
                return ("bg-secondary lightGray", "Expired on " + validity.Value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
            return ("bg-success", "Active");
        }

        var trialEnd = uploaded?.AddDays(7);
        var trialExpired = trialEnd.HasValue && trialEnd.Value < AppClock.Now;
        var franchiseLinked = !string.IsNullOrWhiteSpace(fUserEmail) && fUserEmail.Trim().Length >= 3;

        if (franchiseLinked)
            return trialExpired ? ("bg-secondary lightGray", "Inactive") : ("bg-pending", "Pending Payment");

        if (trialExpired)
            return ("bg-secondary lightGray", "Inactive");

        return ("bg-pending", "7 Day Trial");
    }

    private static DateTime? TryParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("0000", StringComparison.Ordinal)) return null;
        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt) ? dt : null;
    }

    private static string RoleApi(UserRole role) => role switch
    {
        UserRole.Customer => "CUSTOMER",
        UserRole.Franchisee => "FRANCHISEE",
        UserRole.Team => "TEAM",
        UserRole.Admin => "ADMIN",
        _ => "CUSTOMER"
    };

    private static string? FormatDate(DateTime? value)
    {
        if (value is not { } d || d.Year < 2) return null;
        return d.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    }

    private static string? FormatDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("0000", StringComparison.Ordinal)) return null;
        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt)
            ? dt.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
            : raw;
    }

    private sealed class BankSql
    {
        public string? BankName { get; set; }
        public string? AccountHolder { get; set; }
        public string? AccountNumber { get; set; }
        public string? IfscCode { get; set; }
        public string? UpiId { get; set; }
        public string? UpiName { get; set; }
    }

    private sealed class ReferredUserSql
    {
        public int ReferralId { get; set; }
        public string? ReferredEmail { get; set; }
        public string? ReferralDate { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public string? PaymentDate { get; set; }
        public string? IsCollaboration { get; set; }
        public int? UserId { get; set; }
        public string? ReferredRole { get; set; }
        public string? UserEmail { get; set; }
        public string? UserContact { get; set; }
        public string? UserName { get; set; }
        public int? CardId { get; set; }
        public string? CardUploadedDate { get; set; }
        public string? CardValidityDate { get; set; }
        public string? ComplimentaryEnabled { get; set; }
        public string? DPaymentStatus { get; set; }
        public string? DPaymentDate { get; set; }
        public string? FUserEmail { get; set; }
    }

    /// <summary>Referred user identity; earnings without a linked account fall back to their email.</summary>
    private readonly record struct ReferredKey(int? UserId, string Email)
    {
        public static ReferredKey From(int? userId, string? email) => userId is int id
            ? new ReferredKey(id, "")
            : new ReferredKey(null, (email ?? "").Trim().ToLowerInvariant());
    }
}
