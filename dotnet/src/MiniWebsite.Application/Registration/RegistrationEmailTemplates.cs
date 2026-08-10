using MiniWebsite.Application.Common.Options;

namespace MiniWebsite.Application.Registration;

public static class RegistrationEmailTemplates
{
    public static (string Subject, string Html) OtpEmail(string name, string otp, string host) =>
    (
        $"{host} - Your OTP for Registration",
        $@"Hi {System.Net.WebUtility.HtmlEncode(name)},<br><br>
Your OTP for registration on {System.Net.WebUtility.HtmlEncode(host)} is: <b>{System.Net.WebUtility.HtmlEncode(otp)}</b><br><br>
This OTP is valid for 10 minutes.<br><br>
Thanks,<br>{System.Net.WebUtility.HtmlEncode(host)} Team"
    );

    public static (string Subject, string Html) CustomerWelcome(
        string name, string email, string plainPassword, AppOptions app)
    {
        var host = app.PublicHost;
        var dashboard = app.CustomerDashboardUrl;
        var subject = $"Welcome to {host}";
        var html = $@"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <p style=""color: #333; font-size: 16px; line-height: 1.6;"">Hi <strong>{System.Net.WebUtility.HtmlEncode(name)}</strong>,</p>
    <p style=""color: #333; font-size: 16px; line-height: 1.6;"">Thank you for registering on {System.Net.WebUtility.HtmlEncode(host)}.</p>
    <p style=""color: #333; font-size: 16px; line-height: 1.6;"">Your account has been created successfully and you have been automatically logged in!</p>
    <div style=""background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 8px; padding: 20px; margin: 20px 0;"">
        <h3 style=""color: #333; font-size: 18px; margin-top: 0; margin-bottom: 15px;"">🔐 Your Login Details:</h3>
        <p style=""color: #333; font-size: 16px; line-height: 1.6; margin: 10px 0;""><strong>Email ID:</strong> {System.Net.WebUtility.HtmlEncode(email)}</p>
        <p style=""color: #333; font-size: 16px; line-height: 1.6; margin: 10px 0;""><strong>Password:</strong> {System.Net.WebUtility.HtmlEncode(plainPassword)}</p>
        <p style=""color: #333; font-size: 16px; line-height: 1.6; margin: 10px 0;"">👉 <a href=""{System.Net.WebUtility.HtmlEncode(dashboard)}"" style=""color: #007bff; text-decoration: none;"">Click here to access your dashboard</a></p>
    </div>
    <p style=""color: #333; font-size: 16px; line-height: 1.6;"">You can now start creating your Mini Website from your dashboard.</p>
    <br>
    <p style=""color: #333; font-size: 16px; line-height: 1.6;"">Thanks,<br>{System.Net.WebUtility.HtmlEncode(host)} Team</p>
</div>";
        return (subject, html);
    }

    public static (string Subject, string Html) FranchiseeWelcome(
        string name, string email, string plainPassword, AppOptions app, bool includePaymentStep = true)
    {
        var host = app.PublicHost;
        var loginUrl = app.FranchiseeLoginUrl;
        var payUrl = $"{app.FranchiseAgreementUrl}?email={Uri.EscapeDataString(email)}";
        var displayName = string.IsNullOrWhiteSpace(name) || name.Contains('@', StringComparison.Ordinal)
            ? "there"
            : name;

        var subject = "Welcome to MiniWebsite.in – Your Franchise Account is Ready!";
        var intro =
            "We are excited to have you on board! Your franchise account has been successfully created. You can now log in using your email and password at the link below:";

        var steps = includePaymentStep
            ? $@"
            <p style=""color: #333; font-size: 16px; line-height: 1.6;""><strong>1. Pay the One-Time Franchise Fee (Non-Refundable)</strong><br>
            Amount: ₹30,000 + 18% GST = ₹35,400<br>
            <a href=""{System.Net.WebUtility.HtmlEncode(payUrl)}"" style=""color: #007bff; text-decoration: none;"">(Click to Pay)</a></p>
            <p style=""color: #333; font-size: 16px; line-height: 1.6;""><strong>2. After payment, complete your document Verification from your Dashboard.</strong></p>
            <p style=""color: #333; font-size: 16px; line-height: 1.6;""><strong>3. After the documents get verified, you can access your Marketing Kit and Onboarding Material from your dashboard only.</strong></p>"
            : $@"
            <p style=""color: #333; font-size: 16px; line-height: 1.6;""><strong>1. Complete your document Verification from your Dashboard.</strong></p>
            <p style=""color: #333; font-size: 16px; line-height: 1.6;""><strong>2. After the documents get verified, you can access your Marketing Kit and Onboarding Material from your dashboard only.</strong></p>";

        var html = $@"
        <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
            <p style=""color: #333; font-size: 16px; line-height: 1.6;"">Hi <strong>{System.Net.WebUtility.HtmlEncode(displayName)}</strong>,</p>
            <p style=""color: #333; font-size: 16px; line-height: 1.6;"">Thank you for registering as a franchise with MiniWebsite.in.</p>
            <p style=""color: #333; font-size: 16px; line-height: 1.6;"">{intro}</p>
            <div style=""background-color: #f8f9fa; border: 1px solid #dee2e6; border-radius: 8px; padding: 20px; margin: 20px 0;"">
                <h3 style=""color: #333; font-size: 18px; margin-top: 0; margin-bottom: 15px;"">🔐 Your Login Details:</h3>
                <p style=""color: #333; font-size: 16px; line-height: 1.6; margin: 10px 0;""><strong>Email ID:</strong> {System.Net.WebUtility.HtmlEncode(email)}</p>
                <p style=""color: #333; font-size: 16px; line-height: 1.6; margin: 10px 0;""><strong>Password:</strong> {System.Net.WebUtility.HtmlEncode(plainPassword)}</p>
                <p style=""color: #333; font-size: 16px; line-height: 1.6; margin: 10px 0;"">👉 <a href=""{System.Net.WebUtility.HtmlEncode(loginUrl)}"" style=""color: #007bff; text-decoration: none;"">Click here to login</a></p>
            </div>
            <br>
            <p style=""color: #333; font-size: 16px; line-height: 1.6;""><strong>Follow these simple steps to activate your franchise:</strong></p>
            {steps}
            <br>
            <p style=""color: #333; font-size: 16px; line-height: 1.6;"">That's it! Once these steps are completed, you are officially part of the MiniWebsite.in franchise network. You can begin building your business and start earning right away.</p>
            <p style=""color: #333; font-size: 16px; line-height: 1.6;"">If you have any questions or need assistance, feel free to reach out to our support team.</p>
            <br>
            <p style=""color: #333; font-size: 16px; line-height: 1.6;"">Best regards,<br>
            Team MiniWebsite.in<br>
            <a href=""https://www.miniwebsite.in"">www.miniwebsite.in</a></p>
        </div>";

        return (subject, html);
    }
}
