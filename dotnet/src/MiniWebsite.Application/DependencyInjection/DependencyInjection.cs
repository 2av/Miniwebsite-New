using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MiniWebsite.Application.Admin.EmailTemplates;
using MiniWebsite.Application.Admin.AllOrders;
using MiniWebsite.Application.Admin.FranchiseDistributors;
using MiniWebsite.Application.Admin.Franchisees;
using MiniWebsite.Application.Admin.Invoices;
using MiniWebsite.Application.Admin.ManageCards;
using MiniWebsite.Application.Admin.ManageUsers;
using MiniWebsite.Application.Admin.ManageCategories;
using MiniWebsite.Application.Admin.ManageContent;
using MiniWebsite.Application.Admin.ManageFaqs;
using MiniWebsite.Application.Admin.ManageTeams;
using MiniWebsite.Application.Admin.GrowWithMw;
using MiniWebsite.Application.Admin.KitManagement;
using MiniWebsite.Application.Admin.RoleManagement;
using MiniWebsite.Application.Admin.SalesChannelPartners;
using MiniWebsite.Application.Admin.Coupons;
using MiniWebsite.Application.Admin.ManagePricing;
using MiniWebsite.Application.Admin.PlanCommissions;
using MiniWebsite.Application.Admin.ManageReferrals;
using MiniWebsite.Application.Admin.WalletRecharge;
using MiniWebsite.Application.Admin.UserDeletions;
using MiniWebsite.Application.Auth;
using MiniWebsite.Application.DigiCards;
using MiniWebsite.Application.Registration;
using MiniWebsite.Application.Users;
using MiniWebsite.Application.Website;
using MiniWebsite.Application.PublicSite;
using MiniWebsite.Application.Portal;
using System.Reflection;

namespace MiniWebsite.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IDigiCardService, DigiCardService>();
        services.AddScoped<IWebsiteService, WebsiteService>();
        services.AddScoped<IAdminManageUsersService, AdminManageUsersService>();
        services.AddScoped<IAdminManageCardsService, AdminManageCardsService>();
        services.AddScoped<IAdminFranchiseDistributorsService, AdminFranchiseDistributorsService>();
        services.AddScoped<IAdminFranchiseesService, AdminFranchiseesService>();
        services.AddScoped<IAdminAllOrdersService, AdminAllOrdersService>();
        services.AddScoped<IAdminInvoiceAccessService, AdminInvoiceAccessService>();
        services.AddScoped<IAdminUserDeletionsService, AdminUserDeletionsService>();
        services.AddScoped<IAdminManageReferralsService, AdminManageReferralsService>();
        services.AddScoped<IAdminCouponsService, AdminCouponsService>();
        services.AddScoped<IAdminManagePricingService, AdminManagePricingService>();
        services.AddScoped<IAdminPlanCommissionsService, AdminPlanCommissionsService>();
        services.AddScoped<IAdminEmailTemplatesService, AdminEmailTemplatesService>();
        services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
        services.AddScoped<IAdminWalletRechargeService, AdminWalletRechargeService>();
        services.AddScoped<IAdminManageFaqsService, AdminManageFaqsService>();
        services.AddScoped<IAdminManageContentService, AdminManageContentService>();
        services.AddScoped<IAdminManageCategoriesService, AdminManageCategoriesService>();
        services.AddScoped<IAdminManageTeamsService, AdminManageTeamsService>();
        services.AddScoped<IAdminGrowWithMwService, AdminGrowWithMwService>();
        services.AddScoped<IAdminKitManagementService, AdminKitManagementService>();
        services.AddScoped<IAdminRoleManagementService, AdminRoleManagementService>();
        services.AddScoped<IAdminSalesChannelPartnersService, AdminSalesChannelPartnersService>();
        services.AddScoped<IPublicSiteService, PublicSiteService>();
        services.AddScoped<IPublicCardService, PublicCardService>();
        services.AddScoped<IPortalService, PortalService>();
        return services;
    }
}
