# Project Service

`ProjectService` implements `IProjectService` and coordinates project rules, repositories, the current authenticated actor, audit writes, notification fan-out, and a single unit-of-work commit. Persistence stays in repositories; the HTTP layer maps service results to response DTOs.

## Tenant and Actor Context

The service does not accept an organization ID from callers. `ICurrentActorContext` supplies the organization and user IDs from the authenticated request's validated claims. All project reads are scoped by that organization. Project creation also verifies that the selected team belongs to it. Actor ID and the observed remote IP address are attached to audit records.

Do not trust a client-provided organization ID. When deployed behind a reverse proxy, only use client IP information after configuring trusted forwarded headers.

## Methods

### `CreateAsync(name, teamId, cancellationToken)`

Creates a project in the current actor's organization. The team must exist in the same organization or the method throws `KeyNotFoundException`. The `Project` constructor trims the name and enforces the non-empty and 200-character rules. The service stages a `PROJECT_CREATED` audit entry and notifications for all team members, then commits the project, audit row, and notifications together. It returns the created project after persistence succeeds.

### `GetByIdAsync(projectId, cancellationToken)`

Looks up a project by ID within the current organization. Returns `null` when the project is missing or belongs to another organization. It does not return or disclose cross-tenant records.

### `GetByTeamAsync(teamId, cancellationToken)`

Returns projects for the given team, filtered to the current organization and ordered by project name. A team from another organization cannot expose its projects.

### `UpdateStatusAsync(projectId, status, cancellationToken)`

Finds the project within the current organization. Returns `null` if it is missing or belongs to another organization. If the requested status is unchanged, it returns the project without writing an audit event. Otherwise, it captures the previous state, validates and changes the status, records a `PROJECT_UPDATED` audit entry containing previous and new snapshots, queues team notifications, and commits the changes together. Invalid enum values are rejected by the domain method.

### `DeleteAsync(projectId, cancellationToken)`

Finds the project within the current organization. Returns `false` if it is missing or belongs to another organization. Otherwise it stages a `PROJECT_DELETED` audit entry containing the pre-delete snapshot, queues team notifications, removes the project, and commits all operations together. This is a hard delete of the project; audit and notification records remain stored.

## Audit and Persistence Guarantees

- Audit snapshots serialize enum values as names and include project ID, name, status, and team ID.
- Audit entries include project ID, event type, actor user ID, organization ID, previous/new state, timestamp, and actor IP address.
- Project changes, audit entries, and notifications are staged through the same EF Core context and committed once, avoiding partial event handling.
- EF Core rejects tracked audit updates/deletes, and the SQL Server migration installs a trigger to reject direct SQL updates/deletes.
- Logs use structured message templates and include project and organization identifiers. Request bodies, credentials, and tokens are not logged.

## HTTP Mapping

| Operation | Route | Policy | Success | Missing resource |
|---|---|---|---|---|
| Create | `POST /projects/` | `ProjectWriter` | `201 Created` with `Location` | `404` if team is absent from the actor's organization |
| Get by ID | `GET /projects/{projectId}` | `ProjectReader` | `200 OK` | `404 Not Found` |
| Get by team | `GET /projects/team/{teamId}` | `ProjectReader` | `200 OK` with a list | Empty list |
| Update status | `PATCH /projects/{projectId}/status` | `ProjectWriter` | `200 OK` | `404 Not Found` |
| Delete | `DELETE /projects/{projectId}` | `ProjectWriter` | `204 No Content` | `404 Not Found` |
| Internal audit write | `POST /audit` | `AuditWriter` | `201 Created` | `404 Not Found` for a project outside the actor's organization |
| Audit history | `GET /audit/{projectId}?from=&to=&eventType=` | `AuditReader` | `200 OK` with matching events | Empty list for no visible events |
| Unread inbox | `GET /notifications/{userId}` | `NotificationReader` | `200 OK` for the JWT subject | `403 Forbidden` if `userId` differs from `sub` |
| Mark notification read | `PATCH /notifications/{notificationId}/read` | `NotificationReader` | `204 No Content` | `404 Not Found` outside recipient/tenant scope |

Request payloads are validated by FluentValidation. The API returns validation failures as `400` Problem Details responses. Domain entities are mapped to `ProjectResponse` and are not returned directly by the endpoints.

`POST /audit` is reserved for trusted internal callers. Only grant `AuditWriter` to approved service identities; actor, organization, and IP attribution are always taken from the authenticated request context, never from the body.

## Verification

`tests/ProjectServiceTests.cs` and `tests/NotificationAuditTests.cs` cover project audit creation, foreign-team rejection, organization-scoped lookup, status snapshots, deletion auditing, team-wide notification delivery, recipient/tenant isolation, mark-read ownership, audit date/event filters, audit immutability, and validation. These tests use EF Core InMemory; SQL Server migrations and trigger behavior still require verification against a SQL Server instance.
