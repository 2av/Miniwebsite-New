using Microsoft.EntityFrameworkCore;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Domain.Entities;
using MiniWebsite.Domain.Enums;

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
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<DealCustomerMapping> DealCustomerMappings => Set<DealCustomerMapping>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardSlugHistory> CardSlugHistories => Set<CardSlugHistory>();
    public DbSet<CardProduct> CardProducts => Set<CardProduct>();
    public DbSet<CardService> CardServices => Set<CardService>();
    public DbSet<CardGalleryImage> CardGallery => Set<CardGalleryImage>();
    public DbSet<CardOffer> CardOffers => Set<CardOffer>();
    public DbSet<CardVideo> CardVideos => Set<CardVideo>();
    public DbSet<PricingPlan> PricingPlans => Set<PricingPlan>();
    public DbSet<JoiningDeal> JoiningDeals => Set<JoiningDeal>();
    public DbSet<UserJoiningDeal> UserJoiningDeals => Set<UserJoiningDeal>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
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
    public DbSet<RoleAccessProfile> RoleAccessProfiles => Set<RoleAccessProfile>();
    public DbSet<RoleAccessFeature> RoleAccessFeatures => Set<RoleAccessFeature>();
    public DbSet<RoleAccessSetting> RoleAccessSettings => Set<RoleAccessSetting>();
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
        await AssignDefaultRoleIdsAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Users created through flows that only know the legacy role (registration, team/franchisee admin)
    /// still get a RoleId so every user maps to the roles table.
    /// </summary>
    private async Task AssignDefaultRoleIdsAsync(CancellationToken ct)
    {
        var pending = ChangeTracker.Entries<User>()
            .Where(e => e.State == EntityState.Added && e.Entity.RoleId == null)
            .Select(e => e.Entity)
            .ToList();
        if (pending.Count == 0) return;

        var rolesByPrefix = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in await Roles.AsNoTracking().Where(r => r.Prefix != null).OrderBy(r => r.RoleId).ToListAsync(ct))
            rolesByPrefix.TryAdd(r.Prefix!, r.RoleId);

        foreach (var user in pending)
        {
            var prefix = DefaultRolePrefix(user.Role);
            if (prefix != null && rolesByPrefix.TryGetValue(prefix, out var roleId))
                user.RoleId = roleId;
        }
    }

    private static string? DefaultRolePrefix(UserRole role) => role switch
    {
        UserRole.Customer => "SBU",
        UserRole.Franchisee => "FRP",
        UserRole.Team => "DMP",
        _ => null
    };
}
