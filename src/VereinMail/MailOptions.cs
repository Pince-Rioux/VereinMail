namespace VereinMail;

/// <summary>
/// Configuration for mail delivery.
/// Bound from the <c>MembershipMail</c> configuration section.
/// </summary>
public class MailOptions
{
    /// <summary>Delivery mode: <c>File</c>, <c>Smtp</c>, <c>Log</c>.</summary>
    public string Mode { get; set; } = "Log";

    /// <summary>Sender address used for all module emails.</summary>
    public MailAddressOptions DefaultFrom { get; set; } = new();

    /// <summary>Optional organization header.</summary>
    public string? Organization { get; set; }

    /// <summary>File transport settings (used when <see cref="Mode"/> is <c>File</c>).</summary>
    public FileMailOptions File { get; set; } = new();

    /// <summary>SMTP transport settings (used when <see cref="Mode"/> is <c>Smtp</c>).</summary>
    public SmtpMailOptions Smtp { get; set; } = new();

    /// <summary>A single address with display name.</summary>
    public class MailAddressOptions
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>Directory where <c>.eml</c> files are written.</summary>
    public class FileMailOptions
    {
        public string Path { get; set; } = string.Empty;
    }

    /// <summary>SMTP connection parameters.</summary>
    public class SmtpMailOptions
    {
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Timeout { get; set; } = 30;
    }
}