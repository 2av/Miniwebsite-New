namespace MiniWebsite.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// <paramref name="replyToMailbox"/> is a configured mailbox name, such as Support or Billing.
    /// </summary>
    Task SendAsync(string toEmail, string subject, string htmlBody, string replyToMailbox, CancellationToken cancellationToken = default);
}
