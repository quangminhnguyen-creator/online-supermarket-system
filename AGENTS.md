# Repository guide for AI agents

## Scope and current state

This repository contains an academic online supermarket system. `main` carries the approved design and Sprint 1 plan; feature work may live in linked worktrees. Before running a command, confirm that its referenced manifest exists in the current branch.

Agents must not invent commands, behavior, or architecture. Use CodeGraph for structural symbol queries when available; use text search for literal strings and comments.

## Technology stack

- Backend: .NET SDK 10.0.103, ASP.NET Core 10 Minimal API, C# with nullable enabled and warnings as errors.
- Persistence: Entity Framework Core 10.0.9 with MySQL Connector/NET; MySQL 8.4 LTS.
- Backend tests: xUnit and ASP.NET Core integration tests.
- Frontend: React 19.2.8, TypeScript 5.9.3, Vite 7.3.6, Vitest 4.1.10, Testing Library.
- Local deployment: Docker Compose with frontend, API, and MySQL services.
- API documentation: checked-in OpenAPI contract under `docs/api/`.

## Project structure

```text
backend/src/OnlineSupermarket.Api/             HTTP composition root and contracts
backend/src/OnlineSupermarket.Domain/          entities and business invariants
backend/src/OnlineSupermarket.Infrastructure/  EF Core, MySQL, configurations, migrations
backend/tests/OnlineSupermarket.Domain.Tests/  domain unit tests
backend/tests/OnlineSupermarket.Api.Tests/     API and persistence integration tests
frontend/src/api/                              frontend HTTP/API boundary
frontend/src/features/                         feature UI and colocated tests
docs/architecture/                             architecture and ERD
docs/api/                                      OpenAPI contract
docs/superpowers/                              approved specifications and implementation plans
.ai/                                           workflow status, tasks, results, and reviews
.opencode/agents/                              OpenCode role definitions
.opencode/commands/                            project-local workflow commands
```

Backend dependency direction is `Api -> Infrastructure -> Domain`. Domain code must not depend on EF Core or ASP.NET Core. The backend is a modular monolith, not microservices.

## Verified commands

Run from the repository root when `OnlineSupermarket.slnx` exists:

```powershell
dotnet restore OnlineSupermarket.slnx
dotnet build OnlineSupermarket.slnx --no-restore
dotnet test OnlineSupermarket.slnx --no-restore
dotnet run --project backend/src/OnlineSupermarket.Api
```

Run frontend commands when `frontend/package.json` exists:

```powershell
Set-Location frontend
npm.cmd install
npm.cmd test -- --run
npm.cmd run build
npm.cmd run dev
```

No standalone lint script is defined in `frontend/package.json`. Do not invent one. TypeScript validation is part of `npm.cmd run build`; .NET compiler/analyzer warnings fail the build.

Database migration commands, when the backend manifests exist:

```powershell
dotnet tool restore
dotnet dotnet-ef migrations add <MigrationName> --project backend/src/OnlineSupermarket.Infrastructure --startup-project backend/src/OnlineSupermarket.Api --output-dir Persistence/Migrations
dotnet dotnet-ef database update --project backend/src/OnlineSupermarket.Infrastructure --startup-project backend/src/OnlineSupermarket.Api
```

Full-stack validation requires Docker Desktop:

```powershell
docker compose config --quiet
docker compose up --build -d
Invoke-RestMethod http://localhost:8080/api/health
Invoke-WebRequest http://localhost:5173
docker compose down
```

## Architecture and coding conventions

- Keep domain invariants in Domain; keep EF Core mappings and migrations in Infrastructure; keep HTTP composition and transport contracts in Api.
- Use private constructors for EF Core entities and named factory methods for valid creation where the established model follows that pattern.
- Keep Domain free of persistence attributes.
- Use `decimal(18,2)` for money, UTC timestamps, restrictive deletes by default, and explicit unique indexes for business keys.
- Price and inventory are branch-specific through `BranchInventory`; never move inventory directly onto `Product`.
- Checkout must recalculate price, promotions, and stock server-side and reserve inventory transactionally.
- Payment return URLs are display-only; only validated, idempotent IPN callbacks update payment/order state.
- Never store card number, expiry, or CVV. Secrets belong in environment variables; commit only `.env.example`.
- Frontend API calls live under `frontend/src/api/`; feature components and their tests are colocated under `frontend/src/features/`.
- Prefer the smallest task-scoped change. Do not perform unrelated refactoring or formatting.

## Naming conventions

- C#: PascalCase for public types/members, camelCase for parameters/locals, one responsibility per file, namespaces aligned with project folders.
- TypeScript/React: PascalCase for components and component files, camelCase for functions and variables, `*.test.tsx` for component tests.
- Database: snake_case table/column names as defined by EF Core configurations; migration names use PascalCase intent such as `InitialFoundation`.
- AI artifacts: `TASK-NNN.md`, `TASK-NNN-ACTION.md`, numeric code reviews such as `TASK-NNN-R1.md` and `TASK-NNN-R2.md`, docs results `TASK-NNN-DOCS.md`, and numeric docs reviews such as `TASK-NNN-DR1.md` and `TASK-NNN-DR2.md`.

## Files and directories to avoid

- Never edit `.git/`, `.worktrees/`, `.codegraph/`, generated `bin/`, `obj/`, `node_modules/`, or `dist/` content.
- Do not hand-edit generated EF migration designer files or `AppDbContextModelSnapshot.cs`; generate migrations with the pinned local tool.
- Do not edit checked-in `docs/api/openapi.json` independently of the implemented API and export script.
- Do not read or commit `.env`; `.env.example` must contain local placeholders only.
- Do not modify application source from Workflow, Review, or Docs.

## Migration rules

- A schema change requires a focused model/configuration test, an EF Core migration, and inspection of generated SQL/model changes.
- Never rewrite or delete an already-shared migration without an explicit architecture decision.
- Migrations must preserve existing data or document the approved destructive behavior in the task.
- Database contract changes must be reviewed for indexes, foreign keys, delete behavior, defaults, nullability, and rollback implications.

## AI workflow rules

- Workflow: primary orchestrator; writes task/status artifacts, waits for approval, and invokes only Action, Review, and Docs.
- Action: implements one approved task or fixes explicit blocking review findings, runs required checks, and records exact evidence.
- Review: independently reviews the task, diff, relevant source, and test evidence; application code is read-only.
- Docs: runs after `APPROVED` when public behavior, setup, API, or maintained documentation changed; it edits maintained documentation only.
- Follow `.ai/WORKFLOW.md`; use `.ai/tasks/TASK-TEMPLATE.md`, `.ai/reviews/REVIEW-TEMPLATE.md`, and `.ai/results/RESULT-TEMPLATE.md`.
- Never bypass the approval gate or the mandatory R2 after fixes. CODE and DOCS_ONLY stop after two automatic review rounds; R3 requires explicit user approval.
- After successful completion, Workflow resets `.ai/STATUS.md` to the documented neutral state only after durable evidence is recorded; `BLOCKED` state is never auto-reset.
