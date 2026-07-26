<!--
Sync Impact Report
==================
Version change: (template) → 1.0.0
Modified principles: n/a (initial ratification — all placeholders filled)
Added sections:
  - Core Principles (5): I. Clean Architecture & Layer Boundaries;
    II. Test-Driven Development; III. API-First & Documented Contracts;
    IV. CQRS & Modern Engineering Patterns; V. Automated Pipelines & Quality Gates
  - Technology & Security Constraints
  - Development Workflow & Quality Gates
  - Governance
Removed sections: none
Templates requiring updates:
  - ✅ .specify/templates/plan-template.md — Constitution Check is a generic
    gate resolved per-plan against this file; no structural change required.
  - ✅ .specify/templates/spec-template.md — no constitution-specific
    references; aligned.
  - ✅ .specify/templates/tasks-template.md — test-first task ordering already
    supported; aligned.
Follow-up TODOs: none
-->

# InventoryX Constitution

## Core Principles

### I. Clean Architecture & Layer Boundaries (NON-NEGOTIABLE)

The solution MUST preserve the four-layer Clean Architecture structure with
dependencies pointing strictly inward:

- **InventoryX.Domain**: entities and core business rules only. MUST NOT
  reference any other project or infrastructure package.
- **InventoryX.Application**: use cases, CQRS commands/queries, DTOs, and
  service interfaces. MAY reference Domain only.
- **InventoryX.Infrastructure**: EF Core persistence, identity, and external
  service implementations. Implements Application interfaces.
- **InventoryX.Presentation**: API controllers, middleware, and composition
  root. MUST NOT contain business logic.

Business rules MUST NOT leak into controllers or data access code. Any new
external dependency (database, message broker, third-party API) MUST be
consumed through an interface defined in the Application layer.

Rationale: inward-pointing dependencies keep the domain testable in isolation
and allow infrastructure to change without rewriting business rules.

### II. Test-Driven Development (NON-NEGOTIABLE)

TDD is mandatory for all production code changes:

- Tests MUST be written before implementation (Red → Green → Refactor).
- Every command/query handler MUST have unit tests covering success and
  failure paths in the matching `tests/` project
  (`InventoryX.Application.Tests`, `InventoryX.Infrastructure.Tests`,
  `InventoryX.Presentation.Tests`, `InventoryX.Common.Tests`).
- Bug fixes MUST start with a failing test that reproduces the defect.
- A PR MUST NOT be merged with failing or skipped tests; new features without
  accompanying tests MUST be rejected in review.

Rationale: a SaaS backend handling inventory and sales data cannot tolerate
regressions in stock, purchase, or sale calculations; tests are the executable
specification.

### III. API-First & Documented Contracts

The REST API is the product surface and MUST be treated as a contract:

- Every endpoint MUST be exposed in Swagger/OpenAPI with accurate request and
  response schemas, status codes, and auth requirements.
- Endpoint naming, HTTP verbs, and status codes MUST follow REST conventions;
  errors MUST return a consistent problem-details shape.
- Breaking changes to a published endpoint REQUIRE a new API version;
  unversioned breaking changes are forbidden.
- Public-facing docs (README, CHANGELOG) MUST be updated in the same PR that
  changes behavior they describe.

Rationale: SaaS consumers integrate against the documented contract; drift
between docs and behavior is a production incident, not a cosmetic issue.

### IV. CQRS & Modern Engineering Patterns

Application logic MUST follow the established patterns of the codebase:

- Reads and writes MUST be modeled as MediatR queries and commands; new
  features MUST NOT bypass the CQRS pipeline with ad-hoc service calls.
- Object mapping MUST go through AutoMapper profiles, not hand-rolled mapping
  scattered across handlers.
- Validation MUST run before a command reaches domain state changes.
- SOLID, DRY, and YAGNI apply: introduce abstractions only when a second
  concrete need exists, and justify any deviation in the PR description.

Rationale: consistent patterns keep the codebase navigable as the team and
feature set grow; exceptions multiply maintenance cost.

### V. Automated Pipelines & Quality Gates

Delivery MUST be automated and repeatable:

- CI MUST build the solution and run the full test suite on every PR; a red
  pipeline blocks merge.
- Database schema changes MUST be expressed as EF Core migrations committed
  with the feature that requires them — never manual SQL against environments.
- Secrets MUST NOT be committed; configuration MUST come from environment
  variables or secret stores per deployment environment.
- Releases MUST be traceable: CHANGELOG entries and semantic version tags for
  every deployable change.

Rationale: pipelines are the enforcement mechanism for every other principle;
manual steps are where quality silently erodes.

## Technology & Security Constraints

- **Stack**: .NET 8.0, ASP.NET Core Web API, Entity Framework Core, SQL
  Server, MediatR, AutoMapper, Swagger/OpenAPI, ASP.NET Identity. Introducing
  a new framework-level dependency REQUIRES review approval and a note in the
  plan's Complexity Tracking.
- **AuthN/AuthZ**: every endpoint MUST declare its authorization policy
  explicitly; anonymous access is opt-in and justified, never the default.
- **Multi-tenancy & data safety**: queries touching tenant-owned data MUST be
  scoped to the caller's tenant/organization; cross-tenant leakage is a
  release-blocking defect.
- **Performance**: list endpoints MUST paginate; N+1 query patterns found in
  review MUST be fixed before merge.

## Development Workflow & Quality Gates

- All work happens on feature branches (`feat/*`, `fix/*`) and merges to
  `main` via pull request; direct pushes to `main` are forbidden.
- PR review MUST verify: constitution compliance, test coverage for new
  behavior, Swagger accuracy for changed endpoints, and migration presence
  for schema changes.
- Spec Kit artifacts (spec.md, plan.md, tasks.md) MUST exist for any
  non-trivial feature before implementation begins; the plan's Constitution
  Check gate MUST pass before Phase 0 research.
- Commit messages follow the existing convention (`feat:`, `fix:`, `docs:`,
  etc.).

## Governance

This constitution supersedes all other development practices in this
repository. Where guidance conflicts, the constitution wins.

- **Amendments**: proposed via PR modifying this file, including a Sync
  Impact Report and updates to any dependent templates. Approval by a
  maintainer is required before merge.
- **Versioning**: semantic versioning of this document — MAJOR for principle
  removals or incompatible redefinitions, MINOR for new principles or
  materially expanded guidance, PATCH for clarifications and wording.
- **Compliance review**: every PR review checks changes against the Core
  Principles; violations MUST be fixed or explicitly justified in the plan's
  Complexity Tracking section before merge. Unjustifiable complexity MUST be
  simplified.

**Version**: 1.0.0 | **Ratified**: 2026-07-26 | **Last Amended**: 2026-07-26
