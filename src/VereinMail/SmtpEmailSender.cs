namespace VereinMail;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using System;
using System.Threading.Tasks;

/// <summary>
/// Production transport: sends messages through an SMTP server.
/// Settings come from the SMTP sub-section of the mail configuration.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly MailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<MailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var smtp = _options.Smtp;
        if (string.IsNullOrWhiteSpace(smtp.Server))
            throw new InvalidOperationException(
                "SMTP server is not configured; cannot send email.");

        var message = BuildMessage(to, subject, body);

        using var client = new SmtpClient
        {
            Timeout = smtp.Timeout > 0 ? smtp.Timeout * 1000 : 30_000,
            ServerCertificateValidationCallback = (_, _, _, _) => true
        };

        await client.ConnectAsync(smtp.Server, smtp.Port, SecureSocketOptions.StartTlsWhenAvailable);
        if (!string.IsNullOrWhiteSpace(smtp.Username))
            await client.AuthenticateAsync(smtp.Username, smtp.Password);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation(
            "Mail sent via SMTP to {To} (subject {Subject})", to, subject);
    }

    private MimeMessage BuildMessage(string to, string subject, string body)
    {
        var message = new MimeMessage();

        if (!string.IsNullOrWhiteSpace(_options.Organization))
            message.Headers.Add(HeaderId.Organization, _options.Organization);

        var from = _options.DefaultFrom;
        message.From.Add(new MailboxAddress(
            string.IsNullOrWhiteSpace(from.Name) ? "VereinMail" : from.Name,
            string.IsNullOrWhiteSpace(from.Email) ? "noreply@localhost" : from.Email));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Text) { Text = body };

        return message;
    }
}