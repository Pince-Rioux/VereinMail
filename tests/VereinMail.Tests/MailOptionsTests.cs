using VereinMail;
using Xunit;

namespace VereinMail.Tests;

/// <summary>
/// Tests for the MailOptions configuration model.
/// </summary>
public class MailOptionsTests
{
    [Fact]
    public void Defaults_Are_Correct()
    {
        var options = new MailOptions();

        Assert.Equal("Log", options.Mode);
        Assert.Equal(587, options.Smtp.Port);
        Assert.Equal(SmtpSecurityMode.StartTls, options.Smtp.Security);
        Assert.True(options.Smtp.VerifyServerCertificate);
        Assert.Equal(30, options.Smtp.Timeout);
        Assert.Equal(string.Empty, options.File.Path);
        Assert.Equal(string.Empty, options.DefaultFrom.Name);
        Assert.Equal(string.Empty, options.DefaultFrom.Email);
    }

    [Fact]
    public void VerifyServerCertificate_Can_Be_Disabled()
    {
        var smtp = new MailOptions.SmtpMailOptions
        {
            VerifyServerCertificate = false
        };

        Assert.False(smtp.VerifyServerCertificate);
    }

    [Fact]
    public void Mode_Can_Be_Smtp()
    {
        var options = new MailOptions { Mode = "Smtp" };

        Assert.Equal("Smtp", options.Mode);
    }

    [Theory]
    [InlineData(SmtpSecurityMode.StartTls)]
    [InlineData(SmtpSecurityMode.StartTlsWhenAvailable)]
    [InlineData(SmtpSecurityMode.SslOnConnect)]
    [InlineData(SmtpSecurityMode.None)]
    public void Security_Can_Be_Configured(SmtpSecurityMode mode)
    {
        var smtp = new MailOptions.SmtpMailOptions { Security = mode };

        Assert.Equal(mode, smtp.Security);
    }
}
