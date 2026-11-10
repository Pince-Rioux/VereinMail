# VereinMail – Prüfbericht

Stand: 10.11.2026. Geprüft wurden Quellcode, Konfiguration, lokale Builds, Unit-Tests und CI. Keine SMTP-Verbindung, echten Zugangsdaten oder Produktionssysteme verwendet; kein vollständiges Sicherheits-Audit.

## Zusammenfassung

Die Bibliothek entsorgt den SMTP-Client jetzt zuverlässig, besitzt eine echte xUnit-Test-Suite und verwendet standardmäßig verpflichtendes STARTTLS. Die frühere Testlücke und das unsichere TLS-Default sind damit behoben. Die Zertifikatsprüfung kann weiterhin explizit abgeschaltet werden; das bleibt eine bewusste Entwicklungsoption und wird geloggt.

## Änderungen und Prüfungen

- **SMTP-Client:** `SmtpEmailSender` verwendet `using var client`, sodass die Verbindung auch bei Fehlern freigegeben wird.
- **TLS-Modus:** Neues `SmtpSecurityMode`-Enum mit `StartTls` (Default), `StartTlsWhenAvailable`, `SslOnConnect` und `None`. Der Sender verwendet den konfigurierten Modus; `StartTls` verhindert einen stillen Klartext-Fallback.
- **Zertifikatsprüfung:** `VerifyServerCertificate` bleibt standardmäßig `true`. Bei `false` wird eine Warnung geloggt und die Prüfung deaktiviert; diese Option ist nur für isolierte Entwicklungsumgebungen gedacht.
- **Tests:** `tests/VereinMail.Tests` ist jetzt ein echtes xUnit-Projekt mit `Microsoft.NET.Test.Sdk`, xUnit und Runner. Die bisherigen `MailOptions`-Tests haben Testattribute; zusätzlich gibt es einen Test für den fehlenden SMTP-Server.
- **Assertions:** Der frühere Referenzvergleich bei boxed Werten wurde durch `Assert.Equal` ersetzt.
- **README:** Konfigurationstabelle und Beispiel um `Security` und `VerifyServerCertificate` ergänzt; TLS-Default und Migrationshinweis dokumentiert.
- **CI:** Workflow führt Restore, Build und Test aus; `actions/checkout@v7`, `actions/setup-dotnet@v6`, Runner `ubuntu-24.04`.

## Test- und Buildstand

- `dotnet build src/VereinMail/VereinMail.csproj --no-restore`: **0 Warnungen, 0 Fehler**.
- `dotnet test tests/VereinMail.Tests/VereinMail.Tests.csproj --no-restore`: **8 bestanden, 0 fehlgeschlagen** (3 Fakten + 4 Theory-Fälle + 1 SMTP-Konfigurationstest).
- Kein echter SMTP-/TLS-Handshake, kein Versand und keine Zertifikatsfehler-Simulation ausgeführt.
- CI-Lauf nach dem Push: siehe GitHub Actions; der Workflow ist auf Testausführung umgestellt.

## Offene Probleme

1. **Mittel – Zertifikatsprüfung abschaltbar:** `VerifyServerCertificate=false` akzeptiert weiterhin jedes Serverzertifikat. Das ist bewusst als Entwicklungsoption erhalten, wird aber geloggt. Für Produktion sollte die Option entfernt oder über eine separate, klar als unsicher markierte Entwicklungsumgebung abgesichert werden.
2. **Opt-in unsichere Modi:** `StartTlsWhenAvailable` und `None` erlauben weiterhin Klartext, wenn sie explizit konfiguriert werden. Das ist für Legacy-Server dokumentiert, sollte aber nicht der Produktionsdefault sein.
3. **Testabdeckung:** Es gibt keinen Integrationstest gegen einen lokalen Fake-SMTP-Server; Erfolgs-/Fehlerpfad, TLS-Handshake und Zertifikatsablehnung sind nicht automatisiert nachgewiesen.
4. **Betrieb:** SMTP-Timeouts, Authentifizierung und Fehlerbehandlung wurden statisch geprüft, aber nicht gegen einen realen Server getestet.

## Breaking-Change-Hinweis

Der Default `Security = StartTls` ist strenger als das frühere `StartTlsWhenAvailable`. Server, die STARTTLS nicht anbieten, schlagen jetzt fehl. Für solche Legacy-Umgebungen muss `Security` explizit auf `StartTlsWhenAvailable` gesetzt werden. Das sollte bei der nächsten Versionsveröffentlichung als Verhaltensänderung dokumentiert werden.

## Ergebnis

Die TLS- und Testinfrastruktur ist deutlich verbessert: sicherer Default, echte Testsuite, warnungsfreier Build. Die verbleibenden Risiken betreffen bewusst konfigurierbare Ausnahmen und die fehlende Live-SMTP-Abdeckung. Keine pauschale Produktivfreigabe ohne Betriebsprüfung.
