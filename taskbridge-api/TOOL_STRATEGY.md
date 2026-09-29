# Tool Strategy

## Feature Usage Log

| # | Copilot feature used | What was used | Why this feature instead of another | Outcome |
|---|---|---|---|---|
| 1 | Copilot Chat (Ask/explain with selection context) | Asked for an explanation of the selected `DeleteAsync` implementation. | The request was to understand one code block, not change it, so a focused chat explanation was more appropriate than Agent Mode edits. | Explained tenant-scoped lookup, hard deletion, return values, cancellation, and the audit limitation in that version. |
| 2 | Copilot Chat (code review) | Reviewed the generated project service and later checked the notification/audit implementation against repository standards. | Review needed findings and risks without changing code; this separated analysis from remediation. | Identified direct `DbContext` use, caller-supplied tenant IDs, missing audit/team checks, and incomplete milestone orchestration. |
| 3 | Copilot Agent Mode | Created and later remediated the .NET project/service/API across multiple files. | The work crossed models, repositories, services, endpoints, EF mappings, migrations, tests, and docs; Agent Mode could coordinate that workflow and run checks. | Added claim-derived tenant scope, repository boundaries, audit/notification services, endpoints, and migrations. |
| 4 | Repository custom instructions (`.github/copilot-instructions.md`) | Used the assessment's .NET, layered architecture, security, logging, and testing standards as persistent workspace context. | Workspace instructions are better for constraints that should apply across multiple requests than repeating them in every prompt. | Corrected the initial Node/TypeScript stack drift and used the guide to identify architecture/security gaps. |
| 5 | Editor context: selected code and attached files | Reviewed the actual selected `Notification`, `AuditEntry`, and test code supplied in editor context. | Selection/attachments scoped the response to the file under discussion instead of guessing from a broad project description. | Explained constructors and test coverage against the current implementation; surfaced missing milestone workflow coverage. |
| 6 | Agent tool integration: build/test and migration tools | Ran .NET build/test commands, generated migration SQL, and inspected the output. | Compilation and test execution provide evidence that reasoning alone cannot; migration SQL must be inspected before deployment. | The latest recorded solution run passed 23 tests; migration SQL was generated and inspected but not applied to SQL Server. |

Inline Suggestions and Edit Mode were **not used** during this case study and are not claimed as usage. The assessment may require them separately; record actual future use rather than retroactively claiming it.

## Scenario Responses

### Understand a complex 600-line legacy service

Use **Copilot Chat Ask Mode** with the file attached and ask it to trace entry points, state changes, persistence, external calls, and tenant/auth boundaries, citing symbols. Ask Mode is suitable for an initial explanation without changing the legacy file; verify the map against call sites and tests before wiring a new service.

### Generate validation middleware across 10 route handlers

Use **Agent Mode** with the repository instructions and representative route/validator patterns, asking it to inventory all ten handlers, apply one consistent validation approach, and add tests for valid and invalid requests. Agent Mode is appropriate because this is a coordinated multi-file change; review the diff and run focused plus full tests before accepting it.

### Verify JWT expiry and signature-tampering handling

Use **Copilot Chat Ask Mode** to inspect token validation parameters and propose tests using controlled signing keys, expired tokens, wrong signatures, issuer, and audience. Chat can identify gaps and test cases, but correctness must be established by executing authentication tests against the configured handler, not by accepting a prose assurance.

### Enforce lint and coverage thresholds on every `main` commit

Use **Agent Mode** to draft a GitHub Actions workflow that restores, builds, runs lint/format checks, collects coverage, and fails below the agreed threshold; then review the workflow and test it on a branch. Enforcement comes from required CI status checks and branch protection, not Copilot itself, so configure those repository controls independently.

### Review a contractor's generated service for security issues

Use **Copilot Chat Ask Mode** for a findings-first review with the threat model, tenant rules, and relevant repositories attached. Ask it to trace untrusted input to data access and authorization, then manually validate every finding and run targeted tests/scanners before staging.

