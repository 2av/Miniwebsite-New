namespace MiniWebsite.Application.Common.Extensions;

public static class ReferralOrderMatching
{
    /// <summary>
    /// Order a commission belongs to: its linked invoice, otherwise the referred user's invoice dated closest
    /// to the referral (lowest id on ties). Manage Referrals and All Orders must agree on this.
    /// </summary>
    public static int? ResolveInvoiceId(
        int? linkedInvoiceId, DateTime referralDate, IEnumerable<(int Id, DateTime InvoiceDate)> referredUserInvoices) =>
        linkedInvoiceId ?? referredUserInvoices
            .OrderBy(i => Math.Abs((i.InvoiceDate - referralDate).TotalDays))
            .ThenBy(i => i.Id)
            .Select(i => (int?)i.Id)
            .FirstOrDefault();
}
