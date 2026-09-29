# TaskBridge Assessment Implementation Guide (.NET 10)

## Overview
This document serves as the master implementation guide for the TaskBridge Notification & Audit Service assessment. It defines architecture, technology choices, coding standards, security requirements, testing standards, documentation expectations, and GitHub Copilot usage guidelines.

All development work must align with this document unless explicitly overridden by assessment requirements.
---


# Technology Stack

## Platform
- .NET 10
- ASP.NET Core Web API

## Data Access
- Entity Framework Core
- SQL Server

## Validation
- FluentValidation

## Authentication & Authorization
- JWT Bearer Authentication
- Policy-Based Authorization

## Logging
- Serilog

## Documentation
- Swagger / OpenAPI

## Testing
- xUnit
- Moq
- FluentAssertions

## AI Tooling
- GitHub Copilot
- GitHub Copilot Chat

---


## Architecture Conventions

- Use the layered flow `Controller -> Service -> Repository -> Entity Framework Core -> SQL Server`.
- Keep feature code under `src/projects/` and `src/notifications/`, organized into the established `Models/`, `Repositories/`, `Services/`, `Controllers/`, and `DTOs/` areas where applicable. Keep audit functionality in the notification/audit feature unless the solution establishes a dedicated audit feature.
- Controllers handle HTTP concerns only: bind and validate DTOs, obtain authenticated-user context, call services, and produce the appropriate response. Controllers must not access `DbContext` or contain business rules.
- Services own business rules and coordinate repositories and feature workflows. Repositories contain persistence operations only; they must not make authorization or business-policy decisions.
- Use DTOs for every API request and response. Keep EF Core entities internal to the data/domain layers and never serialize or return entities directly.
- Keep dependencies explicit and avoid circular references. Treat files labeled `AI-generated, unreviewed` as drafts; review their design, security, and tests before relying on them.

## Coding Standards

- Follow standard C# naming: `PascalCase` for types, methods, properties, and constants; `I`-prefixed `PascalCase` for interfaces; `camelCase` for parameters and locals; and `_camelCase` for private fields. Use the existing feature and folder naming conventions.
- Enable nullable reference types and address nullability warnings deliberately. Use explicit domain types and enums instead of loosely typed values; avoid unnecessary suppressions, `dynamic`, and unsafe casts.
- Name asynchronous methods with the `Async` suffix, use `async`/`await` appropriately, and propagate `CancellationToken` through request-driven asynchronous service and repository operations. Do not block on asynchronous work or swallow exceptions.
- Use FluentValidation for request validation and return the documented `400 Bad Request` response for invalid input. Keep validation at request boundaries and enforce business invariants in services as well.
- Use injected `ILogger<T>` backed by Serilog and structured message templates (for example, `logger.LogInformation("Project {ProjectId} created for organization {OrganizationId}", ...)`); do not use string interpolation for log messages. Follow the information, warning, and error event guidance in Logging Standards, and never log secrets, tokens, or sensitive personal data.
- Keep methods focused, avoid hidden side effects, and follow the repository's formatting and analyzer settings. Do not add suppressions or comments solely to hide warnings.

## Security Rules

