# TaskBridge

## TaskBridge API

The API uses **.NET 10, ASP.NET Core Minimal APIs, EF Core, and SQL Server**. JWT bearer authentication and role-based policies protect all routes. FluentValidation handles request validation, Serilog provides structured logging, and built-in OpenAPI is exposed in Development.

### Run

Configure these environment variables before starting the API:

- `ConnectionStrings__TaskBridge`: SQL Server connection string.
- `Authentication__Authority`: HTTPS issuer/authority URL for JWT metadata.
- `Authentication__Audience`: expected JWT audience.

The identity provider must issue `sub`, `organizationId`, and `role` claims. Project reads require the `ProjectReader` role; writes require `ProjectWriter`. Do not pass the organization ID in request data; it is derived from the validated token.

```powershell
dotnet run --project taskbridge-api/TaskBridge.Api.csproj
dotnet test taskbridge-api/tests/TaskBridge.Api.Tests.csproj
```

The API does not automatically migrate the database at startup. The initial schema is in `taskbridge-api/src/Data/Migrations/`. Restore the repository-local EF tool and apply migrations explicitly:

```powershell
dotnet tool restore --tool-manifest taskbridge-api/.config/dotnet-tools.json
dotnet ef database update --project taskbridge-api/TaskBridge.Api.csproj --startup-project taskbridge-api/TaskBridge.Api.csproj
```

Package versions are centralized in `taskbridge-api/Directory.Packages.props`; NuGet Audit remains enabled during restore. The host applies a per-user/IP request limit of 120 requests per minute. When running behind a reverse proxy, configure only trusted forwarded headers so rate limiting and audit IP attribution use the intended client address.

Teams and their memberships must be provisioned by the owning team-management system. Project creation verifies that the team belongs to the current organization, and notifications go only to that organization's registered team members.

Project create, status-update, and delete operations write audit records and queue in-app notifications for all provisioned team members. Audit history and notification inbox/read endpoints are implemented. External email/push delivery and milestone-specific workflows remain assessment work. The test database uses EF Core InMemory and does not replace SQL Server integration testing.

See [the project service documentation](taskbridge-api/PROJECT_SERVICE.md) for method behavior, tenant scoping, audit guarantees, and HTTP mappings.