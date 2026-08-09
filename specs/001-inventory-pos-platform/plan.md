# Implementation Plan: Inventory Management & Point of Sale Platform — Cycle 1

**Branch**: `feat/saleGroup` | **Date**: 2026-07-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-inventory-pos-platform/spec.md`

**Note**: This plan covers the first delivery cycle agreed in clarifications: Phase 1 core
(accounts/subscriptions/billing, users & roles, catalogue with simple + variant items,
multi-location stock & movements, POS sales with cash/card/mobile-money recording,
receipts, returns, shift & cash management, offline-sale sync API, essential reports,
spreadsheet import/export) **plus** two Phase 2 items brought forward: purchase orders &
suppliers, and batch/expiry tracking. Backend API only; Ghana-first configuration.

## Summary

Evolve the existing InventoryX Clean Architecture API from a single-business inventory
tracker into a multi-tenant SaaS backend combining inventory management and POS. The
technical approach keeps the proven stack (.NET 8, EF Core + SQL Server, MediatR CQRS,
AutoMapper, ASP.NET Identity + JWT, Swashbuckle, xUnit) and layers on: tenant isolation
via a `TenantId` discriminator with EF global query filters; a subscription/plan engine
with usage enforcement; a reworked catalogue (Product/Variant replacing InventoryItem);
per-location stock ledger with immutable movements; POS sale/return/shift flows;
idempotent offline-sale sync; purchase orders with receipt matching; batch/expiry
tracking with FEFO issue; and Paystack-based subscription billing for Ghana (cards +
MTN MoMo). New dependencies (FluentValidation, Serilog, Paystack via REST) are justified
in Complexity Tracking.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0 (LTS), ASP.NET Core Web API

**Primary Dependencies**: EF Core 9 (SqlServer provider), MediatR 13, AutoMapper 15,
ASP.NET Identity + JWT Bearer + Google OAuth, Swashbuckle (OpenAPI), SendGrid (email),
FluentValidation (new), Serilog (new), Paystack REST API (new — subscription billing,
cards + mobile money for Ghana)

**Storage**: SQL Server (single database, shared schema, `TenantId` row isolation with
EF Core global query filters); EF Core migrations only

**Testing**: xUnit + Moq + FluentAssertions + AutoFixture (existing shared test kit in
`tests/InventoryX.Common.Tests`), coverlet for coverage; TDD per constitution

**Target Platform**: Linux server (containerized Kestrel behind reverse proxy)

**Project Type**: web-service — REST API backend only (clients are separate projects)

**Performance Goals**: sale-line operations (the POS scan path) p95 < 300 ms server-side;
ordinary endpoints p95 < 500 ms; hundreds of transactions/minute per tenant at peak;
long-period reports streamed/async rather than blocking

**Constraints**: offline-capable POS clients served via catalogue snapshot + idempotent
queued-sale upload with conflict flagging (never silent overwrite); strict tenant
isolation; 99.9% monthly uptime design target; no card data stored on platform;
plan-limit enforcement (300/3,000 monthly sales on Free/Standard, feature gates)

**Scale/Scope**: hundreds of thousands of products per tenant, dozens of locations,
data history retention per plan tier; Cycle 1 scope = user stories P1–P7 (P3 batch
portions included, serials deferred), ~45 of the 61 FRs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Principle | Status | Notes |
|------|-----------|--------|-------|
| G1 | I. Clean Architecture & Layer Boundaries | ✅ PASS | Design keeps the four projects; new billing/payment/tenancy concerns enter via Application interfaces implemented in Infrastructure; no business logic in controllers |
| G2 | II. Test-Driven Development | ✅ PASS | Every new/changed handler gets failing-first unit tests in the matching test project; integration tests for tenancy filters, sync idempotency, and migrations |
| G3 | III. API-First & Documented Contracts | ✅ PASS | All endpoints defined in `contracts/` before implementation; versioned under `/api/v1`; problem-details error shape; Swagger kept accurate per PR |
| G4 | IV. CQRS & Modern Engineering Patterns | ✅ PASS | All features as MediatR commands/queries; AutoMapper profiles; FluentValidation in a MediatR pipeline behavior ahead of handlers |
| G5 | V. Automated Pipelines & Quality Gates | ✅ PASS | CI builds + tests on PR; schema via EF migrations committed with features; secrets from environment; CHANGELOG per release |
| G6 | Tech & Security Constraints | ✅ PASS (with additions) | Stack unchanged; three new dependencies justified in Complexity Tracking; tenant scoping is G6's release-blocking rule — enforced by global query filters + integration tests |

**Post-design re-check (after Phase 1)**: all gates still pass. No violations requiring
justification beyond the new-dependency entries in Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/001-inventory-pos-platform/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output — API contracts by domain area
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
InventoryX.Domain/
├── Models/
│   ├── Common/BaseModel.cs          # existing — gains TenantId, audit fields
│   ├── Tenancy/                     # NEW: Tenant, Subscription, PlanDefinition, UsageCounter
│   ├── Catalog/                     # NEW: Product, ProductVariant, Category, UnitOfMeasure
│   ├── Inventory/                   # NEW: Location, StockLevel, StockMovement, Batch,
│   │                                #      StockTransfer, StockCount, AdjustmentReason
│   ├── Purchasing/                  # NEW: Supplier, PurchaseOrder(+Line), GoodsReceipt(+Line)
│   ├── Selling/                     # EVOLVED: Sale(+Line,+Payment), Return, Receipt,
│   │                                #      Register, Shift, CashMovement
│   └── Auditing/                    # NEW: AuditLogEntry, Notification(+Preference)
│   # Existing InventoryItem/RetailStock/Purchase/Sale/SaleGroup migrate per research.md R4

InventoryX.Application/
├── Commands/{Requests,RequestHandlers}/<Area>/   # existing pattern, new areas added
├── Queries/{Requests,RequestHandlers}/<Area>/
├── DTOs/<Area>/
├── Validators/<Area>/               # NEW: FluentValidation validators
├── Behaviors/                       # NEW: ValidationBehavior, AuditBehavior (MediatR pipeline)
├── Repository/                      # existing IRepository + new specific interfaces
├── Services/IServices/              # + IPaymentGateway, ITenantContext, IPlanEnforcer,
│                                    #   IStockLedger, IReceiptRenderer, IImportService
└── Options/                         # + PaystackOptions, PlanOptions, GhanaTaxOptions

InventoryX.Infrastructure/
├── Data/                            # DbContext + global query filters + migrations
├── Repositories/
├── Services/                        # PaystackGateway, SendGrid mailer, report exporters
└── BackgroundJobs/                  # NEW: hosted services — billing grace, alerts, digests

InventoryX.Presentation/
├── Controllers/v1/                  # versioned: Auth, Tenants, Users, Billing, Products,
│                                    #   Locations, Stock, Transfers, Counts, Suppliers,
│                                    #   PurchaseOrders, Registers, Shifts, Sales, Returns,
│                                    #   Sync, Reports, Notifications, ImportExport
└── Middleware/                      # TenantResolution, ProblemDetails, request logging

tests/
├── InventoryX.Common.Tests/         # shared fixtures/builders (existing)
├── InventoryX.Application.Tests/    # handler + validator unit tests (TDD)
├── InventoryX.Infrastructure.Tests/ # repository, query-filter, migration tests
└── InventoryX.Presentation.Tests/   # controller/auth/middleware tests
```