### Apply tenant-isolation rules across developers and sessions

Use **repository custom instructions** in `.github/copilot-instructions.md` to state that tenant identity comes only from validated claims and every read/write is organization-scoped. This is more reliable than relying on one chat prompt, but instructions guide rather than enforce behavior; use repository tests, code review, and CI checks as deterministic gates.

## Limitations Encountered

### 1. Scaffold defaulted to the wrong stack

- **Prompt:** The initial request asked for a `taskbridge-api/` tree and a dependency file but did not specify a technology stack.
- **What went wrong:** The scaffold selected Node.js/TypeScript/Express, while the assessment's authoritative instructions specified .NET 10 and ASP.NET Core.
- **Detection:** Comparing the created `package.json`/README with the manually updated `.github/copilot-instructions.md` exposed the conflict.
- **Fix:** Replaced the active scaffold with the .NET project and aligned the README. The obsolete TS files were removed after confirming the .NET implementation.
- **Do differently:** Inspect repository/assessment instructions and confirm the stack before generating framework files; when ambiguous, ask rather than infer.

### 2. Generated Project service trusted caller-supplied tenant identity

- **Prompt:** `Generate a Project model and a Project service with create, update status, get by team, and delete functions. Use a database.`
- **What went wrong:** The first service accepted `organizationId` as a method argument and accessed `DbContext` directly; it also did not verify that the team belonged to that organization or write audit events.
- **Detection:** Reviewed each query/mutation against the multi-tenant JWT claim rule and Controller/Service/Repository architecture in the instructions.
- **Fix:** Moved persistence to repositories, derived organization/user from the current actor context, verified team ownership, and staged audit/notification writes in the project unit of work.
- **Do differently:** Include the tenant trust boundary, repository contract, event behavior, and explicit negative tests in the generation prompt, then review generated code before extending it.

### 3. Milestone event support was incomplete

- **Prompt:** `Notification & Audit Service not created yet in this project , Generate Audit Log Model, Notification Model, Core Service Logic (audit +notification handling), API Endpoints, Tests (≥6)`
- **What went wrong:** The implementation accepts milestone event names and can record an internal `MILESTONE_UPDATED`/`MILESTONE_REOPENED` audit request, but no milestone entity or transition service automatically captures state and dispatches notifications. A test of the audit service alone could be mistaken for end-to-end milestone support.
- **Detection:** Traced the event from allow-list through a real mutation path and noticed there is no milestone model/service; compared this with the requirement that a milestone change triggers both audit and notification.
- **Fix:** Added tests for audit snapshots and project status-change fan-out, and documented the milestone workflow as outstanding rather than claiming it is implemented.
- **Do differently:** Define milestone state transitions and expected side effects first, then implement and test the complete transaction from state change through audit and team notification.

## Development Workflow and Verification

1. Read `.github/copilot-instructions.md` and inspect the owning source/tests before changes.
2. Use Copilot for a bounded task and keep one architectural concern per edit slice.
3. Preserve user edits and generated assessment artifacts unless explicitly asked to replace them.
4. Build after structural changes; run focused tests after behavior changes and `dotnet test TaskBridge.sln` before completion.
5. Inspect generated EF migrations and SQL scripts. Never auto-apply migrations at production startup.
6. Have a human review JWT claim derivation, tenant filters, role assignment, forwarded IP trust, audit immutability, privacy/retention, and milestone semantics.

From `taskbridge-api/`:

```powershell
dotnet build TaskBridge.Api.csproj
dotnet test TaskBridge.sln
dotnet tool restore --tool-manifest .config/dotnet-tools.json
dotnet ef migrations script --idempotent --project TaskBridge.Api.csproj --startup-project TaskBridge.Api.csproj
```

`dotnet ef database update` requires a configured SQL Server connection and should only be run against an intentional target database. Current tests use EF Core InMemory; they do not prove SQL Server trigger/migration behavior. HTTP integration tests against a configured identity provider remain future work.
