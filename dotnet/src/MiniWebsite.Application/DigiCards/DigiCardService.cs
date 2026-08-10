using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Common.Options;
using MiniWebsite.Application.DigiCards.Dtos;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;

namespace MiniWebsite.Application.DigiCards;

public interface IDigiCardService
{
    Task<ApiResult<IReadOnlyList<DigiCardListItemDto>>> ListByUserEmailAsync(string userEmail, CancellationToken ct = default);
    Task<ApiResult<IReadOnlyList<DigiCardListItemDto>>> ListByFranchiseeEmailAsync(string franchiseeEmail, CancellationToken ct = default);
    Task<ApiResult<DigiCardDetailDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ApiResult<DigiCardSlugLookupDto>> ResolvePreviousSlugAsync(string previousSlug, CancellationToken ct = default);
    Task<ApiResult<CustomerDashboardDto>> GetCustomerDashboardAsync(string email, CancellationToken ct = default);
    Task<ApiResult<FranchiseeDashboardDto>> GetFranchiseeDashboardAsync(string email, CancellationToken ct = default);
}

public class DigiCardService : IDigiCardService
{
    private readonly IApplicationDbContext _db;
    private readonly IWalletBalanceLookup _wallet;
    private readonly AppOptions _app;

    public DigiCardService(IApplicationDbContext db, IWalletBalanceLookup wallet, IOptions<AppOptions> app)
    {
        _db = db;
        _wallet = wallet;
        _app = app.Value;
    }

    public async Task<ApiResult<IReadOnlyList<DigiCardListItemDto>>> ListByUserEmailAsync(string userEmail, CancellationToken ct = default)
    {
        var email = Normalize(userEmail);
        var cards = await _db.Cards.AsNoTracking()
            .Where(c => c.UserEmail != null && c.UserEmail.ToLower() == email)
            .OrderByDescending(c => c.Id)
            .ToListAsync(ct);

        return ApiResult<IReadOnlyList<DigiCardListItemDto>>.Ok(cards.Select(MapListItem).ToList());
    }

    public async Task<ApiResult<IReadOnlyList<DigiCardListItemDto>>> ListByFranchiseeEmailAsync(string franchiseeEmail, CancellationToken ct = default)
    {
        var email = Normalize(franchiseeEmail);
        var cards = await _db.Cards.AsNoTracking()
            .Where(c => c.FUserEmail != null && c.FUserEmail.ToLower() == email)
            .OrderByDescending(c => c.Id)
            .ToListAsync(ct);

        return ApiResult<IReadOnlyList<DigiCardListItemDto>>.Ok(cards.Select(MapListItem).ToList());
    }

    public async Task<ApiResult<DigiCardDetailDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var card = await _db.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (card == null)
            return ApiResult<DigiCardDetailDto>.Fail("Mini Website / card not found.");

        var meta = await _db.CardSlugHistories.AsNoTracking()
            .FirstOrDefaultAsync(m => m.CardId == id, ct);

