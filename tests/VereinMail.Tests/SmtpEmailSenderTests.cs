using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VereinMail;
using Xunit;

namespace VereinMail.Tests;

public class SmtpEmailSenderTests
{
    [Fact]
    public async Task SendAsync_WithoutServer_ThrowsInvalidOperationException()
    {
        var options = Options.Create(new MailOptions
        {
            Smtp = new MailOptions.SmtpMailOptions
            {
                Server = string.Empty
            }
        });

        var sender = new SmtpEmailSender(
            options,
            NullLogger<SmtpEmailSender>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync("recipient@example.com", "Subject", "Body"));
    }
}