- Require validated JWT Bearer authentication for every API endpoint; do not expose anonymous endpoints. Validate token signature, issuer, audience, and lifetime, and use the `sub`, `email`, `organizationId`, and `role` claims as specified by the authentication configuration.
- Apply the named policy-based authorization rules (`ProjectReader`, `ProjectWriter`, `AuditReader`, `AuditWriter`, and `NotificationReader`) at the appropriate endpoints and service boundaries. Never treat authentication alone as authorization.
- Enforce tenant isolation on every read and write. Derive `OrganizationId` only from the authenticated principal's `organizationId` claim, never from a request body, query, or route value. Scope repository queries and mutations by that organization and deny cross-tenant access; do not rely on controller filtering alone.
- Derive the current user's identity from trusted claims. For user-specific notification operations, do not allow a caller to read or change another user's notifications by supplying a different user ID.
- Validate all external input with DTOs and FluentValidation. Use EF Core parameterized query APIs; never concatenate untrusted data into raw SQL or commands. Enforce authorization and tenant checks before returning or mutating records.
- Keep secrets and signing keys out of source control, logs, responses, and fixtures. Load them from appropriately protected configuration providers and fail startup when required security configuration is absent.
- Return safe error responses without stack traces or internal details. Use centralized ASP.NET Core exception handling and `ProblemDetails`; log diagnostic details securely on the server.
- Preserve audit immutability: audit entries may be created, read, searched, and filtered, but must not be updated or deleted. Treat forwarded client IP headers as trustworthy only when forwarded-header processing is configured for known proxies.

## Testing Expectations

- Use xUnit for automated tests, with Moq for isolated dependencies and FluentAssertions for readable assertions. Keep tests in the solution's test project(s) under `tests/`.
- Unit-test service business rules and validation outcomes. Add API integration tests for routes, DTO responses, HTTP status codes, authentication, authorization policies, and error handling; cover persistence behavior with an appropriate relational test setup where needed.
- Every tenant-scoped feature must test that organization claims constrain both reads and writes and that cross-tenant access is denied. Test missing or invalid claims and attempts to supply a different organization or recipient identity.
- Cover at least these assessment scenarios: notification delivery to all team members; audit creation after a status change; audit entries cannot be updated or deleted; audit queries filter by date range and event type; and unauthorized cross-tenant access is denied. Add further edge-case tests; ten or more tests are recommended.
- Keep tests deterministic and isolated from production services and real credentials. Use controlled fixtures and mocks for external boundaries, and do not weaken production security or audit rules solely to make tests pass.
- Add or update tests for every behavior change, including AI-generated code. Run `dotnet test` for the affected test project during iteration and the full solution test suite before completion; report any unavailable or failing checks accurately.



# Solution Structure
```text
taskbridge-api/
│
├── .github/
│   └── copilot-instructions.md
│
├── src/
│   ├── projects/
│   │   ├── Models/
│   │   ├── Repositories/
│   │   ├── Services/
│   │   ├── Controllers/
│   │   └── DTOs/
│   │
│   └── notifications/
│       ├── Models/
│       ├── Repositories/
│       ├── Services/
│       ├── Controllers/
│       └── DTOs/
│
├── tests/
│
├── README.md
├── SPEC.md
├── REVIEW.md
├── IMPACT_ANALYSIS.md
├── PROMPTS.md
├── TOOL_STRATEGY.md
├── ARCHITECTURE.md
├── PR_DESCRIPTION.md
├── instructions.md
└── TaskBridge.sln
```

---

# Assessment Workflow

## Step 1 – Generate Legacy Project Service
Before creating any new functionality, use GitHub Copilot Chat and execute the following prompt exactly as provided:

```text
Generate a Project model and a Project service with create, update status, get by team, and delete functions. Use a database.
```

Requirements:

- Save generated files immediately.
- Do not edit generated files.
- Store them under src/projects/.
- Treat the code as inherited contractor-generated code.

---

## Step 2 – Create Project Standards
Create:
```text
.github/copilot-instructions.md
```

This file must define:

- Technology stack
- Coding standards
- Architecture expectations
- Security practices
- Testing requirements
- Multi-tenant SaaS rules
- GitHub Copilot expectations

---

## Step 3 – Create SPEC.md
Before implementation create SPEC.md.
Contents must include:

### Functional Requirements
- Audit logging
- Notification generation
- Audit search capabilities
- Notification retrieval capabilities

### Data Models
- Project
- AuditEntry
- Notification

### API Contracts
Request and response definitions for all endpoints.

### Security Constraints
Authentication requirements.
Authorization requirements.
Tenant isolation requirements.

### Validation Rules
Input validation requirements.

