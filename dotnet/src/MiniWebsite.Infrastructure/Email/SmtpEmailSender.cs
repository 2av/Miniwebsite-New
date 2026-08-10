using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MiniWebsite.Application.Common.Interfaces;
using MiniWebsite.Infrastructure.Options;

namespace MiniWebsite.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("SMTP Host is empty. Email to {To} was skipped (scaffold mode).", toEmail);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            string.IsNullOrWhiteSpace(_options.FromName) ? "MiniWebsite Support" : _options.FromName,
            string.IsNullOrWhiteSpace(_options.FromEmail) ? _options.Username : _options.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        // Port 465 = SSL on connect (same as PHP SMTP_SECURE=ssl); 587 = STARTTLS
        var secure = _options.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : (_options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

        await client.ConnectAsync(_options.Host, _options.Port, secure, cancellationToken);
        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        _logger.LogInformation("Email sent to {To} subject={Subject}", toEmail, subject);
    }
}
