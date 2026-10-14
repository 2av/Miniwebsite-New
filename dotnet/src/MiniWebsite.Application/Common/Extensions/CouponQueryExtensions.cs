using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Common.Extensions;

public static class CouponQueryExtensions
{
    /// <summary>Coupons whose Plan Type is a role with the given prefix (e.g. <see cref="RolePrefixes.ShareBizzUser"/>).</summary>
    public static IQueryable<Coupon> ForRolePrefix(this IQueryable<Coupon> query, string rolePrefix) =>
        query.Where(c => c.Role != null && c.Role.Prefix == rolePrefix);

    /// <summary>Active coupons whose validity date has not passed.</summary>
    public static IQueryable<Coupon> Usable(this IQueryable<Coupon> query)
    {
        var today = AppClock.Today;
        return query.Where(c =>
            c.Status == CouponValues.StatusActive
            && (c.ValidityDate == null || c.ValidityDate >= today));
    }
}
