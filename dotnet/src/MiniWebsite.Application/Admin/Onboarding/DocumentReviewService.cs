using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Application.Portal;
using MiniWebsite.Domain.Common;

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

    public DocumentReviewService(IApplicationDbContext db)
    {
        _db = db;
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
        return ApiResult.Ok("Document status updated to " + decision + ".");
    }
}
