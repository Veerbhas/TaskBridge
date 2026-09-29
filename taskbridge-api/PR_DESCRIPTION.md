# Pull Request Description

## Summary

Adds a .NET 10 TaskBridge API with tenant-scoped project operations, project audit recording, in-app team notifications, audit-history filtering, and notification inbox/read endpoints.

## Changes

- Refactors project persistence behind repositories and a unit of work; organization identity comes from JWT claims.
- Adds team and team-membership validation and tenant-aware SQL keys.
- Records immutable audit snapshots for project create/status-update/delete and queues notifications for team members in the same EF commit.
- Adds authenticated Minimal API routes for project, audit, and notification operations with DTO validation, policy authorization, Problem Details, Serilog, rate limiting, and OpenAPI.
- Adds migrations, REST Client examples, documentation, and service-level tests.

## Verification

- `dotnet test TaskBridge.sln`: 23 passing tests in the latest recorded run.
- Build and diagnostics completed without errors in the latest recorded verification.
- Idempotent SQL migration script was generated and inspected; it was not applied to a live SQL Server.

## Remaining work / risks

- Implement a milestone domain workflow that automatically records milestone old/new state and dispatches notifications.
- Restrict `AuditWriter` to approved internal service identities at deployment.
- Validate required identity claims and deployment configuration against the real identity provider.
- Run SQL Server migration/trigger and API auth/policy integration tests in a disposable test environment.
- Configure trusted reverse-proxy forwarding before attributing client IP addresses.
