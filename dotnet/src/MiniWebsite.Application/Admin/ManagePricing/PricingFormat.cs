using System.Globalization;

namespace MiniWebsite.Application.Admin.ManagePricing;

/// <summary>Display helpers shared by Manage Pricing and Plan Commissions.</summary>
public static class PricingFormat
{
    public static decimal RoundMoney(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    public static string Money(decimal v) =>
        decimal.Truncate(v) == v
            ? "₹" + v.ToString("N0", CultureInfo.InvariantCulture)
            : "₹" + v.ToString("N2", CultureInfo.InvariantCulture);

    public static string? Money(decimal? v) => v is { } d ? Money(d) : null;

    public static string Validity(int? months) => months switch
    {
        null => "Lifetime",
        var m when m % 12 == 0 => m / 12 == 1 ? "1 Year" : $"{m / 12} Years",
        1 => "1 Month",
        var m => $"{m} Months",
    };

    public static string? Timestamp(DateTime? dt)
    {
        if (dt is null || dt.Value.Year < 1) return null;
        return dt.Value.ToString("dd MMM yyyy, hh:mm tt", CultureInfo.InvariantCulture);
    }
}
