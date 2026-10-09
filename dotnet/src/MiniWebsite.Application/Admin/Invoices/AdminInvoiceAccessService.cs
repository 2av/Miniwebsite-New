using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Application.Common.Models;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Admin.Invoices;

public class AdminInvoiceAccessService : IAdminInvoiceAccessService
{
    private readonly IApplicationDbContext _db;

    public AdminInvoiceAccessService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult<InvoiceHtmlResult>> GetInvoiceHtmlByIdAsync(int invoiceId, CancellationToken ct = default)
    {
        if (invoiceId <= 0)
            return ApiResult<InvoiceHtmlResult>.Fail("Invalid invoice id.");

        var row = await LoadInvoiceAsync(invoiceId, ct);
        if (row == null)
            return ApiResult<InvoiceHtmlResult>.Fail("Invoice not found.");

        return ApiResult<InvoiceHtmlResult>.Ok(new InvoiceHtmlResult(
            BuildHtml(row),
            SanitizeFileName(row.InvoiceNumber) + ".html"));
    }

    public async Task<ApiResult<InvoiceHtmlResult>> GetInvoiceHtmlByCardIdAsync(int cardId, CancellationToken ct = default)
    {
        if (cardId <= 0)
            return ApiResult<InvoiceHtmlResult>.Fail("Invalid card id.");

        var invoiceId = await FindLatestInvoiceIdForCardAsync(cardId, ct);
        if (invoiceId is null)
            return ApiResult<InvoiceHtmlResult>.Fail("Invoice not found for this miniwebsite.");

        return await GetInvoiceHtmlByIdAsync(invoiceId.Value, ct);
    }

    private async Task<Invoice?> LoadInvoiceAsync(int invoiceId, CancellationToken ct)
    {
        try
        {
            return await _db.Invoices.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        }
        catch
        {
            return null;
        }
    }

