using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Admin.EmailTemplates;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Portal;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Admin.Onboarding;

public record ReviewDocumentsRequest(string Decision, string? Remarks);

public interface IDocumentReviewService
{
    Task<ApiResult> ReviewAsync(int userId, string reviewerEmail, ReviewDocumentsRequest request, CancellationToken ct = default);
}

public class DocumentReviewService : IDocumentReviewService
{
    private static readonly string[] Decisions = ["Approved", "Resubmission Required", "Rejected"];

    private readonly IApplicationDbContext _db;
    private readonly ILibraryEmailSender _mail;

    public DocumentReviewService(IApplicationDbContext db, ILibraryEmailSender mail)
    {
        _db = db;
        _mail = mail;
    }

    public async Task<ApiResult> ReviewAsync(
        int userId, string reviewerEmail, ReviewDocumentsRequest request, CancellationToken ct = default)
    {
        var decision = (request.Decision ?? "").Trim();
        if (!Decisions.Contains(decision, StringComparer.OrdinalIgnoreCase))
            return ApiResult.Fail("Choose Approved, Resubmission Required, or Rejected.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null) return ApiResult.Fail("User not found.");

        var row = await _db.FranchiseVerifications.FirstOrDefaultAsync(v => v.UserId == user.Id, ct);
        if (row == null || string.Equals(row.Status, "pending", StringComparison.OrdinalIgnoreCase))
            return ApiResult.Fail("This user has not submitted documents.");

        var now = AppClock.Now;
        row.Status = decision.ToLowerInvariant() switch
        {
            "approved" => "approved",
            "rejected" => "rejected",
            _ => "resubmission",
        };
        row.AdminRemarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();
        row.ReviewedAt = now;
        row.ReviewedBy = string.IsNullOrWhiteSpace(reviewerEmail) ? "SB team" : reviewerEmail.Trim();
        row.UpdatedAt = now;
        OnboardingAccess.ApplyDocumentDecision(user, decision, now);
        await _db.SaveChangesAsync(ct);
        await NotifyDocumentDecisionAsync(user, row, decision, now, ct);
        return ApiResult.Ok("Document status updated to " + decision + ".");
    }

    private async Task NotifyDocumentDecisionAsync(
        User user, FranchiseVerification row, string decision, DateTime now, CancellationToken ct)
    {
        var firstName = string.IsNullOrWhiteSpace(user.FirstName) ? "there" : user.FirstName;
        var link = _mail.Page("verification");
        if (decision.Equals("Approved", StringComparison.OrdinalIgnoreCase))
        {
            await _mail.TrySendAsync(EmailLibraryCatalog.DocumentsVerified, user.Email, new Dictionary<string, string?>
            {
                ["first_name"] = firstName,
                ["reference_id"] = row.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["approval_date"] = EmailLibraryMessages.Date(now),
                ["dashboard_link"] = link,
            }, ct: ct);
            return;
        }

        if (!decision.Equals("Resubmission Required", StringComparison.OrdinalIgnoreCase))
            return;

        var remarks = string.IsNullOrWhiteSpace(row.AdminRemarks)
            ? "The submitted information needs correction before verification can be completed."
            : row.AdminRemarks;
        await _mail.TrySendAsync(EmailLibraryCatalog.DocumentResubmission, user.Email, new Dictionary<string, string?>
        {
            ["first_name"] = firstName,
            ["document_name"] = "Submitted verification documents",
            ["reason"] = remarks,
            ["required_correction"] = remarks,
            ["dashboard_link"] = link,
            ["reference_id"] = row.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
        }, ct: ct);
    }
}
