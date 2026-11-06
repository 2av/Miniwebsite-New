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

        var from = Mailbox("Noreply");
        var support = Mailbox("Support");
        var fromEmail = First(from.Username, _options.FromEmail, _options.Username);
        var username = First(from.Username, _options.Username, fromEmail);
        var password = First(from.Password, _options.Password);
        var replyTo = First(support.Username, "support@sharebizz.com");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            string.IsNullOrWhiteSpace(_options.FromName) ? "ShareBizz" : _options.FromName,
            fromEmail));
        if (!string.IsNullOrWhiteSpace(replyTo)
            && !string.Equals(replyTo, fromEmail, StringComparison.OrdinalIgnoreCase))
        {
            message.ReplyTo.Add(MailboxAddress.Parse(replyTo));
        }
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        // Port 465 = SSL on connect (same as PHP SMTP_SECURE=ssl); 587 = STARTTLS
        var secure = _options.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : (_options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

        await client.ConnectAsync(_options.Host, _options.Port, secure, cancellationToken);
        if (!string.IsNullOrWhiteSpace(username))
        {
            await client.AuthenticateAsync(username, password, cancellationToken);
        }
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        _logger.LogInformation("Email sent to {To} from {From} subject={Subject}", toEmail, fromEmail, subject);
    }

    private SmtpMailbox Mailbox(string name)
    {
        if (_options.Mailboxes.TryGetValue(name, out var box) && box != null)
            return box;
        return new SmtpMailbox();
    }

    private static string First(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
        }
        return "";
    }
}
