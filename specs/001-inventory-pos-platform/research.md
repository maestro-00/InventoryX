# Research & Decisions: Inventory + POS Platform — Cycle 1

**Date**: 2026-07-26 | **Plan**: [plan.md](./plan.md)

Per the user directive — "use existing stack, tools and packages if it makes sense,
otherwise discard" — every existing dependency was evaluated as keep/discard, and each
gap in the Technical Context resolved below. No NEEDS CLARIFICATION items remain.

## R1. Existing stack audit (keep/discard)

**Decision**: Keep .NET 8 + ASP.NET Core, EF Core 9 (SqlServer), MediatR 13, AutoMapper
15, ASP.NET Identity + JWT Bearer + Google OAuth, Swashbuckle, SendGrid, and the full
test kit (xUnit, Moq, FluentAssertions, AutoFixture, coverlet). Discard/deprecate:
Newtonsoft.Json (Application layer) in favour of System.Text.Json for new code;
`SPA_AUTH_EXAMPLE.tsx` and any client-side artifacts (out of scope — backend only).

**Rationale**: The constitution pins this stack; all packages are current majors and the
CQRS/test conventions are established and working. Newtonsoft duplicates the built-in
serializer ASP.NET Core already uses; two JSON stacks cause subtle contract bugs.

**Alternatives considered**: Wholesale replacement of MediatR/AutoMapper (both moved to
commercial licensing) with hand-rolled dispatch/mapping — rejected: license cost is not
currently a constraint at this team size, and a rewrite would destabilise every existing
handler for zero functional gain. Revisit at Enterprise scale.

## R2. Multi-tenancy model

**Decision**: Single SQL Server database, shared schema, `TenantId` (GUID) column on
every tenant-owned table, enforced by EF Core global query filters driven by an
`ITenantContext` resolved per-request from the JWT claim, plus a `SaveChanges`
interceptor that stamps `TenantId` and rejects cross-tenant writes.

**Rationale**: Cheapest to operate at launch, one migration path, works with plan-based
row counts; global query filters give constitution G6's "release-blocking" isolation a
single enforcement point that integration tests can attack directly.

**Alternatives considered**: Database-per-tenant (operational cost explodes with
self-service signups; migrations × N); schema-per-tenant (SQL Server schema sprawl,
tooling pain). Both deferred as a possible Enterprise-tier offering.

## R3. Tenant identity & auth flow

**Decision**: Keep ASP.NET Identity users, extend with `TenantId` + role claims. JWT
carries `tenant_id`, `role`, and `location_scope` claims. Register PIN (FR-007) is a
separate short-credential table hashed with the Identity password hasher, exchangeable
for a short-lived, register-scoped JWT only when a full-session device token is present.
2FA via Identity's built-in TOTP; Google OAuth retained for owner sign-up/sign-in.

**Rationale**: Reuses installed Identity + JWT + Google packages; PIN-for-scoped-token
keeps the fast-cashier-switch UX without weakening account auth.

**Alternatives considered**: External IdP (Auth0/Keycloak) — rejected for launch: new
infrastructure, cost, and the constitution already pins ASP.NET Identity.

## R4. Migration of existing domain model

**Decision**: Evolve in place with renames + data migrations:
`InventoryItemType` → `Category`; `InventoryItem` → `Product` (gains `TrackingMode`
enum: Simple | Variant | Batch, reserved values for Serial etc.); `RetailStock` →
`StockLevel` keyed by (Product/Variant, Location, Batch?); `Purchase` → historical
`GoodsReceipt` rows; `SaleGroup`/`Sale` → `Sale`/`SaleLine`. Existing controllers are
rebuilt under `/api/v1` as the versioned contract; pre-versioned routes are removed in
the same release (no external consumers exist yet).

**Rationale**: Preserves git history, test assets, and working handler patterns while
correcting names to the spec's ubiquitous language; a greenfield rewrite discards a
tested foundation for no user-visible benefit.

**Alternatives considered**: Parallel new schema with sync bridge — rejected: nothing in
production to bridge; big-bang rewrite — rejected: violates YAGNI and inflates risk.

## R5. Stock ledger design

**Decision**: `StockMovement` is an append-only ledger (type, qty ±, product/variant,
batch, location, user, reason, timestamp, correlation id); `StockLevel` is a maintained
projection updated in the same transaction. Corrections are compensating entries
(FR-024). FEFO issue for batch-tracked products picks earliest-expiry batch at sale/
receipt-consumption time.

**Rationale**: Matches FR-023/24/25's immutability demand, gives free audit trail, and
keeps reads O(1) via the projection instead of summing history.

**Alternatives considered**: Full event sourcing (Marten/EventStore) — rejected: new
infra + paradigm for a team already productive in CRUD+CQRS; ledger table delivers the
same auditability. Computing stock by SUM on demand — rejected: kills POS latency at
scale.

## R6. Offline sale sync protocol

**Decision**: Clients download a versioned catalogue snapshot (`GET /sync/snapshot`,
delta by `rowversion`). Offline sales carry a client-generated UUID (`ClientSaleId`) and
device/register id; upload via `POST /sync/sales` (batch, idempotent upsert on
`ClientSaleId` — replays return prior result, never duplicate). Stock effects apply on
ingest; if application would drive `StockLevel` negative or conflicts with a concurrent
movement, the sale still records but a `StockConflict` flag + notification is raised for
review (FR-046) — never silent adjustment.

**Rationale**: Idempotency-by-client-key is the industry-standard answer to
at-least-once retry; recording the sale but flagging stock honours "honest about the
numbers" and FR-044/45/46.

