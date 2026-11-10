# VereinMail – Prüfbericht

Stand: 10.11.2026. Geprüft wurden ausgewählte Quelltexte, lokale Builds, Testhelper und SMTP-Konfiguration statisch. Keine SMTP-Verbindung, echten Zugangsdaten oder Produktionssysteme verwendet; kein vollständiges Sicherheits-Audit.

## Änderungen und Ergebnisse

- `src/VereinMail/SmtpEmailSender.cs`: SMTP-Client wird mit `using var` auch bei Exceptions freigegeben. Die normale explizite Trennung bleibt bestehen.
- `tests/VereinMail.Tests/MailOptionsTests.cs`: Der Assertion-Helper vergleicht Werte mit `object.Equals` statt Referenzen. `coder-1` implementierte, `coder-2` prüfte unabhängig; `qa` prüfte vor und nach der Korrektur; `security` prüfte statisch.
- Library- und Testprojekt-Build mit SDK 8 und `--no-restore`: erfolgreich, jeweils 0 Fehler und 0 Warnungen.
- Vor der Korrektur scheiterten beim manuellen Aufruf 2 der 3 vorhandenen Testmethoden wegen des Referenzvergleichs bei boxed Werten; nach der Korrektur bestanden alle drei. Bei einer unabhängigen Nachprüfung mit synthetischem projektspezifischem Harness bestanden **18/18 Prüfungen** inklusive Gleich-/Ungleich- und Nullfällen.
- **Wichtig:** `dotnet test` beendet sich erfolgreich, entdeckt aber **0 Tests**. Der Test-SDK, ein Framework/Adapter und Testattribute fehlen. SMTP-Senden, TLS-Handshake und Dispose-Verhalten wurden nicht zur Laufzeit getestet. Der temporäre Test-Harness ist kein Ersatz für eine dauerhafte automatisierte Testsuite.

## Offene Probleme

1. **Hoch – TLS nicht verpflichtend:** `src/VereinMail/SmtpEmailSender.cs:43–47` verwendet `StartTlsWhenAvailable`. Wenn STARTTLS nicht angeboten oder unterdrückt wird, kann die Verbindung ohne TLS erfolgen; je nach SMTP-Server und Authentifizierung können Nachrichten bzw. Zugangsdaten gefährdet sein. STARTTLS verpflichtend machen oder korrekt konfiguriertes SSL-on-connect verwenden, ohne Klartext-Fallback.
2. **Hoch bei unsicherer Konfiguration – Zertifikatsprüfung abschaltbar:** `SmtpEmailSender.cs:39–41` und `MailOptions.cs:44`. Der Standard `VerifyServerCertificate=true` ist sicherer; bei explizitem `false` akzeptiert der Callback jedes Zertifikat. Abschaltung in Produktion entfernen oder strikt auf isolierte Entwicklung begrenzen.
3. **Testinfrastruktur:** `tests/VereinMail.Tests/VereinMail.Tests.csproj` referenziert keinen Test-SDK oder Adapter; Methoden in `MailOptionsTests.cs` haben keine Testattribute. CI baut die Bibliothek, statt diese Tests auszuführen. Ein echtes Testframework und einen CI-Testlauf einrichten und sicherstellen, dass null entdeckte Tests nicht als Erfolg interpretiert werden.
4. **Fehlende Abdeckung:** SMTP-Erfolg und -Fehler, Zertifikatsprüfung und Ressourcenfreigabe sind nicht automatisiert nachgewiesen. Mit kontrolliertem Fake-Transport bzw. isoliertem Testserver testen.

## Ergebnis

Die beiden kleinen Änderungen wurden unabhängig geprüft; der Assertion-Fix besteht synthetische Prüfungen. Die bestehende Testlücke und die SMTP-TLS-Risiken bleiben offen; keine Produktivfreigabe aus diesem Bericht ableiten.
