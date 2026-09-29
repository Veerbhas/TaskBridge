# Contributor Instructions

The authoritative coding, architecture, security, and testing rules are in [`.github/copilot-instructions.md`](.github/copilot-instructions.md). Read that guide before changing code; do not copy divergent standards into this file.

## Quick checks

- Target .NET 10 and follow the existing Minimal API style; route handlers belong under `src/**/Endpoints/`.
- Keep business rules in services and persistence in repositories. Do not inject `DbContext` into endpoints.
- Derive `OrganizationId` and user identity from validated JWT claims, never request data. Scope every tenant-owned query and mutation.
- Use DTOs and FluentValidation at HTTP boundaries. Never return EF entities from API endpoints.
- Keep audit events immutable. Stage project changes, audit rows, and in-app notifications in one unit of work.
- Add/update tests for behavior changes. Run `dotnet test TaskBridge.sln` before completion and report any environment-dependent checks that were not run.
- Treat AI-generated code as a draft requiring human review, especially for tenant isolation, authorization, migrations, audit retention, and compliance.
