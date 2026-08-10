using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Infrastructure.Persistence;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}

public sealed class DataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(ApplicationDbContext db, ILogger<DataSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedThemesAsync(ct);
        await SeedPricingPlansAsync(ct);
        await SeedRoleAccessCatalogAsync(ct);
        await SeedEmailTemplatesAsync(ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Reference data seed completed.");
    }

    private async Task SeedEmailTemplatesAsync(CancellationToken ct)
    {
        if (await _db.EmailTemplates.AnyAsync(ct)) return;

        var now = DateTime.UtcNow;
        EmailTemplate T(string key, string name, string category, string subject, string body, string vars, string desc) =>
            new()
            {
                TemplateKey = key,
                Name = name,
                Category = category,
                Subject = subject,
                BodyHtml = body,
                AvailableVariables = vars,
                Description = desc,
                IsActive = true,
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now,
                UpdatedBy = "system",
            };

        _db.EmailTemplates.AddRange(
            T("registration_otp", "Registration OTP", "auth",
                "{{host}} - Your OTP for Registration",
                @"Hi {{name}},<br><br>Your OTP for registration on {{host}} is: <b>{{otp}}</b><br><br>This OTP is valid for 10 minutes.<br><br>Thanks,<br>{{host}} Team",
                "name,otp,host", "Sent when a user requests registration OTP."),

            T("customer_welcome", "Customer Welcome", "welcome",
                "Welcome to {{host}}",
                @"<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
  <p>Hi <strong>{{name}}</strong>,</p>
  <p>Thank you for registering on {{host}}.</p>
  <p>Your account has been created successfully!</p>
  <div style=""background:#f8f9fa;border:1px solid #dee2e6;border-radius:8px;padding:20px;margin:20px 0;"">
    <h3 style=""margin-top:0;"">Your Login Details</h3>
    <p><strong>Email ID:</strong> {{email}}</p>
    <p><strong>Password:</strong> {{password}}</p>
    <p><a href=""{{dashboard_url}}"">Click here to access your dashboard</a></p>
  </div>
  <p>You can now start creating your Mini Website from your dashboard.</p>
  <p>Thanks,<br>{{host}} Team</p>
</div>",
                "name,email,password,host,dashboard_url", "Welcome email after customer registration."),

            T("franchisee_welcome", "Franchisee Welcome (with payment)", "franchise",
                "Welcome to MiniWebsite.in – Your Franchise Account is Ready!",
                @"<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
  <p>Hi <strong>{{name}}</strong>,</p>
  <p>Thank you for registering as a franchise with MiniWebsite.in.</p>
  <p>Your franchise account has been successfully created.</p>
  <div style=""background:#f8f9fa;border:1px solid #dee2e6;border-radius:8px;padding:20px;margin:20px 0;"">
    <h3 style=""margin-top:0;"">Your Login Details</h3>
    <p><strong>Email ID:</strong> {{email}}</p>
    <p><strong>Password:</strong> {{password}}</p>
    <p><a href=""{{login_url}}"">Click here to login</a></p>
  </div>
  <p><strong>1. Pay the One-Time Franchise Fee</strong><br>Amount: ₹30,000 + 18% GST = ₹35,400<br><a href=""{{payment_url}}"">Click to Pay</a></p>
  <p><strong>2.</strong> Complete document verification from your Dashboard.</p>
  <p><strong>3.</strong> After verification, access your Marketing Kit from the dashboard.</p>
  <p>Best regards,<br>Team MiniWebsite.in</p>
</div>",
                "name,email,password,login_url,payment_url", "Franchisee welcome including payment step."),

            T("franchisee_welcome_no_payment", "Franchisee Welcome (no payment)", "franchise",
                "Welcome to MiniWebsite.in – Your Franchise Account is Ready!",
                @"<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
  <p>Hi <strong>{{name}}</strong>,</p>
  <p>Thank you for registering as a franchise with MiniWebsite.in.</p>
  <div style=""background:#f8f9fa;border:1px solid #dee2e6;border-radius:8px;padding:20px;margin:20px 0;"">
    <h3 style=""margin-top:0;"">Your Login Details</h3>
    <p><strong>Email ID:</strong> {{email}}</p>
    <p><strong>Password:</strong> {{password}}</p>
    <p><a href=""{{login_url}}"">Click here to login</a></p>
  </div>
  <p><strong>1.</strong> Complete document verification from your Dashboard.</p>
  <p><strong>2.</strong> After verification, access your Marketing Kit from the dashboard.</p>
  <p>Best regards,<br>Team MiniWebsite.in</p>
</div>",
                "name,email,password,login_url", "Franchisee welcome without payment step."),

            T("password_reset", "Password Reset OTP", "auth",
                "Password reset code",
                @"Hi {{name}},<br><br>Your password reset code is: <b>{{otp}}</b><br><br>This code is valid for a limited time.<br><br>If you did not request this, please ignore this email.<br><br>Thanks,<br>{{host}} Team",
                "name,otp,host", "Forgot-password OTP email."),

            T("team_member_welcome", "Team Member Welcome", "team",
                "Welcome to MiniWebsite Team",
                @"<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
  <p>Hi <strong>{{name}}</strong>,</p>
  <p>Your team account has been created on MiniWebsite.</p>
  <p><strong>Email:</strong> {{email}}<br><strong>Password:</strong> {{password}}</p>
  <p><a href=""{{login_url}}"">Login here</a></p>
  <p>Thanks,<br>MiniWebsite Team</p>
</div>",
                "name,email,password,login_url", "Sent when a team member is created."),

            T("franchisee_admin_created", "Franchisee Created by Admin", "franchise",
                "Welcome to MiniWebsite Franchisee",
                @"<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
  <p>Hi <strong>{{name}}</strong>,</p>
  <p>Your franchisee account has been created by the MiniWebsite admin team.</p>
  <p><strong>Email:</strong> {{email}}<br><strong>Password:</strong> {{password}}</p>
  <p><a href=""{{login_url}}"">Login here</a></p>
  <p>Thanks,<br>MiniWebsite Team</p>
</div>",
                "name,email,password,login_url", "Admin-created franchisee welcome."),

            T("collaboration_upgraded", "Collaboration Feature Enabled", "admin",
                "Congratulations! Your Account Has Been Upgraded with Collaborator Feature",
                @"Hi {{name}},<br><br>Congratulations! The Collaborator feature has been enabled on your MiniWebsite account.<br><br>You can now collaborate and grow with MiniWebsite.<br><br>Thanks,<br>MiniWebsite Team",
                "name,email", "Sent when collaboration is enabled for a user."),

            T("franchise_enquiry_admin", "Franchise Enquiry (to support)", "admin",
                "Franchise Enquiry from {{name}}",
                @"<p><strong>Name:</strong> {{name}}</p>
<p><strong>Email:</strong> {{email}}</p>
<p><strong>Phone:</strong> {{phone}}</p>
<p><strong>State:</strong> {{state}}</p>
<p><strong>Message:</strong><br>{{message}}</p>",
                "name,email,phone,state,message", "Internal alert when franchise enquiry form is submitted.")
        );
    }

    private async Task SeedThemesAsync(CancellationToken ct)
    {
        if (await _db.ThemeCatalog.AnyAsync(ct)) return;

        for (var i = 1; i <= 5; i++)
        {
            _db.ThemeCatalog.Add(new ThemeCatalog
            {
                ThemeNumber = i,
                ThemeName = $"Theme {i}",
                CssValue = $"theme/css/theme{i}.css",
                IsActive = true,
                SortOrder = i,
                CreatedAt = DateTime.UtcNow,
            });
        }
    }

    private async Task SeedPricingPlansAsync(CancellationToken ct)
    {
        if (await _db.PricingPlans.AnyAsync(ct)) return;

        var seeds = new (string Type, string Name, decimal Price)[]
        {
            ("MW Plan (For MW Users)", "MiniWebsite 1 Year Plan", 999m),
            ("MW Plan (For MW Users)", "MiniWebsite 2 Year Plan", 1799m),
            ("MW Plan (For MW Users)", "MiniWebsite 3 Year Plan", 2499m),
            ("MW Plan (For Franchise Partner)", "MiniWebsite 1 Year Plan", 799m),
            ("MW Plan (For Franchise Partner)", "MiniWebsite 2 Year Plan", 1499m),
            ("MW Plan (For Franchise Partner)", "MiniWebsite 3 Year Plan", 2199m),
            ("Franchise Fees", "MW Full Franchise (Lifetime Fees)", 25000m),
            ("Digital Marketer Partnership Fees", "MW Digital Marketing (Annual Membership Fees)", 5000m),
        };

        foreach (var (type, name, price) in seeds)
        {
            var gst = Math.Round(price * 1.18m, 2);
            _db.PricingPlans.Add(new PricingPlan
            {
                PlanType = type,
                PlanName = name,
                OriginalPrice = price,
                OriginalPriceGst = gst,
                OfferedPrice = price,
                OfferedPriceGst = gst,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
            });
        }
    }

    private async Task SeedRoleAccessCatalogAsync(CancellationToken ct)
    {
        if (await _db.RoleAccessProfiles.AnyAsync(ct)) return;

        var profiles = new[]
        {
            new RoleAccessProfile { ProfileKey = "customer", ProfileLabel = "Customer", BaseRole = "CUSTOMER", SortOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new RoleAccessProfile { ProfileKey = "franchisee", ProfileLabel = "Franchisee", BaseRole = "FRANCHISEE", SortOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
            new RoleAccessProfile { ProfileKey = "team", ProfileLabel = "Team", BaseRole = "TEAM", SortOrder = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
            new RoleAccessProfile { ProfileKey = "admin", ProfileLabel = "Admin", BaseRole = "ADMIN", SortOrder = 4, IsActive = true, CreatedAt = DateTime.UtcNow },
        };
        _db.RoleAccessProfiles.AddRange(profiles);

        var features = new[]
        {
            new RoleAccessFeature { FeatureKey = "website_builder", FeatureLabel = "Website Builder", FeatureGroup = "core", FieldType = "yes_no", SortOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new RoleAccessFeature { FeatureKey = "wallet", FeatureLabel = "Wallet", FeatureGroup = "core", FieldType = "yes_no", SortOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
            new RoleAccessFeature { FeatureKey = "referral", FeatureLabel = "Referral", FeatureGroup = "core", FieldType = "yes_no", SortOrder = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
            new RoleAccessFeature { FeatureKey = "kit", FeatureLabel = "Sales Kit", FeatureGroup = "franchise", FieldType = "yes_no", SortOrder = 4, IsActive = true, CreatedAt = DateTime.UtcNow },
            new RoleAccessFeature { FeatureKey = "customer_manager", FeatureLabel = "Customer Manager", FeatureGroup = "crm", FieldType = "yes_no", SortOrder = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
        };
        _db.RoleAccessFeatures.AddRange(features);
    }
}

public static class DataSeederExtensions
{
    public static IServiceCollection AddDataSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDataSeeder, DataSeeder>();
        return services;
    }
}
