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
    /// <remarks>
    /// An exception does not always mean that no mail was delivered: cancellation or a
    /// lost connection after the SMTP message was submitted leaves the outcome unknown.
    /// The caller owns the retry policy; do not retry such a failure automatically.
    /// </remarks>
    Task SendAsync(string to, string subject, string body);

    /// <summary>
    /// Sends a message with cancellation. The default implementation checks cancellation before
    /// calling a legacy sender; only implementations of this overload can cancel in-flight work.
    /// </summary>
    /// <remarks>
    /// Cancelling an in-flight SMTP send can leave the message accepted at the server.
    /// Reusing the same operation can therefore produce a duplicate.
    /// </remarks>
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return SendAsync(to, subject, body);
    }
}