# VereinMail

> A lightweight, dependency-injected email transport library for .NET applications.

VereinMail provides a clean `IEmailSender` interface and three ready-to-use implementations — **File** (`.eml` for development), **SMTP** (production), and **Log** (safe fallback). It follows the dependency-inversion principle: consumers depend only on the interface; wiring the transport is the application's job.

It was extracted from the [VereinMember](https://github.com/Pince-Rioux/VereinMember) membership module and is designed to be reused by any .NET project in the ClubSite ecosystem (and beyond).

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
- **Configuration-driven** — `MailOptions` bound from the application config (e.g., `MembershipMail` section). No hardcoded settings.
- **Zero static state** — all implementations are injectable and testable.
- **No assumptions about callers** — the interface carries no club- or membership-specific types.

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

> **Note:** The package is currently published to the `LocalPackages` source. See the [publishing guide](PUBLISHING.md) for GitHub Packages setup.

---

## Configuration

All settings are defined in `MailOptions` and bound from a configuration section — typically `MembershipMail` in an `AppSettings.json` file.

| Section | Key | Type | Default | Description |
|---|---|---|---|---|
| `MembershipMail` | `Mode` | string | `"Log"` | Transport: `"File"`, `"Smtp"`, or `"Log"` |
| `MembershipMail.DefaultFrom` | `Name` | string | `"VereinMail"` | Display name for the sender |
| `MembershipMail.DefaultFrom` | `Email` | string | `"noreply@localhost"` | Sender email address |
| `MembershipMail` | `Organization` | string | *(none)* | Optional `Organization` MIME header |
| `MembershipMail.File` | `Path` | string | `"maildrop"` | Directory for `.eml` files (absolute or relative to content root) |
| `MembershipMail.Smtp` | `Server` | string | `""` | SMTP hostname |
| `MembershipMail.Smtp` | `Port` | int | `587` | SMTP port |
| `MembershipMail.Smtp` | `Username` | string | `""` | SMTP login (optional) |
| `MembershipMail.Smtp` | `Password` | string | `""` | SMTP password (optional) |
| `MembershipMail.Smtp` | `Timeout` | int | `30` | Connection timeout in seconds |

### Example configuration

```json
{
  "MembershipMail": {
    "Mode": "Smtp",
    "DefaultFrom": {
      "Name": "VC Neusäß Mitgliederverwaltung",
      "Email": "mitglied@volleyballclub.de"
    },
    "Organization": "Volleyballclub Neusäß e.V.",
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
// WebAppStartup.cs — or equivalent
services.Configure<MembershipMailOptions>(configuration);

var mailOptions = configuration.GetValue<MailOptions>("MembershipMail");
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
public class MembershipService
{
    private readonly IEmailSender _mailSender;

    public MembershipService(IEmailSender mailSender)
    {
        _mailSender = mailSender;
    }

    public async Task NotifyMember(string email, string name)
    {
        await _mailSender.SendAsync(
            email,
            "Willkommen bei VC Neusäß",
            $"Hallo {name},\n\nwillkommen im Verein!\n\nMit sportlichen Grüßen\nDein VC Neusäß"
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
- .NET SDK 8.0+ (or whatever `TargetFramework` is set to)
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

VereinMail is developed for the ClubSite ecosystem of VC Neusäß, but is intentionally decoupled and reusable by any .NET project.