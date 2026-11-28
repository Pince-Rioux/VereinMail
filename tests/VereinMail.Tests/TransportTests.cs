using System.Buffers;
using System.Net;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpServer;
using SmtpServer.Net;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using Xunit;

namespace VereinMail.Tests;

public class TransportTests
{
    [Fact]
    public async Task FileSender_WritesTwoDistinctMessages()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var sender = new FileEmailSender(Options.Create(MailOptionsForTest()),
                new TestEnvironment { ContentRootPath = root.FullName }, NullLogger<FileEmailSender>.Instance);
            await sender.SendAsync("to@example.com", "First", "one");
            await sender.SendAsync("to@example.com", "Second", "two");
            var files = Directory.GetFiles(Path.Combine(root.FullName, "maildrop"));
            Assert.Equal(2, files.Length);
            Assert.All(files, path => Assert.EndsWith(".eml", path));
            var messages = await Task.WhenAll(files.Select(path => MimeMessage.LoadAsync(path)));
            Assert.Equal(new[] { "First", "Second" }, messages.Select(m => m.Subject).Order());
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SmtpSender_EnforcesRequiredTlsAtLoopbackServer(bool requireTls)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var options = new SmtpServerOptionsBuilder().ServerName("localhost")
            .Endpoint(endpoint => endpoint.Endpoint(new IPEndPoint(IPAddress.Loopback, 0))).Build();
        var store = new CaptureStore();
        var ready = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = new EndpointListenerFactory();
        listener.EndpointStarted += (_, args) => ready.TrySetResult(((IPEndPoint)args.LocalEndPoint).Port);
        var services = new SmtpServer.ComponentModel.ServiceProvider();
        services.Add(listener);
        services.Add(store);
        var server = new SmtpServer.SmtpServer(options, services);
        var running = server.StartAsync(deadline.Token);
        try
        {
            var mail = MailOptionsForTest();
            mail.Smtp.Server = "127.0.0.1";
            mail.Smtp.Port = await ready.Task.WaitAsync(deadline.Token);
            if (!requireTls)
            {
                mail.Smtp.Security = SmtpSecurityMode.None;
                mail.Smtp.AllowInsecure = true;
            }
            IEmailSender sender = new SmtpEmailSender(Options.Create(mail), NullLogger<SmtpEmailSender>.Instance);
            Task Send() => sender.SendAsync("to@example.com", "Local delivery", "Hello loopback", deadline.Token);
            if (requireTls)
            {
                await Assert.ThrowsAsync<NotSupportedException>(Send);
                Assert.False(store.Received.Task.IsCompleted);
            }
            else
            {
                await Send();
                var message = await store.Received.Task.WaitAsync(deadline.Token);
                Assert.Equal("to@example.com", Assert.Single(message.To.Mailboxes).Address);
                Assert.Equal("Hello loopback", message.TextBody?.Trim());
            }
        }
        finally
        {
            server.Shutdown();
            await running.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task LegacyImplementation_StillWorksWithCancellationOverload()
    {
        var legacy = new LegacySender();
        IEmailSender sender = legacy;
        await sender.SendAsync("to@example.com", "Subject", "Body", CancellationToken.None);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sender.SendAsync("to@example.com", "Subject", "Body", new CancellationToken(true)));
        Assert.Equal(1, legacy.Calls);
    }

    [Theory]
    [InlineData(SmtpSecurityMode.None, true)]
    [InlineData(SmtpSecurityMode.StartTlsWhenAvailable, true)]
    [InlineData(SmtpSecurityMode.StartTls, false)]
    public async Task SmtpSender_RejectsInsecureSettingsWithoutOptIn(SmtpSecurityMode mode, bool verify)
    {
        var mail = MailOptionsForTest();
        mail.Smtp.Server = "127.0.0.1";
        mail.Smtp.Security = mode;
        mail.Smtp.VerifyServerCertificate = verify;
        var sender = new SmtpEmailSender(Options.Create(mail), NullLogger<SmtpEmailSender>.Instance);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync("to@example.com", "Subject", "Body"));
        Assert.Contains("AllowInsecure", error.Message);
    }

    private static MailOptions MailOptionsForTest() => new() { DefaultFrom = new() { Email = "from@example.com" } };

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "VereinMail.Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class CaptureStore : MessageStore
    {
        public TaskCompletionSource<MimeMessage> Received { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction,
            ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream(buffer.ToArray());
            Received.TrySetResult(await MimeMessage.LoadAsync(stream, cancellationToken));
            return SmtpResponse.Ok;
        }
    }

    private sealed class LegacySender : IEmailSender
    {
        public int Calls { get; private set; }
        public Task SendAsync(string to, string subject, string body)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("to@example.com, second@example.com")]
    [InlineData("Group: to@example.com;")]
    public async Task FileSender_RejectsNonSingleAddressSyntax(string recipient)
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var sender = new FileEmailSender(Options.Create(MailOptionsForTest()),
                new TestEnvironment { ContentRootPath = root.FullName }, NullLogger<FileEmailSender>.Instance);
            await Assert.ThrowsAsync<MimeKit.ParseException>(
                () => sender.SendAsync(recipient, "Subject", "Body"));
            Assert.Empty(Directory.GetFiles(root.FullName));
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task FileSender_PreservesSingleMailboxDisplayName()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var sender = new FileEmailSender(Options.Create(MailOptionsForTest()),
                new TestEnvironment { ContentRootPath = root.FullName }, NullLogger<FileEmailSender>.Instance);
            await sender.SendAsync("Member <member@example.com>", "Subject", "Body");
            var files = Directory.GetFiles(Path.Combine(root.FullName, "maildrop"));
            var message = await MimeMessage.LoadAsync(Assert.Single(files));
            var address = Assert.Single(message.To.Mailboxes);
            Assert.Equal("Member", address.Name);
            Assert.Equal("member@example.com", address.Address);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }
}
