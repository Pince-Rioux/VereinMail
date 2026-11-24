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

    /// <summary>
    /// Sends a message with cancellation. The default implementation checks cancellation before
    /// calling a legacy sender; only implementations of this overload can cancel in-flight work.
    /// </summary>
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return SendAsync(to, subject, body);
    }
}