### Copilot Usage Notes
Document:
- Where Copilot helped.
- Where human judgment was required.

---

# Architecture Standards
The application must follow layered architecture.
```text
Controller
    ↓
Service
    ↓
Repository
    ↓
Entity Framework Core
    ↓
Database
```
Rules:

- Controllers must never use DbContext.
- Controllers contain no business logic.
- Services contain business rules.
- Repositories contain persistence logic only.
- DTOs are used for all API requests and responses.
- Entities must not be exposed directly.

---

# Multi-Tenant SaaS Requirements
TaskBridge is a multi-tenant application.
Every tenant represents a separate organization.
Every entity must include:

```csharp
public Guid OrganizationId { get; set; }
```

Applies to:

- Projects
- Audit Entries
- Notifications

All data access must be filtered by OrganizationId.
No user may access data belonging to another organization.
Organization values must come from JWT claims and never from request payloads.

---

# Security Requirements

## Authentication
Implement JWT Bearer Authentication.
Required claims:
```text
sub
email
organizationId
role
```

---

## Authorization

Use policy-based authorization.

Policies:

```text
ProjectReader
ProjectWriter
AuditReader
AuditWriter
NotificationReader
```

No anonymous endpoints are permitted.
All APIs require authentication.
---

# Logging Standards
Use Serilog structured logging.
## Information

Log:

- Project creation
- Project updates
- Project deletion
- Audit creation
- Notification creation

## Warning
Log:
- Validation errors
- Authorization failures
- Tenant boundary violations

## Error
Log:
- Database failures
- Unexpected exceptions
- Service processing failures

Never log:
- Passwords
- JWT tokens
- Secrets
- Sensitive personal information

---

# Validation Standards
Use FluentValidation.
All requests must be validated.

## Project Creation Validation
Required:

- Name
- TeamId

Rules:

- Name maximum 200 characters
- TeamId must not be empty

---

## Status Update Validation
Required:
- ProjectId
- Status
Status must be a valid enum value.

---

## Audit History Query Validation
Required:
- ProjectId
Optional:
- Date From
- Date To
- Event Type

If dates are supplied:

- From must be before To

Invalid requests return 400 Bad Request.

---

# Project Service Remediation
The inherited contractor-generated Project Service must be reviewed and rewritten.
Required layers:

## Model
Project
Fields:
```text
Id
Name
Status
TeamId
OrganizationId
CreatedAt
UpdatedAt
```

---

## Repository
Create:
```text
IProjectRepository
ProjectRepository
```

Responsibilities:
- Create
- Read
- Update
- Delete

No business logic allowed.

---

## Service
Create:
```text
IProjectService
ProjectService
```

Responsibilities:

- Project creation
- Project deletion
- Status updates
- Team project retrieval
- Audit event publishing

---

## Controller
Create:
```text
ProjectsController
```

Endpoints:
```http
POST    /projects
PATCH   /projects/{id}/status
GET     /projects/team/{teamId}
DELETE  /projects/{id}
```
---

# Notification & Audit Service
## AuditEntry Model
Fields:
```text
Id
ProjectId
EntityType
EventType
ActorUserId
OrganizationId
PreviousState
NewState
Timestamp
```
Scope change addition:

```text
ActorIpAddress
```
---

## Notification Model
Fields:
```text
Id
RecipientUserId
ProjectId
EventType
Message
IsRead
CreatedAt
OrganizationId
```
---

# Supported Event Types
Initial:
```text
PROJECT_CREATED
PROJECT_UPDATED
PROJECT_DELETED
MILESTONE_CREATED
MILESTONE_UPDATED
MILESTONE_CLOSED
```
Scope Change:
```text
MILESTONE_REOPENED
```
---

# Audit Immutability Requirement
Audit entries must be immutable.
Allowed:
```text
Create
Read
Search
Filter
```
Forbidden:
```text
Update
Delete
```
Do not implement:

