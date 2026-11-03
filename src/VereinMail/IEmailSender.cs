namespace VereinMail;

/// <summary>
/// Abstraction for sending emails.
/// Implementations may write to disk, send via SMTP, or only log.
/// Consumers receive this interface through dependency injection.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email to a single recipient.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="body">Plain-text email body.</param>
    Task SendAsync(string to, string subject, string body);
}