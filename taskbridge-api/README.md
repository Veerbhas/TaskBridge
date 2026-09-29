# TaskBridge API

TaskBridge API is a .NET 10 ASP.NET Core Minimal API using EF Core and SQL Server. The implemented scope includes tenant-scoped project operations, immutable project audit records, in-app notifications, audit history queries, JWT authentication, FluentValidation, and Serilog.

## Run and test

Configure these environment variables before running the API:

- `ConnectionStrings__TaskBridge`: SQL Server connection string.
- `Authentication__Authority`: HTTPS JWT authority/issuer metadata endpoint.
- `Authentication__Audience`: expected JWT audience.

The identity provider must issue `sub`, `organizationId`, and `role` claims. The current actor context derives user and organization identity from `sub` and `organizationId`; do not accept tenant identity from request bodies.

From the workspace root:

```powershell
dotnet run --project taskbridge-api/TaskBridge.Api.csproj
dotnet test taskbridge-api/TaskBridge.sln
```

Teams and team memberships must be provisioned by the owning system before project notifications can be sent. The API does not automatically apply EF migrations. After configuring a SQL Server connection, restore the local EF tool and apply migrations deliberately:

```powershell
dotnet tool restore --tool-manifest taskbridge-api/.config/dotnet-tools.json
dotnet ef database update --project taskbridge-api/TaskBridge.Api.csproj --startup-project taskbridge-api/TaskBridge.Api.csproj
```

## Routes

| Method and route | Policy | Purpose |
|---|---|---|
| `POST /projects/` | `ProjectWriter` | Create a project and emit audit/notification events |
| `GET /projects/{projectId}` | `ProjectReader` | Get a project in the current organization |
| `GET /projects/team/{teamId}` | `ProjectReader` | List projects for an organization-scoped team |
| `PATCH /projects/{projectId}/status` | `ProjectWriter` | Update project status and emit events |
| `DELETE /projects/{projectId}` | `ProjectWriter` | Delete a project and emit events |
| `POST /audit` | `AuditWriter` | Record an internal audit event |
| `GET /audit/{projectId}?from=&to=&eventType=` | `AuditReader` | Query tenant-scoped immutable audit history; `to` is exclusive |
| `GET /notifications/{userId}` | `NotificationReader` | Get unread notifications for the JWT subject |
| `PATCH /notifications/{notificationId}/read` | `NotificationReader` | Mark the caller's notification as read |

The current notification fan-out covers project create, status update, and delete events. A milestone domain workflow and email/push delivery are not implemented. Tests use EF Core InMemory; production SQL Server migration and trigger behavior still need database-backed verification.

## Documentation

- [Specification](SPEC.md)
- [Architecture](ARCHITECTURE.md)
- [Implementation review](REVIEW.md)
- [Impact analysis](IMPACT_ANALYSIS.md)
- [Copilot prompt log](PROMPTS.md)
- [Tool strategy](TOOL_STRATEGY.md)
- [Pull request description](PR_DESCRIPTION.md)
- [Project service details](PROJECT_SERVICE.md)
- [Contributor instructions](instructions.md)
