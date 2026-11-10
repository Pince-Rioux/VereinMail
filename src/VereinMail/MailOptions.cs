namespace VereinMail;

/// <summary>
/// SMTP transport security mode.
/// </summary>
public enum SmtpSecurityMode
{
    /// <summary>Require STARTTLS; fail if the server does not offer it. Recommended.</summary>
    StartTls,

    /// <summary>Use STARTTLS if offered, otherwise continue in plaintext. Less secure.</summary>
    StartTlsWhenAvailable,

    /// <summary>Use implicit TLS (SMTPS), typically on port 465.</summary>
    SslOnConnect,

    /// <summary>Do not use TLS. Use only in isolated development environments.</summary>
    None
}

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

        /// <summary>
        /// TLS mode. Defaults to <see cref="SmtpSecurityMode.StartTls"/> so that
        /// production mail is never sent over a plaintext connection.
        /// </summary>
        public SmtpSecurityMode Security { get; set; } = SmtpSecurityMode.StartTls;

        /// <summary>
        /// Verify the SMTP server certificate. Keep this enabled outside isolated
        /// development environments.
        /// </summary>
        public bool VerifyServerCertificate { get; set; } = true;

        public int Timeout { get; set; } = 30;
    }
}