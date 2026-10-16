using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RegistrationPending> RegistrationPendings => Set<RegistrationPending>();
    public DbSet<ReferralEarning> ReferralEarnings => Set<ReferralEarning>();
    public DbSet<ReferralPayment> ReferralPayments => Set<ReferralPayment>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<DealCustomerMapping> DealCustomerMappings => Set<DealCustomerMapping>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardSlugHistory> CardSlugHistories => Set<CardSlugHistory>();
    public DbSet<CardProduct> CardProducts => Set<CardProduct>();
    public DbSet<CardService> CardServices => Set<CardService>();
    public DbSet<CardGalleryImage> CardGallery => Set<CardGalleryImage>();
    public DbSet<CardOffer> CardOffers => Set<CardOffer>();
    public DbSet<CardVideo> CardVideos => Set<CardVideo>();
    public DbSet<PricingPlan> PricingPlans => Set<PricingPlan>();
    public DbSet<PlanCommission> PlanCommissions => Set<PlanCommission>();
    public DbSet<JoiningDeal> JoiningDeals => Set<JoiningDeal>();
    public DbSet<UserJoiningDeal> UserJoiningDeals => Set<UserJoiningDeal>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<FranchisePayment> FranchisePayments => Set<FranchisePayment>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerFollowup> CustomerFollowups => Set<CustomerFollowup>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<DocSection> DocSections => Set<DocSection>();
    public DbSet<DocPage> DocPages => Set<DocPage>();
    public DbSet<DocMedia> DocMedia => Set<DocMedia>();
    public DbSet<ThemeCatalog> ThemeCatalog => Set<ThemeCatalog>();
    public DbSet<CardView> CardViews => Set<CardView>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<KitFolder> KitFolders => Set<KitFolder>();
    public DbSet<KitItem> KitItems => Set<KitItem>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<UserCustomCategory> UserCustomCategories => Set<UserCustomCategory>();
    public DbSet<UserBankDetails> UserBankDetails => Set<UserBankDetails>();
    public DbSet<FranchiseVerification> FranchiseVerifications => Set<FranchiseVerification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await AssignUserCodesAsync(cancellationToken);
        await AssignOrderCodesAsync(cancellationToken);

        for (var attempt = 1; ; attempt++)
        {
            var added = ChangeTracker.Entries<ReferralEarning>()
                .Where(e => e.State == EntityState.Added && string.IsNullOrEmpty(e.Entity.CommissionCode))
                .Select(e => e.Entity)
                .ToList();
            await AssignCommissionCodesAsync(added, cancellationToken);
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (attempt < 3 && added.Count > 0 && IsCommissionCodeConflict(ex))
            {
                foreach (var earning in added)
                    earning.CommissionCode = null;
            }
        }
    }

    private static bool IsCommissionCodeConflict(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("IX_referral_earnings_CommissionCode", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Numbers commissions per Indian financial year (April–March) of the referral date:
    /// <c>COM-26-27-00000001</c>, <c>COM-26-27-00000002</c>, …
    /// </summary>
    internal async Task AssignCommissionCodesAsync(IEnumerable<ReferralEarning> earnings, CancellationToken ct)
    {
        var pending = earnings
            .Where(e => string.IsNullOrEmpty(e.CommissionCode))
            .OrderBy(e => e.ReferralDate)
            .ThenBy(e => e.Id)
            .ToList();
        if (pending.Count == 0) return;

        var nextByPrefix = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var earning in pending)
        {
            var date = earning.ReferralDate.Year > 1 ? earning.ReferralDate : AppClock.Now;
            var startYear = date.Month >= 4 ? date.Year : date.Year - 1;
            var prefix = $"COM-{startYear % 100:D2}-{(startYear + 1) % 100:D2}-";

            if (!nextByPrefix.TryGetValue(prefix, out var next))
            {
                var latest = await ReferralEarnings.AsNoTracking()
                    .Where(r => r.CommissionCode != null && r.CommissionCode.StartsWith(prefix))
                    .OrderByDescending(r => r.CommissionCode)
                    .Select(r => r.CommissionCode!)
                    .FirstOrDefaultAsync(ct);
                next = (latest != null && int.TryParse(latest[prefix.Length..], out var n) ? n : 0) + 1;
            }

            earning.CommissionCode = $"{prefix}{next:D8}";
            nextByPrefix[prefix] = next + 1;
        }
    }

    private async Task AssignOrderCodesAsync(CancellationToken ct)
    {
        var pending = ChangeTracker.Entries<Invoice>()
            .Where(e => e.State == EntityState.Added && string.IsNullOrEmpty(e.Entity.OrderCode))
            .Select(e => e.Entity)
            .ToList();
        if (pending.Count == 0) return;

        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var invoice in pending)
        {
            string code;
            do code = UserCodeGenerator.Generate(UserCodeGenerator.OrderPrefix);
            while (!claimed.Add(code) || await Invoices.AnyAsync(i => i.OrderCode == code, ct));
            invoice.OrderCode = code;
        }
    }

    private async Task AssignUserCodesAsync(CancellationToken ct)
    {
        var pending = ChangeTracker.Entries<User>()
            .Where(e => e.State == EntityState.Added && string.IsNullOrEmpty(e.Entity.UserCode))
            .Select(e => e.Entity)
            .ToList();
        if (pending.Count == 0) return;

        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var user in pending)
        {
            var prefix = user.AssignedRole?.Prefix
                ?? await Roles.AsNoTracking()
                    .Where(r => r.RoleId == user.RoleId)
                    .Select(r => r.Prefix)
                    .FirstOrDefaultAsync(ct);

            string code;
            do code = UserCodeGenerator.Generate(prefix);
            while (!claimed.Add(code)
                   || await Users.IgnoreQueryFilters().AnyAsync(u => u.UserCode == code, ct));
            user.UserCode = code;
        }
    }

}
