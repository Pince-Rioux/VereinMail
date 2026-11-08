using System.Threading.Tasks;
using System;
using VereinMail;

/// <summary>
/// Tests for MailOptions configuration model.
/// </summary>
public class MailOptionsTests
{
    /// <summary>
    /// Tests that default values are set correctly.
    /// </summary>
    public void Defaults_Are_Correct()
    {
        var options = new MailOptions();
        AssertEqual("Mode should default to Log", "Log", options.Mode);
        AssertEqual("SMTP Port should default to 587", 587, options.Smtp.Port);
        AssertEqual("VerifyServerCertificate should default to true", true, options.Smtp.VerifyServerCertificate);
        AssertEqual("Timeout should default to 30s", 30, options.Smtp.Timeout);
        AssertEqual("File.Path should default to empty string", string.Empty, options.File.Path);
        AssertEqual("DefaultFrom.Name should default to empty", string.Empty, options.DefaultFrom.Name);
        AssertEqual("DefaultFrom.Email should default to empty", string.Empty, options.DefaultFrom.Email);
    }

    /// <summary>
    /// Tests that VerifyServerCertificate can be explicitly set to false.
    /// </summary>
    public void VerifyServerCertificate_Can_Be_Disabled()
    {
        var smtp = new MailOptions.SmtpMailOptions();
        smtp.VerifyServerCertificate = false;
        AssertEqual("VerifyServerCertificate should be false", false, smtp.VerifyServerCertificate);
    }

    /// <summary>
    /// Tests that Mode can be set to Smtp.
    /// </summary>
    public void Mode_Can_Be_Smtp()
    {
        var options = new MailOptions();
        options.Mode = "Smtp";
        AssertEqual("Mode should be Smtp", "Smtp", options.Mode);
    }

    /// <summary>Simple assertion helper.</summary>
    private void AssertEqual(string message, object expected, object actual)
    {
        if (expected != actual)
            throw new Exception($"{message}: expected '{expected}', got '{actual}'");
    }
}