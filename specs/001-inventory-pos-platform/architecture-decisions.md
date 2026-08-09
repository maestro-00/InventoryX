# ADR: Core Architecture Decisions for InventoryX

- **Status:** Accepted
- **Decision date:** 2026-07-26
- **Scope:** Inventory Management & Point of Sale Platform — Cycle 1
- **Related artifacts:** [Implementation plan](./plan.md), [Research decisions](./research.md), [Data model](./data-model.md), and [Project constitution](../../.specify/memory/constitution.md)

This record consolidates five architecture decisions that shape the first delivery cycle of InventoryX. Each decision is accepted for Cycle 1 and may be revisited when scale, product scope, or operational constraints materially change.

## 1. Preserve Clean Architecture with CQRS

### Problem being solved

InventoryX is expanding from a single-business inventory tracker into a multi-tenant SaaS inventory and POS platform. The expansion introduces billing, tenancy, purchasing, stock, sales, reporting, and background processing without removing the need for clear ownership of business rules, isolated testing, and safe replacement of infrastructure services.

### Decision taken

Retain the existing four-project Clean Architecture structure:

- `InventoryX.Domain` owns entities and business rules.
- `InventoryX.Application` owns use cases, MediatR commands and queries, DTOs, validation, and service interfaces.
- `InventoryX.Infrastructure` implements persistence and external integrations.
- `InventoryX.Presentation` owns HTTP controllers, middleware, and application composition.

All application reads and writes continue through MediatR CQRS handlers. New capabilities are organized by domain area within the existing projects instead of creating additional projects. External dependencies are accessed through interfaces declared in the Application layer.

### Alternatives considered

- **Split immediately into microservices:** rejected because Cycle 1 does not justify the deployment, consistency, observability, and operational burden of distributed services.
- **Use a conventional controller-service-repository architecture:** rejected because it would weaken existing layer boundaries and make it easier for business logic to drift into controllers or persistence code.
- **Replace MediatR and AutoMapper with custom dispatch and mapping:** rejected because it would destabilize established handlers without delivering a user-visible benefit at the current scale.

### Consequences

**Positive**

- Business rules remain independent of HTTP, SQL Server, and third-party providers.
- Existing conventions and test assets can be reused as the feature set grows.
- Cross-cutting validation, auditing, and plan enforcement can be implemented once in the MediatR pipeline.
- Infrastructure implementations can change without rewriting application use cases.

**Negative**

- Simple features require several types and files across multiple projects.
- CQRS, mapping profiles, and pipeline behaviors add indirection and a learning curve.
- A single deployable application can develop internal coupling unless module boundaries are actively reviewed.

### Tradeoffs

The project accepts additional ceremony and indirection in exchange for consistent boundaries, testability, and lower change risk. It favors a modular monolith that can be split later over paying distributed-system costs before they are necessary.

## 2. Use Shared-Schema Multi-Tenancy with Enforced Tenant Isolation

### Problem being solved

The SaaS platform must serve many independent businesses while preventing cross-tenant data access. It also needs a cost-effective launch model, a single migration path, and tenant-aware usage enforcement and reporting.

### Decision taken

Use one SQL Server database and one shared schema. Every tenant-owned row carries a GUID `TenantId`. Tenant isolation is enforced centrally through:

- EF Core global query filters driven by an `ITenantContext` resolved from the JWT `tenant_id` claim;
- a `SaveChanges` interceptor that stamps new tenant-owned rows and rejects cross-tenant writes; and
- integration tests that explicitly attempt to bypass tenant boundaries.

### Alternatives considered

- **Database per tenant:** deferred because self-service growth would multiply provisioning, connection management, backup, and migration work.
- **Schema per tenant:** rejected because it creates SQL Server schema sprawl and complicates migrations and tooling.
- **Developer-applied tenant predicates only:** rejected because a missed `WHERE TenantId = ...` could expose customer data.

### Consequences

**Positive**

- New tenants require no database or schema provisioning.
- All tenants follow one EF Core migration path.
- Shared infrastructure keeps launch and operating costs comparatively low.
- Central filters and write guards reduce reliance on every developer remembering tenant predicates.

**Negative**

- A filter bypass, incorrect context, or unsafe raw SQL can affect multiple tenants.
- Large tenants share database resources and may create noisy-neighbor effects.
- Per-tenant backup, restore, export, and physical data residency are harder than with isolated databases.
- Most indexes and uniqueness constraints must include `TenantId`.

### Tradeoffs

The project chooses operational simplicity and efficient resource sharing over maximum physical isolation. Strict automated isolation tests and centralized enforcement are mandatory compensating controls. Database-per-tenant remains a possible Enterprise-tier evolution if compliance or scale requires it.

## 3. Model Inventory as an Append-Only Ledger with a Transactional Projection

### Problem being solved

Inventory changes must be traceable and correct across sales, returns, receipts, transfers, counts, and adjustments. At the same time, the POS scan path must read available stock quickly and cannot repeatedly sum a large movement history.

### Decision taken

Store every inventory change as an immutable `StockMovement` containing quantity delta, movement type, item or variant, batch, location, user, reason, timestamp, and correlation ID. Maintain `StockLevel` as the current-balance projection, updated in the same database transaction as the movement. Corrections create compensating movements instead of modifying history. Batch-tracked issues use FEFO selection.

### Alternatives considered

