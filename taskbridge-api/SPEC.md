# TaskBridge API Specification

## Purpose and status

This specification describes the Notification and Audit Service assessment and identifies implemented behavior versus remaining milestone work. The API is .NET 10 Minimal APIs with EF Core and SQL Server. **Process note:** this document was formalized after initial implementation work, so it records and clarifies the implementation contract but does not satisfy the assessment's requirement to approve a spec before coding.

## Functional requirements

| Capability | Current status |
|---|---|
| Create, read, list by team, update status, and delete projects | Implemented |
| Keep project access scoped to the authenticated organization | Implemented in services/repositories |
| Create immutable audit records for project create/update/delete | Implemented in the same EF unit of work as the project change |
| Search project audit history by date and event type | Implemented; `to` is exclusive |
| Generate in-app notifications for all provisioned project team members on project create/update/delete | Implemented |
| Retrieve a user's unread notifications and mark them read | Implemented; recipient is bound to JWT `sub` |
| Automatically capture milestone created/updated/closed/deleted/reopened transitions, state snapshots, audit, and notifications | Not implemented; internal audit event recording supports the event names but no milestone domain workflow exists |
| External email/push delivery | Not implemented |

## Data models

- **Project:** `Id: Guid`, `Name: string` (required, maximum 200 characters), `Status: ProjectStatus`, `TeamId: Guid`, `OrganizationId: Guid`, `CreatedAt: DateTimeOffset`, `UpdatedAt: DateTimeOffset`.
- **AuditEntry:** `Id: Guid`, `ProjectId: Guid`, `EntityType: string`, `EventType: string`, `ActorUserId: string`, `OrganizationId: Guid`, `PreviousState: string?`, `NewState: string?`, `Timestamp: DateTimeOffset`, `ActorIpAddress: string?`. Audit entries are immutable and retained independently of hard-deleted projects.
- **Notification:** `Id: Guid`, `RecipientUserId: string`, `ProjectId: Guid`, `EventType: string`, `Message: string`, `IsRead: bool`, `CreatedAt: DateTimeOffset`, `ReadAt: DateTimeOffset?`, `OrganizationId: Guid`.
- **Team:** `Id: Guid`, `OrganizationId: Guid`. **TeamMember:** `TeamId: Guid`, `OrganizationId: Guid`, `UserId: string`. These organization-scoped records validate team ownership and determine notification recipients; team data is provisioned by its owning system.

## API contracts

All endpoints require validated JWT bearer authentication. Request and response bodies use DTOs; EF entities are not returned directly.

| Endpoint | Request/filters | Response |
|---|---|---|
| `POST /projects/` | `{ "name": string, "teamId": Guid }` | `201` with `ProjectResponse`; rejects invalid input and teams outside the caller's organization |
| `GET /projects/{projectId}` | Route `projectId: Guid` | `200` with `ProjectResponse` or `404` |
| `GET /projects/team/{teamId}` | Route `teamId: Guid` | `200` with `ProjectResponse[]`, tenant scoped |
| `PATCH /projects/{projectId}/status` | `{ "status": ProjectStatus }` | `200` with `ProjectResponse`, `400` validation problem, or `404` |
| `DELETE /projects/{projectId}` | Route `projectId: Guid` | `204` or `404` |
| `POST /audit` | `{ "projectId": Guid, "eventType": string, "previousState": string?, "newState": string? }` | Internal `AuditWriter` operation; `201` with `AuditEntryResponse`, `400`, or `404`. Actor, organization, timestamp, and IP are server-derived. |
| `GET /audit/{projectId}` | Optional `from: DateTimeOffset`, `to: DateTimeOffset`, `eventType: string` | `200` with `AuditEntryResponse[]`, scoped by organization; `from < to` when both supplied |
| `GET /notifications/{userId}` | Route `userId: string`, must equal JWT `sub` | `200` with unread `NotificationResponse[]` or `403` |
| `PATCH /notifications/{notificationId}/read` | Route `notificationId: Guid`; no body | `204` or `404` outside the caller's recipient/tenant scope |

Response DTO shapes (JSON uses camel case):

```text
ProjectResponse = { id: Guid, name: string, status: ProjectStatus, teamId: Guid,
					createdAt: DateTimeOffset, updatedAt: DateTimeOffset }
AuditEntryResponse = { id: Guid, projectId: Guid, entityType: string, eventType: string,
					   actorUserId: string, previousState: string?, newState: string?,
					   timestamp: DateTimeOffset, actorIpAddress: string? }
NotificationResponse = { id: Guid, projectId: Guid, eventType: string, message: string,
						 isRead: bool, createdAt: DateTimeOffset, readAt: DateTimeOffset? }
```

Enums are serialized as strings. Invalid request bodies and audit date filters return validation Problem Details.

## Project Service integration points

`ProjectService` is the orchestration point for project lifecycle events. On create, status update, or delete it calls `IAuditService.RecordProjectEvent` and `INotificationService.QueueProjectEventAsync`, then commits the project mutation, audit entry, and queued in-app notifications through the same scoped `TaskBridgeDbContext`/unit of work. The notification service obtains recipients from `TeamMemberRepository` filtered by both `TeamId` and `OrganizationId`. The actor context supplies organization, user subject, and observed IP from the authenticated request.

The internal `POST /audit` endpoint calls `IAuditService.RecordInternalEventAsync` for trusted internal callers. It currently records milestone event names, but it is not called by a milestone domain service: automatic milestone state transitions, snapshots, and notification dispatch remain unimplemented.

Project policies: `ProjectReader`, `ProjectWriter`. Audit policies: `AuditReader`, `AuditWriter`. Notification policy: `NotificationReader`.

## Security and validation constraints

- Obtain `OrganizationId` and actor identity only from validated JWT claims, never request payloads.
- Scope every project, audit, membership, and notification query by organization; notification reads and writes also scope by JWT subject.
- Grant `AuditWriter` only to trusted internal service identities. The application currently enforces the role policy; deployments must ensure only approved clients receive that role.
- Validate requests using FluentValidation. Reject invalid/empty IDs, unsupported event types, names over 200 characters, state snapshots over 10,000 characters, and invalid date ranges.
- Use safe Problem Details responses; do not expose exception internals or log credentials/tokens.
- Audit records can be created/read/searched/filtered but cannot be updated or deleted. EF checks tracked changes and SQL Server migration installs a trigger.

## Copilot usage notes

Copilot Agent Mode assisted with scaffolding, refactoring, endpoint wiring, tests, and documentation. Human review was required for tenant boundaries, event semantics, audit immutability, internal role restrictions, migration safety, and the distinction between an internal audit endpoint and project-service transactional audit writes. See `PROMPTS.md` and `REVIEW.md`.
