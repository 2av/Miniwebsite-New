namespace MiniWebsite.Domain.Common;

/// <summary>
/// Business clock: all stored and displayed timestamps are India Standard Time, independent of the server's
/// time zone. Security token expiries (JWT, refresh, password reset, OTP) stay on <see cref="DateTime.UtcNow"/>.
/// </summary>
public static class AppClock
{
    private static readonly TimeSpan IndiaOffset = TimeSpan.FromMinutes(330);

    public static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow + IndiaOffset, DateTimeKind.Unspecified);

    public static DateTime Today => Now.Date;
}