- **Store only the current stock quantity:** rejected because it cannot explain how a balance was reached and makes fraud or error investigation difficult.
- **Calculate current stock by summing movements on every read:** rejected because query cost grows with history and threatens POS latency targets.
- **Adopt full event sourcing with a dedicated event store:** rejected because it introduces new infrastructure and a new consistency model when an append-only relational ledger satisfies Cycle 1 needs.

### Consequences

**Positive**

- Every stock change has an auditable history.
- Current stock reads remain fast and predictable.
- Reconciliation can compare the projection against the movement ledger.
- Compensating entries preserve evidence of mistakes and corrections.

**Negative**

- Every stock mutation must update two representations atomically.
- Defects in projection updates can cause the balance and ledger to diverge.
- The ledger grows continuously and needs appropriate indexing, retention, and archival planning.
- Corrections are operationally less intuitive than editing a quantity in place.

### Tradeoffs

The design duplicates derived stock state to gain both auditability and low-latency reads. That duplication is accepted only with transactional updates, idempotent commands, reconciliation checks, and tests covering every movement type.

## 4. Synchronize Offline Sales with Idempotent Client IDs and Explicit Conflict Flags

### Problem being solved

POS clients must continue selling during unreliable connectivity. Retried uploads must not create duplicate sales, and stock may have changed elsewhere before an offline sale reaches the server. Rejecting a real completed sale would make the system financially inaccurate, while silently rewriting stock would hide inconsistencies.

### Decision taken

Provide clients with a versioned catalogue snapshot and `rowversion`-based deltas. Each offline sale carries a client-generated UUID (`ClientSaleId`) plus its device and register identifiers. Batch upload is idempotent on `ClientSaleId`: replaying an upload returns the original result without duplicating the sale. The server records the sale and applies its stock effect on ingestion. If the result conflicts with concurrent movement or would make stock negative, it records a `StockConflict` and raises a notification for review rather than silently adjusting or discarding the sale.

### Alternatives considered

- **Reject conflicting offline sales:** rejected because the customer transaction already occurred and must remain in the financial record.
- **Use last-write-wins reconciliation:** rejected because it silently loses stock information.
- **Use CRDT or operational-transform reconciliation:** rejected because its complexity is disproportionate to Cycle 1 inventory semantics.
- **Require continuous connectivity:** rejected because offline-capable POS operation is a core requirement.

### Consequences

**Positive**

- At-least-once retries do not duplicate revenue, payment, or stock effects.
- Cashiers can continue operating through connectivity failures.
- The system preserves completed sales while making inventory uncertainty visible.
- Client and correlation identifiers provide a clear synchronization audit trail.

**Negative**

- Temporarily negative or disputed stock can exist after synchronization.
- Conflict resolution requires an operational review workflow and notifications.
- Client devices must persist stable IDs and upload state correctly.
- Snapshot versioning and backward compatibility add API and testing complexity.

### Tradeoffs

The project prioritizes truthful financial capture and availability over immediately consistent inventory. It accepts explicit, reviewable stock conflicts rather than losing real sales or pretending concurrent offline operations never disagreed.

## 5. Integrate Paystack through an Application Port and Own Subscription State

### Problem being solved

InventoryX needs subscription billing that supports Ghanaian cards and mobile money while enforcing product-specific trial, grace-period, read-only, cancellation, and retention rules. The platform must not store card data and should avoid coupling its core subscription behavior to one provider.

### Decision taken

Integrate Paystack through direct REST calls in an Infrastructure `PaystackGateway` implementing the Application-layer `IPaymentGateway` interface. Use provider webhooks with signature verification for payment confirmation. Keep the InventoryX subscription state machine internally:

`Trial → Active → PastDue (7-day grace) → ReadOnly → Cancelled → Purged after 90 days`

A background worker advances time-based states and retries work. Paystack handles payment rails; InventoryX remains the source of truth for product access and retention policy.

### Alternatives considered

- **Stripe:** rejected for launch because it does not provide the required Ghana mobile-money coverage.
- **Direct mobile-network integrations:** rejected because MTN, Telecel, and AT would require separate integrations in addition to card processing.
- **Paystack Plans as the subscription source of truth:** rejected because provider states do not exactly represent InventoryX trial, grace, read-only, and purge semantics.
- **Flutterwave or Hubtel:** retained as possible future providers, but not selected for the initial implementation.

### Consequences

**Positive**

- One provider supports the initial card and Ghana mobile-money methods.
- Card details remain outside InventoryX's storage and processing boundary.
- Business access rules stay deterministic and provider-independent.
- The Application interface makes an alternative or additional gateway feasible.

**Negative**

- InventoryX must reconcile webhook delivery, duplicate events, delayed events, and provider outages.
- Owning subscription state requires background processing, monitoring, and recovery procedures.
- The direct REST integration must track Paystack API changes.
- Initial geographic coverage and payment behavior are optimized for Ghana rather than every future market.

### Tradeoffs

The project accepts responsibility for subscription orchestration to preserve exact product semantics and provider portability. Paystack reduces launch complexity for Ghana, while the gateway boundary limits—but does not eliminate—future migration cost.

## Review Triggers

Revisit these decisions when any of the following becomes true:

- independent deployment or scaling of a domain becomes necessary;
- regulation, residency, or large-tenant performance requires physical tenant isolation;
- ledger volume or replay requirements justify dedicated event-stream infrastructure;
- offline conflict frequency makes automated reconciliation necessary; or
- expansion beyond Ghana requires multiple payment gateways or a different billing platform.