**Alternatives considered**: Operational-transform/CRDT stock reconciliation — massive
complexity for marginal benefit; rejecting conflicting sales — unacceptable: the sale
already happened at the counter.

## R7. Subscription billing & payments (Ghana-first)

**Decision**: Paystack via direct REST integration (typed `IPaymentGateway` in
Application, `PaystackGateway` in Infrastructure): card + MTN MoMo/Telecel/AT Money
charges, webhook-driven payment confirmation, our own subscription state machine
(Trial → Active → PastDue(7-day grace) → ReadOnly → Cancelled → Purged@90d) run by a
background job rather than Paystack Plans.

**Rationale**: One provider covers FR-016's card + mobile money for Ghana; owning the
state machine keeps FR-011..015's exact trial/grace/read-only/retention semantics
independent of provider quirks; webhooks + signature verification keep card data off
platform (FR-059).

**Alternatives considered**: Stripe (no Ghana MoMo coverage), Flutterwave (viable
fallback — interface abstracts it), Hubtel (Ghana-only, weaker card rails), direct
telco MoMo APIs (three integrations instead of one).

## R8. Validation & cross-cutting behaviors

**Decision**: FluentValidation validators per command/query, executed in a MediatR
`ValidationBehavior` pipeline stage; an `AuditBehavior` writes `AuditLogEntry` rows for
sensitive commands (price change, refund, void, adjustment, permission change — FR-008);
plan-limit checks in a `PlanEnforcementBehavior` consulting `IPlanEnforcer` (FR-010).

**Rationale**: Pipeline behaviors are the MediatR-idiomatic way to satisfy constitution
Principle IV ("validation MUST run before a command reaches domain state changes")
without repeating code in ~80 handlers.

**Alternatives considered**: DataAnnotations (can't express cross-field/state rules);
validation inside handlers (duplication, untestable in isolation).

## R9. Background processing

**Decision**: .NET `IHostedService`/`BackgroundService` workers with a simple DB-backed
job/outbox table for: billing retries & grace transitions, low-stock/expiry alert scans,
notification digests, scheduled report emails, 90-day purge. No Hangfire/Quartz yet.

**Rationale**: Cycle 1 jobs are all periodic scans — cron-like loops over indexed
queries; a scheduler framework adds dashboards/storage/config for needs we don't have.
The outbox table also makes webhook/email sends retry-safe.

**Alternatives considered**: Hangfire (adds dashboard + storage schema; revisit when
per-tenant ad-hoc scheduling of custom reports lands in Cycle 3); Azure/queue-based
(couples to a cloud vendor prematurely).

## R10. API surface conventions

**Decision**: Versioned base path `/api/v1`; RFC 7807 problem-details for all errors;
cursor **and** page/size pagination on list endpoints (default 50, max 200); RFC 5988
`Link` headers; filtering via query params; `rowversion`-based optimistic concurrency
with `If-Match`/`ETag` on mutable aggregates; OpenAPI generated by Swashbuckle and
committed contract docs in `contracts/` as the source of truth per constitution
Principle III.

**Rationale**: Satisfies Principle III's versioning + consistent-error mandates and the
spec's breaking-change rule before any external consumer exists.

**Alternatives considered**: GraphQL (poor fit for POS write-heavy flows + fiscal
receipt determinism); gRPC (clients are browsers/mobile web — REST+JSON is the
integration baseline, FR-054's open interface).

## R11. Ghana tax & receipt configuration

**Decision**: Tax engine as per-country configuration data, seeded for Ghana: VAT 15%
plus NHIL 2.5%, GETFund 2.5%, COVID-19 HRL 1% as separate levy lines (compound per GRA
rules), configurable per product tax treatment (standard/zero/exempt). Receipt payload
is a structured document (JSON) with all fiscal fields; rendering/printing is
client-side; a `FiscalFormat` hook point per country is reserved (GRA e-VAT integration
deferred until certification is pursued).

**Rationale**: FR-040/FR-057 require per-country accommodation without redevelopment;
levies-as-lines matches how Ghanaian receipts must present tax; deferring GRA e-VAT
integration keeps Cycle 1 shippable.

**Alternatives considered**: Hard-coding a single VAT rate (breaks the moment rates
change or a second market opens); third-party tax service (none with Ghana coverage
worth the dependency).

## R12. Spreadsheet import/export

**Decision**: CSV and XLSX support using the existing-free `ClosedXML` for XLSX
generation/parsing and built-in CSV handling; import is a two-step API — `POST
/import/products` (upload → parsed preview with per-row errors, nothing saved) then
`POST /import/products/{jobId}/commit`. Exports stream as CSV/XLSX per report (FR-049,
FR-056).

**Rationale**: FR-018 demands preview-before-save; two-step job model also gives the
resumable UX the onboarding checklist needs. ClosedXML is MIT-licensed and avoids Excel
interop.

**Alternatives considered**: EPPlus (license now commercial); CSV-only (spec's
column-matching preview strongly implies real spreadsheet files from real businesses).

## R13. Observability

**Decision**: Serilog with structured JSON console output, request logging middleware
enriched with `TenantId`/`UserId`/`TraceId`, EF Core command logging at warning+ in
production; health endpoints `/health/live` and `/health/ready` for the 99.9% uptime
operations (SC-009).

**Rationale**: Multi-tenant debugging without tenant-tagged logs is guesswork; health
probes are prerequisites for any orchestrated deployment meeting the uptime target.

**Alternatives considered**: OpenTelemetry full tracing stack — worthwhile later;
Serilog now is one package and zero infra, and OTel can layer on top without rework.
