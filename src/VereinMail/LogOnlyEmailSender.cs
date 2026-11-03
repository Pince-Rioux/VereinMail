namespace VereinMail;

using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

/// <summary>
/// Safe fallback: logs every send request as a warning without delivering.
/// Used when <c>Mode</c> is not configured.
/// </summary>
public class LogOnlyEmailSender : IEmailSender
{
    private readonly ILogger<LogOnlyEmailSender> _logger;

    public LogOnlyEmailSender(ILogger<LogOnlyEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body)
    {
        _logger.LogWarning(
            "Mail NOT delivered (Mode is not File or Smtp). To: {To} | Subject: {Subject}",
            to, subject);

        return Task.CompletedTask;
    }
}