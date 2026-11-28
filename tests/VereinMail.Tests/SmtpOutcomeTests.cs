using System.Net;
using System.Net.Sockets;
using System.Text;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace VereinMail.Tests;

/// <summary>Exercise SMTP ambiguity with a loopback server that controls DATA and QUIT replies.</summary>
public class SmtpOutcomeTests
{
    [Fact]
    public async Task CancellationWhileAwaitingDataReply_MayFollowServerAcceptance()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var sendCancellation = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        using var listener = StartListener();
        var captured = NewSignal();
        var server = ServeAsync(listener, captured, afterData: async (client, writer) =>
        {
            // The server has the message bytes, but its 250 reply is delayed or lost.
            // Assert cancellation behavior, not whether a real SMTP server persisted it.
            await Task.Delay(Timeout.InfiniteTimeSpan, deadline.Token);
        }, deadline.Token);

        try
        {
            var sender = CreateSender(listener);
            var send = sender.SendAsync("to@example.com", "Subject", "Body", sendCancellation.Token);
            var message = await captured.Task.WaitAsync(deadline.Token);
            Assert.Contains("Subject: Subject", message);
            sendCancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
        }
        finally
        {
            deadline.Cancel();
            await StopServerAsync(server);
        }
    }

    [Fact]
    public async Task ConnectionLostAfterData_DoesNotConfirmWhetherMessageWasAccepted()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var listener = StartListener();
        var captured = NewSignal();
        var server = ServeAsync(listener, captured, afterData: (client, writer) =>
        {
            // Disconnect after receiving DATA without confirming acceptance.
            client.Dispose();
            return Task.CompletedTask;
        }, deadline.Token);

        try
        {
            var sender = CreateSender(listener);
            var send = sender.SendAsync("to@example.com", "Subject", "Body", deadline.Token);
            Assert.Contains("Subject: Subject", await captured.Task.WaitAsync(deadline.Token));
            await Assert.ThrowsAsync<SmtpProtocolException>(() => send);
            await server.WaitAsync(deadline.Token);
        }
        finally
        {
            deadline.Cancel();
            await StopServerAsync(server);
        }
    }

    [Fact]
    public async Task CancellationDuringQuit_DoesNotReportFailureAfterDataAccepted()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var sendCancellation = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        using var listener = StartListener();
        var captured = NewSignal();
        var quit = NewSignal();
        var server = ServeAsync(listener, captured, afterData: async (client, writer) =>
        {
            await writer.WriteLineAsync("250 queued");
            using var reader = new StreamReader(client.GetStream(), Encoding.ASCII, leaveOpen: true);
            quit.TrySetResult(await reader.ReadLineAsync(deadline.Token) ?? "<closed>");
            await Task.Delay(Timeout.InfiniteTimeSpan, deadline.Token);
        }, deadline.Token);

        try
        {
            var sender = CreateSender(listener);
            var send = sender.SendAsync("to@example.com", "Subject", "Body", sendCancellation.Token);
            Assert.Contains("Subject: Subject", await captured.Task.WaitAsync(deadline.Token));
            Assert.Equal("QUIT", await quit.Task.WaitAsync(deadline.Token));
            sendCancellation.Cancel();
            await send.WaitAsync(deadline.Token);
        }
        finally
        {
            deadline.Cancel();
            await StopServerAsync(server);
        }
    }

    private static TaskCompletionSource<string> NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TcpListener StartListener()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static SmtpEmailSender CreateSender(TcpListener listener)
    {
        var mail = new MailOptions
        {
            DefaultFrom = new() { Email = "from@example.com" },
            Smtp = new()
            {
                Server = "127.0.0.1",
                Port = ((IPEndPoint)listener.LocalEndpoint).Port,
                Security = SmtpSecurityMode.None,
                AllowInsecure = true,
                Timeout = 5
            }
        };
        return new SmtpEmailSender(Options.Create(mail), NullLogger<SmtpEmailSender>.Instance);
    }

    private static async Task ServeAsync(TcpListener listener, TaskCompletionSource<string> captured,
        Func<TcpClient, StreamWriter, Task> afterData, CancellationToken token)
    {
        using var client = await listener.AcceptTcpClientAsync(token);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true)
        {
            NewLine = "\r\n",
            AutoFlush = true
        };
        await writer.WriteLineAsync("220 localhost test SMTP");
        while (await reader.ReadLineAsync(token) is { } line)
        {
            if (line.StartsWith("EHLO ", StringComparison.OrdinalIgnoreCase))
                await writer.WriteLineAsync("250 localhost");
            else if (line.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
                await writer.WriteLineAsync("250 ok");
            else if (line.Equals("DATA", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("354 end with a dot");
                var data = new StringBuilder();
                while (await reader.ReadLineAsync(token) is { } bodyLine && bodyLine != ".")
                    data.AppendLine(bodyLine);
                captured.TrySetResult(data.ToString());
                await afterData(client, writer);
                return;
            }
            else
                throw new InvalidOperationException($"Unexpected SMTP command: {line}");
        }
    }

    private static async Task StopServerAsync(Task server)
    {
        try { await server.WaitAsync(TimeSpan.FromSeconds(3)); }
        catch (OperationCanceledException) { }
    }
}
