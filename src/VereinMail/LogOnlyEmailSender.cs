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

    public Task SendAsync(string to, string subject, string body) =>
        SendAsync(to, subject, body, CancellationToken.None);

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        _logger.LogWarning("Mail not delivered: the log-only transport is active.");
        return Task.CompletedTask;
    }
}
