using Microsoft.EntityFrameworkCore;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<RegistrationPending> RegistrationPendings { get; }
    DbSet<ReferralEarning> ReferralEarnings { get; }
    DbSet<ReferralPayment> ReferralPayments { get; }
    DbSet<Deal> Deals { get; }
    DbSet<DealCustomerMapping> DealCustomerMappings { get; }
    DbSet<Card> Cards { get; }
    DbSet<CardSlugHistory> CardSlugHistories { get; }
    DbSet<CardProduct> CardProducts { get; }
    DbSet<CardService> CardServices { get; }
    DbSet<CardGalleryImage> CardGallery { get; }
    DbSet<CardOffer> CardOffers { get; }
    DbSet<CardVideo> CardVideos { get; }
    DbSet<PricingPlan> PricingPlans { get; }
    DbSet<JoiningDeal> JoiningDeals { get; }
    DbSet<UserJoiningDeal> UserJoiningDeals { get; }
    DbSet<Wallet> Wallets { get; }
    DbSet<WalletTransaction> WalletTransactions { get; }
    DbSet<Order> Orders { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<FranchisePayment> FranchisePayments { get; }
    DbSet<Customer> Customers { get; }
    DbSet<CustomerFollowup> CustomerFollowups { get; }
    DbSet<ContentPage> ContentPages { get; }
    DbSet<Faq> Faqs { get; }
    DbSet<DocSection> DocSections { get; }
    DbSet<DocPage> DocPages { get; }
    DbSet<DocMedia> DocMedia { get; }
    DbSet<ThemeCatalog> ThemeCatalog { get; }
    DbSet<CardView> CardViews { get; }
    DbSet<EmailLog> EmailLogs { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<KitFolder> KitFolders { get; }
    DbSet<KitItem> KitItems { get; }
    DbSet<Role> Roles { get; }
    DbSet<RoleAccessProfile> RoleAccessProfiles { get; }
    DbSet<RoleAccessFeature> RoleAccessFeatures { get; }
    DbSet<RoleAccessSetting> RoleAccessSettings { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<UserCustomCategory> UserCustomCategories { get; }
    DbSet<UserBankDetails> UserBankDetails { get; }
    DbSet<FranchiseVerification> FranchiseVerifications { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
