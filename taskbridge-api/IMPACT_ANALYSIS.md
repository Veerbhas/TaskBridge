# Scope Change — Impact Analysis

## Change request

> Add a new milestone event type: `MILESTONE_REOPENED`. This should trigger audit logging and notifications. Audit entries must also capture the actor's IP address.

**Timing note:** This analysis is being recorded after implementation work has started. It cannot retroactively satisfy the assessment instruction to document impact before coding. In the current code, `MILESTONE_REOPENED` is accepted by the audit event allow-list and `AuditEntry.ActorIpAddress` is already modeled, but a milestone transition does not yet trigger the complete audit-and-notification workflow.

## Affected files, modules, and compatibility

| File/module | Required or existing change | Compatibility / migration classification |
|---|---|---|
| `src/notifications/Models/AuditEntry.cs` | Add/store nullable `ActorIpAddress: string?` (maximum 45 characters for IPv4/IPv6 text); preserve immutable audit behavior | Additive model change. Existing constructors/callers must provide the optional value or a compatible overload/default. |
| `src/TaskBridgeDbContext.cs` | Map `ActorIpAddress` as nullable `nvarchar(45)`; retain audit immutability enforcement | Additive schema mapping; no endpoint contract break. |
| `src/Data/Migrations/20260929121754_InitialCreate.cs` and model snapshot | Current initial migration already creates nullable `ActorIpAddress`; verify the deployed migration history | No new migration is needed for databases created from this migration. A database on an older baseline needs a forward migration before code requiring the column is deployed. |
| `src/notifications/Services/AuditService.cs` | Permit `MILESTONE_REOPENED`; create audit events with actor, organization, timestamp, old/new state, and IP from the trusted actor context | Additive event value. It must not accept actor/tenant/IP identity from the request body. |
| `src/notifications/Endpoints/NotificationAuditContracts.cs` and `NotificationAuditEndpoints.cs` | Validate and expose the internal audit event contract; audit response includes actor IP | Event type is additive; response adds a nullable field, which is normally backward-compatible for JSON consumers that ignore unknown fields. `AuditWriter` must remain restricted to trusted services. |
| `src/notifications/Services/NotificationService.cs` and `INotificationService.cs` | Dispatch `MILESTONE_REOPENED` notifications to organization-scoped team members as part of the milestone transition | Additive event behavior, but creates new notification side effects and delivery volume. Current project event fan-out does not implement milestone dispatch. |
| Milestone domain/repository/service (not present) | Add the actual reopen state transition, validation, previous/new snapshots, and transactional event orchestration | New module/API work; no existing milestone request contract to preserve. Define it before implementation. |
| `src/notifications/Repositories/` | Reuse tenant-scoped audit and notification persistence; add milestone persistence only if the domain is introduced | Additive if a milestone repository/table is needed; determine schema from the approved model. |
| `tests/NotificationAuditTests.cs` and project tests | Verify reopen transition, audit snapshot and actor/IP attribution, team-wide notification fan-out, immutability, and tenant isolation | Additive test coverage; include SQL Server integration coverage for schema/trigger behavior. |
| `TaskBridge.Api.http`, `SPEC.md`, `README.md`, and `PROJECT_SERVICE.md` | Document any new milestone endpoint/event contract and expected responses | Documentation-only; update only after API and domain contracts are agreed. |

### Compatibility and migration summary

- Adding `MILESTONE_REOPENED` to the existing string event type is additive and does not by itself require a database migration.
- `ActorIpAddress` is nullable and already exists in this repository's `InitialCreate` migration. No additional migration is needed for a database created from that migration; an older deployed schema requires a forward migration before rollout.
- Persisting a new milestone entity/table would require an additive migration. No request breaking change is currently specified. Adding `actorIpAddress` to audit responses is additive for tolerant JSON clients, but strict-schema consumers must be checked.

## Integration and implementation approach

The milestone state transition should be owned by a milestone service, not simulated by posting directly to `/audit`. The service should:

1. Load the milestone using both its ID and the authenticated actor's `OrganizationId`; verify the actor is authorized to reopen it.
2. Validate that the current state permits reopening and capture the complete previous state.
3. Change and persist the milestone state, capture the new state, and stage an immutable `MILESTONE_REOPENED` audit entry.
4. Find recipients from team membership filtered by the same organization and stage notifications for them.
5. Commit the milestone, audit row, and in-app notifications atomically. Use an outbox if delivery becomes asynchronous or external.
6. Return the agreed DTO/status code; do not return EF entities or accept `ActorUserId`, `OrganizationId`, or `ActorIpAddress` from request data.

## Security and compliance risks

- **Personal data:** IP addresses can identify or be linkable to individuals. Establish a lawful purpose, notice, access policy, retention period, deletion/hold rules, and jurisdiction-specific compliance review before production use.
- **Collection accuracy:** `HttpContext.Connection.RemoteIpAddress` may be the reverse proxy address. Only honor forwarded IP headers after configuring known/trusted proxies; never trust arbitrary client-supplied forwarding headers.
- **Exposure:** Restrict audit access by tenant and policy. Do not include IP addresses in ordinary application/request logs, notification messages, or state snapshots. Redact them from diagnostics and support exports unless specifically authorized.
- **Storage and retention:** Keep the audit field nullable for background/system events without a request IP. Preserve audit immutability, protect backups, define retention, and avoid accidental export to analytics systems.
- **Tenant integrity:** Derive actor and organization from the validated JWT. Use the same organization scope for milestone lookup, audit, team membership, and notification writes.
- **Service trust:** `POST /audit` is role-protected; ensure only approved service identities receive `AuditWriter`. Prefer the milestone service's internal audit service call for atomic state-change processing.

## Recommended sequence

1. Confirm milestone states, reopen authorization, event payload, recipient rules, and IP purpose/retention with product, security, and compliance stakeholders.
2. Define the milestone entity and DTO/API contract; decide whether the milestone belongs in this service or an upstream Project Service.
3. Implement tenant-scoped repository and transition service with old/new snapshots, audit and notification staging, and one transaction/outbox boundary.
4. Verify `ActorIpAddress` is present in the target database. If absent, create a forward nullable-column migration; do not edit an already-applied migration or auto-migrate on startup.
5. Add unit and API integration tests for reopen authorization, valid/invalid transitions, actor/IP attribution, previous/new state, notification fan-out, tenant isolation, retries, and immutable audit storage.
6. Review generated SQL, test migration up/down on a disposable SQL Server database, deploy schema before application code, and monitor notification/audit volume.
7. Update API examples, specification, operational retention policy, and release notes.

## How Copilot Assisted This Analysis

**Prompt:** “Add a new milestone event type: `MILESTONE_REOPENED`. This should trigger audit logging and notifications. Audit entries must also capture the actor's IP address.”

Copilot helped enumerate affected model, service, endpoint, repository, migration, test, security, and documentation areas. It identified the need for nullable IP storage, a trusted actor context, organization-scoped recipients, and immutable audit persistence. Human review verified the actual migration and source: the IP column already exists in the initial migration and the audit allow-list accepts the event, but there is no milestone model/transition service and notifications are not triggered by milestone events. Human judgment is also required for IP retention/privacy, trusted proxy configuration, reopen authorization, transaction/outbox design, and whether this change belongs in the Project Service or this API.