        return ApiResult<DigiCardDetailDto>.Ok(MapDetail(card, meta));
    }

    public async Task<ApiResult<DigiCardSlugLookupDto>> ResolvePreviousSlugAsync(string previousSlug, CancellationToken ct = default)
    {
        var slug = previousSlug.Trim();
        if (string.IsNullOrWhiteSpace(slug))
            return ApiResult<DigiCardSlugLookupDto>.Fail("previousSlug is required.");

        var meta = await _db.CardSlugHistories.AsNoTracking()
            .FirstOrDefaultAsync(m => m.PreviousSlug == slug, ct);
        if (meta == null)
            return ApiResult<DigiCardSlugLookupDto>.Fail("Slug not found.");

        var card = await _db.Cards.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == meta.CardId, ct);

        return ApiResult<DigiCardSlugLookupDto>.Ok(new DigiCardSlugLookupDto(
            meta.CardId,
            meta.PreviousSlug,
            card?.CardId));
    }

    public async Task<ApiResult<CustomerDashboardDto>> GetCustomerDashboardAsync(string email, CancellationToken ct = default)
    {
        email = Normalize(email);
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);
        if (user == null)
            return ApiResult<CustomerDashboardDto>.Fail("User not found.");

        if (user.Role is not (UserRole.Customer or UserRole.Team))
            return ApiResult<CustomerDashboardDto>.Fail("Use franchisee dashboard for this role.");

        var referralCode = user.ReferralCode;
        if (string.IsNullOrWhiteSpace(referralCode))
        {
            referralCode = Convert.ToHexString(System.Security.Cryptography.MD5.HashData(
                System.Text.Encoding.UTF8.GetBytes(email + DateTime.Now.Ticks)))[..8];
            var tracked = await _db.Users.FirstOrDefaultAsync(u => u.Id == user.Id, ct);
            if (tracked != null)
            {
                tracked.ReferralCode = referralCode;
                tracked.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(ct);
            }
        }

        var cards = await _db.Cards.AsNoTracking()
            .Where(c => c.UserEmail != null && c.UserEmail.ToLower() == email)
            .OrderByDescending(c => c.Id)
            .ToListAsync(ct);

        var showInvoice = cards.Any(c => string.IsNullOrWhiteSpace(c.FUserEmail));

        return ApiResult<CustomerDashboardDto>.Ok(new CustomerDashboardDto(
            user.Email,
            user.Role == UserRole.Team ? "TEAM" : "CUSTOMER",
            referralCode!,
            user.RefundStatus,
            user.RefundStatusDate,
            showInvoice,
            user.MwReferralId,
            user.Influencer ? "YES" : "NO",
            cards.Select(MapListItem).ToList()));
    }

    public async Task<ApiResult<FranchiseeDashboardDto>> GetFranchiseeDashboardAsync(string email, CancellationToken ct = default)
    {
        email = Normalize(email);
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);
        if (user == null)
            return ApiResult<FranchiseeDashboardDto>.Fail("User not found.");
        if (user.Role != UserRole.Franchisee)
            return ApiResult<FranchiseeDashboardDto>.Fail("User is not a franchisee.");

        var totalCards = await _db.Cards.AsNoTracking()
            .CountAsync(c => c.FUserEmail != null && c.FUserEmail.ToLower() == email, ct);

        var walletBalance = await _wallet.GetLatestBalanceAsync(email, ct);

        // Same join as PHP manage-users query
        var managed = await (
            from ud in _db.Users.AsNoTracking()
            join dc in _db.Cards.AsNoTracking()
                on ud.Email equals dc.UserEmail
            where ud.Role == UserRole.Customer
                  && dc.FUserEmail != null
                  && dc.FUserEmail.ToLower() == email
            orderby ud.CreatedAt descending
            select new { ud, dc }
        ).ToListAsync(ct);

        var rows = managed.Select(x => new FranchiseeManagedUserDto(
            x.ud.Id,
            x.ud.Name,
            x.ud.Phone,
            x.ud.Email,
            x.ud.CreatedAt,
            x.ud.ReferralCode,
            x.ud.ReferredBy,
            x.dc.Id,
            x.dc.DCompName,
            x.dc.DPaymentStatus,
            x.dc.UploadedDate,
            x.dc.DPaymentDate,
            x.dc.ValidityDate,
            FormatValidity(x.dc)
        )).ToList();

        return ApiResult<FranchiseeDashboardDto>.Ok(new FranchiseeDashboardDto(
            user.Email,
            "FRANCHISEE",
            totalCards,
            walletBalance,
            walletBalance >= 413m,
            rows));
    }

    private DigiCardListItemDto MapListItem(Card c) =>
        new(
            c.Id,
            c.CardId,
            c.DCompName,
            c.UserEmail,
            c.FUserEmail,
            c.DPaymentStatus,
            c.DCardStatus,
            c.UploadedDate,
            c.DPaymentDate,
            c.ValidityDate,
            FormatValidity(c),
            BuildPublicUrl(c.CardId));

    private DigiCardDetailDto MapDetail(Card c, CardSlugHistory? meta) =>
        new(
            c.Id,
            c.CardId,
            c.DCompName,
            c.DDisplayName,
            c.UserEmail,
            c.FUserEmail,
            c.DFName,
            c.DLName,
            c.DPosition,
            c.DContact,
            c.DContact2,
            c.DWhatsapp,
            c.DEmail,
            c.DWebsite,
            c.DAddress,
            c.DAddress2,
            c.DCity,
            c.DState,
            c.DPincode,
            c.DCountry,
            c.DAboutUs,
            c.DNatureOfBusiness,
            c.DSpeciality,
            c.DPaymentStatus,
            c.DCardStatus,
            c.DLogoLocation,
            c.DHeroImageLocation,
            c.UploadedDate,
            c.DPaymentDate,
            c.ValidityDate,
            c.DGstNumber,
            c.DBusinessHours,
            meta?.PreviousSlug,
            c.BusinessProfileType ?? "",
            c.BusinessType ?? "",
            c.BusinessOperationArea ?? "",
            c.BusinessOperationLocations);

    private string BuildPublicUrl(string? cardSlug)
    {
        if (string.IsNullOrWhiteSpace(cardSlug)) return "";
        return $"https://{_app.PublicHost}/{cardSlug.Trim()}";
    }

    private static string FormatValidity(Card row)
    {
        if (row.ValidityDate is { } v && v.Year > 1)
            return v.ToString("dd-MM-yyyy");

        if (string.Equals(row.DPaymentStatus, "Success", StringComparison.OrdinalIgnoreCase)
            && row.DPaymentDate is { } pay)
            return pay.AddYears(1).ToString("dd-MM-yyyy");

        if (row.UploadedDate is { } up)
            return up.AddDays(7).ToString("dd-MM-yyyy");

        return "-";
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