```csharp
UpdateAudit()
DeleteAudit()
```

Audit records must remain permanently stored.

---

# Notification Logic
Whenever a project milestone event occurs:
```text
Created
Updated
Closed
Deleted
Reopened
```

The solution must:
1. Capture previous state.
2. Capture new state.
3. Create audit record.
4. Identify project team members.
5. Generate notifications.
6. Persist records.
7. Return success response.

---

# API Endpoints
## Audit
Create Audit Event
```http
POST /audit
```
Internal endpoint.
---
Retrieve Audit History
```http
GET /audit/{projectId}
```
Optional filters:
```text
from
to
eventType
```
---

## Notifications
Get Unread Notifications
```http
GET /notifications/{userId}
```
---
Mark Notification As Read
```http
PATCH /notifications/{id}/read
```

---
# Review Requirements
Create REVIEW.md.
Document:
- Issue description
- Location
- Severity
- Impact
- Detection method
- Fix recommendation

Include section:
```text
Architectural & Security Issues Copilot Introduced That Required Human Judgment
```
Explain:
- AI mistakes
- Security gaps
- Architecture flaws
- Multi-tenant concerns

---

# Impact Analysis Requirements
A change request introduces:
```text
MILESTONE_REOPENED
```
and
```text
Actor IP Address
```
Before coding create IMPACT_ANALYSIS.md.
Document:
- Data model changes
- API changes
- Service changes
- Migration requirements
- Security considerations
- Compliance concerns
- Implementation sequencing

Include:
```text
How Copilot Assisted This Analysis
```

---

# Testing Requirements
Minimum required tests:

### Test 1
Notification dispatched to all team members.

### Test 2
Audit entry created after status change.

### Test 3
Audit entry cannot be modified.

### Test 4
Audit entry cannot be deleted.

### Test 5
Audit query filtered by date range.

### Test 6
Audit query filtered by event type.

### Test 7
Unauthorized tenant access denied.

Recommended:
10 or more tests.

---

# GitHub Copilot Usage
Maintain PROMPTS.md.
For every prompt capture:
- Exact prompt
- Feature used
- Why used
- Result
- Corrections applied

Required Copilot features:

- Copilot Chat
- Inline Suggestions
- Edit Mode
- Agent Mode

Use at least three prompting techniques:

- Specificity
- Decomposition
- Constraints
- Role-Based Prompting
- Iterative Refinement
- Few-Shot Prompting

---

# Commit Strategy
Use Conventional Commits.
Minimum 5 commits.
Suggested history:
```text
chore: add project standards and copilot instructions
feat: remediate project service architecture
feat: implement notification and audit services
test: add audit and notification tests
docs: add architecture documentation and impact analysis
```
Commit messages should include descriptive bodies.

---

# Required Documentation
The final submission must contain:
```text
README.md
.github/copilot-instructions.md
SPEC.md
REVIEW.md
IMPACT_ANALYSIS.md
PROMPTS.md
TOOL_STRATEGY.md
ARCHITECTURE.md
PR_DESCRIPTION.md
```
---

# Definition of Done
The project is complete only if:
- Project Service generated and reviewed
- Project Service remediated
- Notification Service implemented
- Audit Service implemented
- JWT authentication configured
- Policy authorization configured
- Multi-tenant isolation enforced
- Validation implemented
- Structured logging implemented
- Audit immutability enforced
- Scope change analyzed
- Tests implemented and passing
- Documentation completed
- Prompt history captured
- GitHub Copilot usage documented
- Conventional commit history documented

---

# AI Usage Principle
GitHub Copilot is an engineering assistant, not the source of truth.
All generated code must be:
1. Reviewed.
2. Validated.
3. Tested.
4. Corrected as necessary.

Human judgment is mandatory for:
- Security decisions
- Multi-tenant isolation
- Compliance considerations
- Architecture decisions
- Production readiness

Never merge AI-generated code without review.
