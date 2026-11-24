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

    public Task SendAsync(string to, string subject, string body) =>
        SendAsync(to, subject, body, CancellationToken.None);

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var smtp = _options.Smtp;
        if (string.IsNullOrWhiteSpace(smtp.Server))
            throw new InvalidOperationException(
                "SMTP server is not configured; cannot send email.");

        if (smtp.Port is < 1 or > 65535)
            throw new InvalidOperationException("SMTP port must be between 1 and 65535.");
        if (smtp.Timeout <= 0 || smtp.Timeout > int.MaxValue / 1000)
            throw new InvalidOperationException("SMTP timeout must be positive and representable in milliseconds.");

        var secureSocketOptions = smtp.Security switch
        {
            SmtpSecurityMode.StartTls => SecureSocketOptions.StartTls,
            SmtpSecurityMode.StartTlsWhenAvailable => SecureSocketOptions.StartTlsWhenAvailable,
            SmtpSecurityMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
            SmtpSecurityMode.None => SecureSocketOptions.None,
            _ => throw new InvalidOperationException("Unknown SMTP security mode.")
        };
        var insecure = !smtp.VerifyServerCertificate || smtp.Security is SmtpSecurityMode.None or SmtpSecurityMode.StartTlsWhenAvailable;
        if (insecure && !smtp.AllowInsecure)
            throw new InvalidOperationException("Insecure SMTP settings require AllowInsecure and are for isolated development only.");
        if (insecure)
            _logger.LogWarning("Insecure SMTP settings are enabled. Use only in isolated development environments.");

        var message = BuildMessage(to, subject, body);
        using var client = new SmtpClient { Timeout = smtp.Timeout * 1000 };
        if (!smtp.VerifyServerCertificate)
            client.ServerCertificateValidationCallback = (_, _, _, _) => true;

        await client.ConnectAsync(smtp.Server, smtp.Port, secureSocketOptions, cancellationToken);
        if (!string.IsNullOrWhiteSpace(smtp.Username))
            await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        try
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex) when (ex is OperationCanceledException or System.IO.IOException or MailKit.ProtocolException)
        {
            // DATA has already been accepted. A failed QUIT must not cause a duplicate on retry.
            _logger.LogDebug("SMTP session did not close cleanly after the message was accepted.");
        }

        _logger.LogInformation("Mail accepted by the SMTP server.");
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