    private async Task<int?> FindLatestInvoiceIdForCardAsync(int cardId, CancellationToken ct)
    {
        try
        {
            return await _db.Invoices.AsNoTracking()
                .Where(i => i.CardId == cardId)
                .OrderByDescending(i => i.Id)
                .Select(i => (int?)i.Id)
                .FirstOrDefaultAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildHtml(Invoice inv)
    {
        var original = inv.OriginalAmount;
        var promo = inv.PromoDiscount;
        var subTotal = inv.SubTotal;
        var igst = inv.IgstAmount;
        var cgst = inv.CgstAmount;
        var sgst = inv.SgstAmount;
        var total = inv.TotalAmount;
        if (original <= 0) original = total;
        if (subTotal <= 0) subTotal = total > 0 ? Math.Round(total / 1.18m, 2) : 0;

        var invoiceDate = inv.InvoiceDate.Year > 1
            ? inv.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var description = ResolveServiceDescription(inv);
        var qty = inv.Quantity <= 0 ? "1" : inv.Quantity.ToString(CultureInfo.InvariantCulture);
        var hsn = string.IsNullOrWhiteSpace(inv.HsnSacCode) ? "998313" : inv.HsnSacCode.Trim();
        var amountWords = NumberToWords(total) + " Only";

        string F(decimal v) => v.ToString("N2", CultureInfo.CreateSpecificCulture("en-IN"));
        string H(string? v) => WebUtility.HtmlEncode(v ?? "");

        var taxRows = new StringBuilder();
        taxRows.Append("<tr><td>Original Amount:</td><td>₹").Append(F(original)).Append("</td></tr>");
        if (promo > 0)
            taxRows.Append("<tr><td>Promo Discount:</td><td>-₹").Append(F(promo)).Append("</td></tr>");
        taxRows.Append("<tr><td>Sub Total:</td><td>₹").Append(F(subTotal)).Append("</td></tr>");
        if (igst > 0)
            taxRows.Append("<tr><td>IGST (18%):</td><td>₹").Append(F(igst)).Append("</td></tr>");
        if (cgst > 0)
            taxRows.Append("<tr><td>CGST (9%):</td><td>₹").Append(F(cgst)).Append("</td></tr>");
        if (sgst > 0)
            taxRows.Append("<tr><td>SGST (9%):</td><td>₹").Append(F(sgst)).Append("</td></tr>");
        taxRows.Append("<tr class=\"total-row\"><td><strong>Total:</strong></td><td><strong>₹")
            .Append(F(total)).Append("</strong></td></tr>");

        return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset=""UTF-8"">
  <title>Tax Invoice #{H(inv.InvoiceNumber)}</title>
  <style>
    * {{ box-sizing: border-box; margin: 0; padding: 0; }}
    html, body {{ width: 100%; font-family: Arial, sans-serif; color: #333; font-size: 12px; background: #f5f5f5; }}
    @page {{ size: A4; margin: 20mm 15mm; }}
    .container {{ width: 100%; max-width: 800px; margin: 0 auto; padding: 20px; background: #fff; }}
    table {{ table-layout: fixed; width: 100%; }}
    .print-button {{ display: block; margin: 10px auto 20px; padding: 8px 16px; background: #4CAF50; color: #fff; border: none; border-radius: 4px; cursor: pointer; font-size: 14px; font-weight: bold; }}
    @media print {{ .print-button {{ display: none; }} body {{ background: #fff; }} }}
    .invoice-title {{ text-align: center; font-size: 16px; font-weight: bold; margin: 15px 0; border-bottom: 1px solid #ccc; padding-bottom: 5px; }}
    .header-info td {{ padding: 5px; }}
    .details-container {{ width: 100%; display: table; margin-bottom: 15px; }}
    .customer-details, .company-details {{ border: 1px solid #ccc; padding: 10px; vertical-align: top; display: table-cell; width: 50%; }}
    .details-title {{ font-weight: bold; margin-bottom: 10px; }}
    .items-table {{ width: 100%; border-collapse: collapse; margin-bottom: 15px; }}
    .items-table th, .items-table td {{ border: 1px solid #ccc; padding: 6px; text-align: center; }}
    .items-table th {{ background: #ffbf00; color: #000; font-weight: bold; }}
    .terms {{ font-size: 12px; float: left; width: 60%; }}
    .tax-summary {{ width: 40%; float: right; }}
    .tax-summary td {{ padding: 3px; text-align: right; }}
    .tax-summary .total-row {{ font-weight: bold; }}
    .thank-you-section {{ text-align: center; margin: 20px 0; clear: both; padding-top: 20px; }}
    .thank-you-section p {{ font-weight: bold; font-size: 14px; margin: 5px 0; }}
    .footer {{ text-align: center; margin-top: 30px; color: #777; clear: both; }}
  </style>
  <script>function printInvoice(){{ window.print(); }}</script>
</head>
<body>
  <button class=""print-button"" onclick=""printInvoice()"">Print This Page</button>
  <div class=""container"">
    <div class=""invoice-title"">TAX INVOICE</div>
    <div class=""header-info"">
      <table>
        <tr>
          <td><strong>Date:</strong> {invoiceDate}</td>
          <td style=""text-align:right;""><strong>Invoice No:</strong> {H(inv.InvoiceNumber)}</td>
        </tr>
      </table>
    </div>
    <div class=""details-container"">
      <div class=""customer-details"">
        <div class=""details-title"">CUSTOMER DETAILS</div>
        <div><strong>Billing Name:</strong> {H(inv.BillingName)}</div>
        <div><strong>Address:</strong> {H(inv.BillingAddress)}</div>
        <div><strong>Contact:</strong> {H(inv.BillingContact)}</div>
        <div><strong>GST No:</strong> {(string.IsNullOrWhiteSpace(inv.BillingGstNumber) ? "Not Provided" : H(inv.BillingGstNumber))}</div>
      </div>
      <div class=""company-details"">
        <div class=""details-title"">COMPANY DETAILS</div>
        <div>KIROVA SOLUTIONS LLP</div>
        <div><strong>Address:</strong> plot no 535, 1st floor, block b, near madrasi mandir, sec 23, sanjay colony, Faridabad Sector 22, sector 23 police station, Faridabad, Faridabad- 121005, Haryana, India</div>
        <div><strong>Contact:</strong> +91 9429693061</div>
        <div><strong>Email Id:</strong> support@miniwebsite.in</div>
        <div><strong>PAN No:</strong> ABDFK4023D</div>
        <div><strong>GST No:</strong> 06ABDFK4023D1ZW</div>
      </div>
    </div>
    <table class=""items-table"">
      <thead>
        <tr>
          <th style=""width:10%;"">SR No.</th>
          <th>DESCRIPTION</th>
          <th style=""width:20%;"">HSN/SAC CODE</th>
          <th style=""width:10%;"">QTY.</th>
          <th style=""width:10%;"">PRICE</th>
          <th style=""width:10%;"">TOTAL</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td>1</td>
          <td>{H(description)}</td>
          <td>{H(hsn)}</td>
          <td>{H(qty)}</td>
          <td>₹{F(original)}</td>
          <td>₹{F(original)}</td>
        </tr>
      </tbody>
    </table>
    <div class=""terms"">
      <p><strong>FOR KIROVA SOLUTIONS LLP</strong></p><br/><br/>
      <p style=""font-size:10px;"">This is a computer generated invoice, hence<br>No signature is required.</p>
      <br/><br/>
      <p>(Authorised Signatory)</p>
      <br/><br/>
      <p><strong>Amount (in words):</strong> {H(amountWords)}</p>
      <div style=""margin-top:20px;"">
        <p><strong>COMPANY BANK DETAILS:</strong></p>
        <table>
          <tr><td>A/C Name</td><td>: KIROVA SOLUTIONS LLP</td></tr>
          <tr><td>Bank Name</td><td>: HDFC BANK</td></tr>
          <tr><td>Account No.</td><td>: 50200109163384</td></tr>
          <tr><td>IFSC CODE</td><td>: HDFC0000279</td></tr>
          <tr><td>Account type</td><td>: CURRENT ACCOUNT</td></tr>
        </table>
      </div>
    </div>
    <div class=""tax-summary"">
      <table>
        {taxRows}
      </table>
    </div>
    <div class=""thank-you-section"">
      <p>Thank you for your business!</p>
      <p>www.sharebizz.com</p>
    </div>
    <div class=""footer"">This is a computer generated invoice.</div>
  </div>
</body>
</html>";
    }

    private static string ResolveServiceDescription(Invoice inv)
    {
        var baseDesc = (inv.ServiceDescription ?? "").Trim();
        if (string.IsNullOrWhiteSpace(baseDesc))
            baseDesc = (inv.ServiceName ?? "").Trim();

        var service = (inv.ServiceName ?? "").ToLowerInvariant();
        var paymentType = (inv.PaymentType ?? "").ToLowerInvariant();
        var amountKey = (int)Math.Round(inv.OriginalAmount > 0 ? inv.OriginalAmount : inv.TotalAmount);
        var isFranchise = service.Contains("franchise") || paymentType == "franchisee";

        string plan;
        string validity;
        if (isFranchise)
        {
            (plan, validity) = amountKey switch
            {
                6000 => ("Starter Franchise Plan", "4 Months"),
                30000 => ("Full Franchise Plan", "Lifetime"),
                _ => ("Franchise Plan", "As per plan")
            };
        }
        else
        {
            (plan, validity) = amountKey switch
            {
                500 => ("ShareBizz Plan", "6 Months"),
                847 => ("ShareBizz Plan", "1 Year"),
                1500 => ("ShareBizz Plan", "2 Years"),
                2100 => ("ShareBizz Plan", "3 Years"),
                _ => ("ShareBizz Plan", "As per plan")
            };
        }

        var fallback = $"{plan} ({validity})";
        if (baseDesc.Length == 0) return fallback;
        if (baseDesc.Contains('(') && baseDesc.Contains(')')) return baseDesc;
        return $"{baseDesc} - {fallback}";
    }

    private static string SanitizeFileName(string? invoiceNumber)
    {
        var raw = string.IsNullOrWhiteSpace(invoiceNumber) ? "invoice" : invoiceNumber.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            raw = raw.Replace(c, '-');
        return raw.Replace('/', '-');
    }

    private static string NumberToWords(decimal number)
    {
        if (number < 0) return "Negative " + NumberToWords(Math.Abs(number));
        var parts = number.ToString("0.00", CultureInfo.InvariantCulture).Split('.');
        var integerPart = int.Parse(parts[0], CultureInfo.InvariantCulture);
        var decimalPart = int.Parse(parts[1], CultureInfo.InvariantCulture);
        var rupees = ConvertIntegerToWords(integerPart) + (integerPart == 1 ? " Rupee" : " Rupees");
        if (decimalPart > 0)
            rupees += " and " + ConvertIntegerToWords(decimalPart) + " Paise";
        return rupees.Trim();
    }

    private static string ConvertIntegerToWords(int num)
    {
        if (num == 0) return "Zero";
        string[] ones =
        [
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
            "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
        ];
        string[] tens = ["", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"];

        if (num < 20) return ones[num];
        if (num < 100) return (tens[num / 10] + (num % 10 != 0 ? " " + ones[num % 10] : "")).Trim();
        if (num < 1000)
        {
            var rem = num % 100;
            return (ones[num / 100] + " Hundred" + (rem != 0 ? " " + ConvertIntegerToWords(rem) : "")).Trim();
        }
        if (num < 100000)
        {
            var rem = num % 1000;
            return (ConvertIntegerToWords(num / 1000) + " Thousand" + (rem != 0 ? " " + ConvertIntegerToWords(rem) : "")).Trim();
        }
        if (num < 10000000)
        {
            var rem = num % 100000;
            return (ConvertIntegerToWords(num / 100000) + " Lakh" + (rem != 0 ? " " + ConvertIntegerToWords(rem) : "")).Trim();
        }
        {
            var rem = num % 10000000;
            return (ConvertIntegerToWords(num / 10000000) + " Crore" + (rem != 0 ? " " + ConvertIntegerToWords(rem) : "")).Trim();
        }
    }
}
