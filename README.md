# VereinMail

> A lightweight, dependency-injected email transport library for .NET applications.

VereinMail provides a clean `IEmailSender` interface and three ready-to-use implementations — **File** (`.eml` for development), **SMTP** (production), and **Log** (safe fallback). It follows the dependency-inversion principle: consumers depend only on the interface; wiring the transport is the application's job.

The library targets .NET 8 and does not require the ASP.NET Core shared framework. The file transport uses the .NET Generic Host’s content root.

---

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
- [Building](#building)
- [Project Structure](#project-structure)
- [License](#license)

---

## Features

- **`IEmailSender` interface** — `SendAsync(to, subject, body)` with a cancellable overload. Built-in senders never log recipients, subjects, or bodies.
- **`FileEmailSender`** — writes MIME-compliant `.eml` files for development and testing. No real delivery.
- **`SmtpEmailSender`** — full SMTP delivery with optional authentication, configurable TLS mode, certificate validation, and timeouts.
- **`LogOnlyEmailSender`** — fail-safe fallback that logs every send request as a warning without delivering.
- **Configuration-driven** — `MailOptions` bound from the application config. No hardcoded settings.
- **Zero static state** — all implementations are injectable and testable.

---

## Architecture

```
┌──────────────────────┐
│    IEmailSender      │  ◄── Consumer depends only on this
├──────────────────────┤
│   SendAsync(to,      │
│     subject, body)   │
└──────────────┬───────┘
               │ implements
    ┌──────────┼──────────────────────┐
    │          │                      │
    ▼          ▼                      ▼
┌──────────┐  ┌──────────────┐  ┌────────────────┐
│ File     │  │ Smtp         │  │ LogOnly        │
│ .eml     │  │ SMTP+STARTTLS│  │ Logger.Warning │
│ Dev only │  │ Production   │  │ Safe fallback  │
└──────────┘  └──────────────┘  └────────────────┘
```

**Dependencies:**

- **Framework:** `Microsoft.AspNetCore.App` (for `IWebHostEnvironment`, `ILogger`, `IOptions`)
- **MIME & SMTP:** [MailKit](https://www.nuget.org/packages/MailKit) 4.17+ ([GitHub](https://github.com/jstedfast/MailKit) · [MimeKit](http://www.mimekit.net/))

---

## Installation

### As a Git dependency (developing alongside your project)

```bash
git submodule add https://github.com/Pince-Rioux/VereinMail.git Libraries/VereinMail
```

Add a `ProjectReference` to your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="Libraries/VereinMail/src/VereinMail/VereinMail.csproj" />
</ItemGroup>
```

### As a NuGet package (consuming a release)

VereinMail is distributed through [GitHub Packages](https://github.com/Pince-Rioux/VereinMail/packages), not nuget.org. Configure `https://nuget.pkg.github.com/Pince-Rioux/index.json` as a NuGet source and keep nuget.org enabled for dependencies. Authenticate if required: locally, use a GitHub PAT (classic) with `read:packages` via your NuGet credential configuration; in Actions, use a `GITHUB_TOKEN` with `packages: read` and grant the consuming repository access. Do not commit credentials.

Select a published version from the feed, including the prerelease suffix when installing an RC:

```bash
dotnet add package VereinMail --version "$VERSION"
```

Set `VERSION` to the exact version listed in GitHub Packages. The release workflow does not publish packages automatically on merge.

When upgrading VereinMember, update its direct `MailKit` reference to `4.18.2` or later at the same time. Its `4.17.0` pin otherwise causes a NuGet downgrade error (`NU1605`).

---

## Configuration

All settings are defined in `MailOptions` and bound from a configuration section of your application — for example `Mail` or `MembershipMail` in an `AppSettings.json` file.

| Section | Key | Type | Default | Description |
|---|---|---|---|---|
| `{YourSection}` | `Mode` | string | `"Log"` | Transport: `"File"`, `"Smtp"`, or `"Log"` |
| `{YourSection}.DefaultFrom` | `Name` | string | `"VereinMail"` | Display name for the sender |
| `{YourSection}.DefaultFrom` | `Email` | string | `"noreply@localhost"` | Sender email address |
| `{YourSection}` | `Organization` | string | *(none)* | Optional `Organization` MIME header |
| `{YourSection}.File` | `Path` | string | `"maildrop"` | Directory for `.eml` files (absolute or relative to content root) |
| `{YourSection}.Smtp` | `Server` | string | `""` | SMTP hostname |
| `{YourSection}.Smtp` | `Port` | int | `587` | SMTP port |
| `{YourSection}.Smtp` | `Username` | string | `""` | SMTP login (optional) |
| `{YourSection}.Smtp` | `Password` | string | `""` | SMTP password (optional) |
| `{YourSection}.Smtp` | `Security` | string | `"StartTls"` | TLS mode: `StartTls` (required), `StartTlsWhenAvailable`, `SslOnConnect`, or `None` |
| `{YourSection}.Smtp` | `VerifyServerCertificate` | bool | `true` | Verify the SMTP server certificate. Disable only in isolated development environments. |
| `{YourSection}.Smtp` | `AllowInsecure` | bool | `false` | Explicit opt-in to opportunistic TLS, plaintext, or disabled certificate checks. For isolated development only. |
| `{YourSection}.Smtp` | `Timeout` | int | `30` | Socket timeout in seconds |

> **Migration:** `StartTls` requires TLS and rejects servers that previously worked with `StartTlsWhenAvailable`. Prefer enabling STARTTLS on the server or using `SslOnConnect` (typically port 465). `StartTlsWhenAvailable`, `None`, and disabling certificate validation now also require `AllowInsecure: true`; use these settings only in isolated development. `FileEmailSender` now takes `IHostEnvironment` instead of `IWebHostEnvironment`; existing registrations by implementation type work with the Generic Host (including ASP.NET Core), but rebuild consumers that directly call the constructor. The three-argument `IEmailSender.SendAsync` remains supported. The cancellation overload cancels built-in transports; for legacy implementations it only checks cancellation before dispatch.

### Example configuration

```json
{
  "Mail": {
    "Mode": "Smtp",
    "DefaultFrom": {
      "Name": "Your App Mailer",
      "Email": "noreply@yourdomain.org"
    },
    "Organization": "Your Organization e.V.",
    "Smtp": {
      "Server": "smtp.example.com",
      "Port": 587,
      "Username": "smtp-user",
      "Security": "StartTls",
      "VerifyServerCertificate": true
    }
  }
}
```

---

## Usage

### Registering the sender (in your application startup)

The application selects the transport. In a console app, add `Microsoft.Extensions.Hosting` (8.0 or later, matching your target framework). The following example uses `appsettings.json` or environment variables such as `Mail__Mode` and `Mail__Smtp__Password`:

<!-- quickstart:start -->
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VereinMail;

var builder = Host.CreateApplicationBuilder(args);
var section = builder.Configuration.GetSection("Mail");
var mail = section.Get<MailOptions>() ?? new MailOptions();
builder.Services.Configure<MailOptions>(section);

switch (mail.Mode.Trim().ToUpperInvariant())
{
    case "FILE":
        builder.Services.AddSingleton<IEmailSender, FileEmailSender>();
        break;
    case "SMTP":
        builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
        break;
    case "LOG":
        builder.Services.AddSingleton<IEmailSender, LogOnlyEmailSender>();
        break;
    default:
        throw new InvalidOperationException("Unknown Mail:Mode.");
}

using var host = builder.Build();
var sender = host.Services.GetRequiredService<IEmailSender>();
await sender.SendAsync("recipient@example.com", "Quickstart", "Hello from VereinMail.");
```
<!-- quickstart:end -->

### Sending an email

```csharp
// Any service that depends on IEmailSender
public class NotificationService
{
    private readonly IEmailSender _mailSender;

    public NotificationService(IEmailSender mailSender)
    {
        _mailSender = mailSender;
    }

    public async Task SendWelcome(string email, string name)
    {
        await _mailSender.SendAsync(
            email,
            "Welcome to YourApp",
            $"Hello {name},\n\nwelcome aboard!\n\nBest regards\nThe YourApp Team"
        );
    }
}
```

---

## Building

```bash
# Restore and build the complete solution
dotnet restore VereinMail.slnx
dotnet build VereinMail.slnx --no-restore

# Build in release mode
dotnet build VereinMail.slnx --configuration Release

# Run all tests
dotnet test VereinMail.slnx --configuration Release

# Create a NuGet package
dotnet pack src/VereinMail/VereinMail.csproj --configuration Release
```

**Requirements:**
- .NET SDK 10.0+ for the standard `.slnx` build (the projects themselves still target .NET 8)
- NuGet dependencies resolved (run `dotnet restore` if needed)

---

## Project Structure

```
VereinMail/
├── VereinMail.slnx          # Solution with library and tests
├── .editorconfig           # Shared C# formatting rules
├── .gitattributes          # Line-ending and diff configuration
├── README.md
├── LICENSE
├── CONTRIBUTING.md
├── .gitignore
├── src/
│   └── VereinMail/
│       ├── VereinMail.csproj    # Build configuration
│       ├── IEmailSender.cs      # Interface (the contract)
│       ├── FileEmailSender.cs   # .eml file transport (dev)
│       ├── SmtpEmailSender.cs   # SMTP transport (production)
│       ├── LogOnlyEmailSender.cs# Fallback transport (safe default)
│       └── MailOptions.cs       # Configuration model
└── tests/
    └── VereinMail.Tests/
        └── VereinMail.Tests.csproj
```

---

## License

MIT — see [LICENSE](LICENSE).

VereinMail is an independent, reusable email transport library for .NET applications.
