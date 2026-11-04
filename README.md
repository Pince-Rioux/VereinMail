# VereinMail

> A lightweight, dependency-injected email transport library for .NET applications.

VereinMail provides a clean `IEmailSender` interface and three ready-to-use implementations — **File** (`.eml` for development), **SMTP** (production), and **Log** (safe fallback). It follows the dependency-inversion principle: consumers depend only on the interface; wiring the transport is the application's job.

The library is framework-agnostic in design and can be used in any .NET application.

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

- **`IEmailSender` interface** — single-method contract: `SendAsync(to, subject, body)`.
- **`FileEmailSender`** — writes MIME-compliant `.eml` files for development and testing. No real delivery.
- **`SmtpEmailSender`** — full SMTP delivery with optional authentication, STARTTLS, and configurable timeouts.
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
└────────┬─────────────┘
         │ implements
    ┌────┼──────────────────────┐
    │    │                      │
    ▼    ▼                      ▼
┌──────────┐  ┌──────────────┐  ┌────────────────┐
│ File     │  │ Smtp         │  │ LogOnly        │
│ .eml     │  │ SMTP+STARTTLS│  │ Logger.Warning │
│ Dev only │  │ Production   │  │ Safe fallback  │
└──────────┘  └──────────────┘  └────────────────┘
```

**Dependencies:**

- **Framework:** `Microsoft.AspNetCore.App` (for `IWebHostEnvironment`, `ILogger`, `IOptions`)
- **MIME & SMTP:** [MailKit](https://www.nuget.org/packages/MailKit) 4.17+ (from the [JakMail](https://github.com/jakmackie/JakMail) ecosystem)

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

```bash
dotnet add package VereinMail
```

Or add it to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="VereinMail" Version="1.0.0-dev" />
</ItemGroup>
```

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
| `{YourSection}.Smtp` | `Timeout` | int | `30` | Connection timeout in seconds |

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
      "Password": "smtp-password"
    }
  }
}
```

---

## Usage

### Registering the sender (in your application startup)

The library does not auto-register. Your application's startup code picks the transport based on the configuration mode:

```csharp
// Application startup — e.g., your WebAppStartup class
var mailOptions = configuration.GetValue<MailOptions>("Mail");
IEmailSender mailSender;

switch (mailOptions.Mode.ToLower())
{
    case "file":
        mailSender = new FileEmailSender(
            services.GetRequiredService<IOptions<MailOptions>>(),
            services.GetRequiredService<IWebHostEnvironment>(),
            services.GetRequiredService<ILogger<FileEmailSender>>());
        break;
    case "smtp":
        mailSender = new SmtpEmailSender(
            services.GetRequiredService<IOptions<MailOptions>>(),
            services.GetRequiredService<ILogger<SmtpEmailSender>>());
        break;
    default:
        mailSender = new LogOnlyEmailSender(
            services.GetRequiredService<ILogger<LogOnlyEmailSender>>());
}

// Register as singleton for other services to consume
services.AddSingleton<IEmailSender>(mailSender);
```

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
# Build the library
cd src/VereinMail
dotnet build

# Build in release mode
dotnet build --configuration Release

# Create a NuGet package
dotnet pack --configuration Release
```

**Requirements:**
- .NET SDK 8.0+
- NuGet dependencies resolved (run `dotnet restore` if needed)

---

## Project Structure

```
VereinMail/
├── README.md
├── LICENSE
├── CONTRIBUTING.md
├── .gitignore
└── src/
    └── VereinMail/
        ├── VereinMail.csproj    # Build configuration
        ├── IEmailSender.cs      # Interface (the contract)
        ├── FileEmailSender.cs   # .eml file transport (dev)
        ├── SmtpEmailSender.cs   # SMTP transport (production)
        ├── LogOnlyEmailSender.cs# Fallback transport (safe default)
        └── MailOptions.cs       # Configuration model
```

---

## License

MIT — see [LICENSE](LICENSE).

VereinMail is an independent, reusable email transport library for .NET applications.