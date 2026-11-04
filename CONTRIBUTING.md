# Contributing to VereinMail

Thanks for your interest! VereinMail is a small, focused library — contributions that respect its scope are very welcome.

## Scope

VereinMail provides the **email transport** layer only. It deliberately does **not** include:

- HTML email templates
- Mail-merge or batch-send logic
- Queue/persistence of pending messages
- Any club- or membership-specific types

If your feature fits outside this scope, consider a downstream project (e.g., VereinMember).

## Getting Started

1. Fork the repository.
2. Create a feature branch from `main`:
   ```
   git checkout -b feature/your-feature-name
   ```
3. Make your changes.
4. Ensure the library still builds:
   ```
   dotnet build
   ```

## Pull Request Guidelines

- Keep changes minimal and focused on one concern.
- Use the same coding style as the existing code (nullable enabled, implicit usings, .NET naming conventions).
- Add or update documentation in `README.md` if the public API changes.
- Verify that `dotnet build` succeeds with 0 errors and 0 warnings.
- Use a descriptive commit message (imperative mood, <= 72 chars title, body with motivation if needed).

## Code of Conduct

Be respectful. This is a hobby project run by volunteers.

## Questions?

Open a [discussion](https://github.com/Pince-Rioux/VereinMail/discussions) or an issue.