**Structure Decision**: Keep the existing four-project Clean Architecture solution and
its established CQRS folder convention (`Commands|Queries/{Requests,RequestHandlers}`),
adding domain-area subfolders rather than new projects. New cross-cutting concerns
(validation behaviors, tenant context, background jobs) slot into the layer they belong
to. No new projects are created; the four test projects mirror the four layers.

## Complexity Tracking

> Constitution G6 requires review approval + a note here for any new framework-level
> dependency. No architecture violations exist; entries below are the new dependencies.

| Addition | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| FluentValidation | Constitution Principle IV mandates validation before commands reach domain state; spec has dozens of rule-bearing inputs (plan limits, thresholds, states) | Hand-rolled `if` validation in every handler duplicates logic, is untestable in isolation, and drifts; DataAnnotations can't express conditional/cross-field rules cleanly |
| Serilog + structured logging | Principle II/V demand auditable, debuggable pipelines; FR-008 audit logging and SC-009 uptime work need structured, queryable logs | `Microsoft.Extensions.Logging` default console output lacks structured sinks/enrichment (tenant id per log line) needed for a multi-tenant SaaS |
| Paystack (REST integration, no SDK dependency) | FR-016/FR-039 require card + mobile money billing in Ghana; Paystack covers cards + MTN MoMo/Telecel/AT with one API | Stripe lacks Ghana merchant/MoMo coverage; direct MTN MoMo API means one integration per network; building card handling in-house violates FR-059 (no card data on platform) |
| ClosedXML | FR-018 spreadsheet import with preview and FR-049/056 XLSX export require real .xlsx parsing/generation (research R12) | EPPlus moved to commercial licensing; CSV-only fails the spec's column-matching import from real business spreadsheets; Office interop is not cross-platform |

## Phase Notes

- **Deferred from Cycle 1** (tracked in spec Assumptions): serial-number tracking,
  bundles/recipes, customers/credit/price tiers/promotions/quotes (P9), consignment,
  assets, forecasting, custom roles, multi-currency, accounting/e-commerce integrations,
  custom report builder, FIFO/specific-cost valuation (weighted average only in Cycle 1;
  batch receipt costs are captured so FIFO layers on later), StoreCredit/GiftCard/
  loyalty/on-account tenders, shared-catalogue barcode enrichment, and interface
  languages beyond English (i18n scaffolding deferred with multi-currency). The data model reserves extension points (e.g.
  `Product.TrackingMode` enum, nullable `CustomerId` on Sale) so these bolt on without
  breaking migrations.
- **Existing-code migration strategy** is decision R4 in [research.md](./research.md):
  evolve, don't rewrite — `InventoryItem` → `Product`, `RetailStock` → `StockLevel`,
  `Purchase` → seeded `GoodsReceipt`, `Sale`/`SaleGroup` → `SaleLine`/`Sale`, with EF
  data-migration scripts and the old endpoints retired behind `/api/v1`.
