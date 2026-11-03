namespace VereinMail;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using System;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Development transport: writes each message as an <c>.eml</c> file.
/// The directory is configured through <c>File.Path</c> in the mail section
/// and may be absolute or relative to the application content root.
/// </summary>
public class FileEmailSender : IEmailSender
{
    private readonly MailOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileEmailSender> _logger;

    public FileEmailSender(
        IOptions<MailOptions> options,
        IWebHostEnvironment environment,
        ILogger<FileEmailSender> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var directory = ResolveDirectory();
        Directory.CreateDirectory(directory);

        var message = BuildMessage(to, subject, body);
        var filePath = Path.Combine(directory, message.MessageId + ".eml");

        await message.WriteToAsync(new FormatOptions { MaxLineLength = 100 }, filePath);

        _logger.LogInformation(
            "Mail written to file: {File} (to {To}, subject {Subject})",
            filePath, to, subject);
    }

    private string ResolveDirectory()
    {
        var configuredPath = _options.File.Path;
        if (string.IsNullOrWhiteSpace(configuredPath))
            return Path.Combine(_environment.ContentRootPath, "maildrop");

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configuredPath));
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