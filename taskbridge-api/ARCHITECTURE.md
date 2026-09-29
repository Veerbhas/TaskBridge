# Architecture
TaskBridge.Api is a .NET 10 Minimal API backed by EF Core and SQL Server.
Inbound flow is HTTP request -> JWT authentication/policy -> endpoint DTO validation -> service -> repository -> DbContext -> SQL Server.
`src/projects/Endpoints/` and `src/notifications/Endpoints/` are the HTTP adapter layer; MVC Controllers are not used.
`ProjectService` coordinates project persistence with `IAuditService` and `INotificationService`.
The integration contract is `RecordProjectEvent(...)` plus `QueueProjectEventAsync(...)`; actor and organization context come from `ICurrentActorContext`.
Create, status-update, and delete stage project changes, audit snapshots, and team notifications before one `IUnitOfWork.CommitAsync`.
`AuditRepository` and `NotificationRepository` share the request-scoped `TaskBridgeDbContext`, keeping those writes atomic.
Notification recipients are selected by both `TeamId` and `OrganizationId`; inbox/read operations also require the JWT `sub` recipient.
For multi-tenant B2B isolation, repositories derive organization scope from validated claims, never request-supplied tenant IDs.
Composite organization/team keys prevent projects or memberships from referencing another organization's team.
Audit rows have no cascading project foreign key, so history survives project hard deletion; EF and SQL Server enforce immutability.
Minimal APIs were chosen over MVC Controllers to keep route adapters small; services/repositories retain the same layered boundaries.
Synchronous database notification fan-out favors atomic consistency, trading throughput for simpler delivery; external delivery should use an outbox.
Milestone event names are accepted by audit logic, but no milestone transition service yet coordinates state changes, audit, and notifications.
External email/push delivery and SQL Server/authentication integration tests remain outside the current implementation.
