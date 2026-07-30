# Tasks: Inventory Management & Point of Sale Platform — Cycle 1

**Input**: Design documents from `/specs/001-inventory-pos-platform/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: INCLUDED — constitution Principle II makes TDD non-negotiable. Every story
phase lists failing-first tests before implementation; a task is done only when its
tests are green and `dotnet test InventoryX.sln` passes.

**Organization**: Grouped by user story (spec priorities). Cycle 1 scope per plan:
US1–US8, with variant items folded into US1 and batch/expiry into US7. US9/US10
remainder are later cycles.

**Story labels**: [US1] onboard+first sale · [US2] full checkout · [US3] multi-location
stock · [US4] offline sync · [US5] subscription billing · [US6] shifts & cash ·
[US7] purchasing + batch/expiry · [US8] dashboard & reports

## Format: `[ID] [P?] [Story] Description`

- **[P]**: parallelizable (different files, no dependency on an incomplete task)
- Paths use the existing solution layout (plan.md → Project Structure)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: New packages, cross-cutting plumbing, CI — no domain logic

- [X] T001 Add packages: FluentValidation.DependencyInjectionExtensions to InventoryX.Application/InventoryX.Application.csproj; Serilog.AspNetCore to InventoryX.Presentation/InventoryX.Presentation.csproj; ClosedXML to InventoryX.Infrastructure/InventoryX.Infrastructure.csproj
- [X] T002 [P] Configure Serilog structured JSON logging + request logging in InventoryX.Presentation/Program.cs and appsettings.json (env-var driven, no secrets)
- [X] T003 [P] Add RFC 7807 problem-details middleware (validation, 402 plan-limit, 409 concurrency/state, 423 approval shapes) in InventoryX.Presentation/Middleware/ProblemDetailsMiddleware.cs
- [X] T004 [P] Add health endpoints /health/live and /health/ready in InventoryX.Presentation/Program.cs
- [X] T005 Create versioned controller convention: base route /api/v1, folder InventoryX.Presentation/Controllers/v1/ with ApiControllerBase.cs (tenant/user accessors, ETag helpers)
- [X] T006 [P] Add CI workflow building solution and running all tests on PR in .github/workflows/ci.yml (red pipeline blocks merge — constitution G5)
- [X] T007 [P] Add PagedResult<T> envelope + pagination binding helpers in InventoryX.Application/DTOs/Common/PagedResult.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Multi-tenancy, identity claims, MediatR behaviors, schema evolution — no user story can start before this completes

**⚠️ CRITICAL**: Tenant isolation is the constitution's release-blocking rule; it lands here, test-first.

- [X] T008 [P] Write failing integration tests for tenant isolation (global query filter + cross-tenant write rejection) in tests/InventoryX.Infrastructure.Tests/Data/TenantIsolationTests.cs
- [X] T009 [P] Write failing unit tests for tenant resolution middleware (claim → ITenantContext, missing claim → 401) in tests/InventoryX.Presentation.Tests/Middleware/TenantResolutionMiddlewareTests.cs
- [X] T010 Extend BaseModel with TenantId, UpdatedAt/UpdatedBy, rowversion concurrency token in InventoryX.Domain/Models/Common/BaseModel.cs
- [X] T011 [P] Create Tenant entity (country, currency GHS default, business type, valuation method, thresholds, onboarding checklist JSON) in InventoryX.Domain/Models/Tenancy/Tenant.cs
- [X] T012 [P] Create PlanDefinition and UsageCounter entities in InventoryX.Domain/Models/Tenancy/PlanDefinition.cs and UsageCounter.cs
- [X] T013 [P] Create Subscription entity with status enum (Trialing|Active|PastDue|ReadOnly|Cancelled|PurgePending) and period/grace/purge fields in InventoryX.Domain/Models/Tenancy/Subscription.cs
- [X] T014 [P] Create append-only AuditLogEntry entity, Notification entity (type, channel, consolidation key) and INotificationService raise mechanism (consumed by US4/US5/US6 before US7's alert scanner) in InventoryX.Domain/Models/Auditing/AuditLogEntry.cs, Notification.cs + InventoryX.Application/Services/IServices/INotificationService.cs
- [X] T015 Define ITenantContext in InventoryX.Application/Services/IServices/ITenantContext.cs; implement HttpTenantContext in InventoryX.Infrastructure/Services/HttpTenantContext.cs
- [X] T016 Add tenant-resolution middleware populating ITenantContext from JWT tenant_id claim in InventoryX.Presentation/Middleware/TenantResolutionMiddleware.cs (makes T009 green)
- [X] T017 Apply EF global query filters on all tenant-owned entities + SaveChanges interceptor stamping TenantId and rejecting cross-tenant writes in InventoryX.Infrastructure/Data/ (makes T008 green)
- [X] T018 Extend Identity user with TenantId, IsOwner; issue JWT with tenant_id, role, location_scope claims (update token service and Google flow) in InventoryX.Infrastructure and InventoryX.Application/Services
- [X] T019 Create Role with permission atoms (Sell, Refund+limit, Discount+maxPercent, VoidSale, ViewProfit, ManageStock, ManagePurchasing, ManagePricing, ManageUsers, ViewReports, ApproveAdjustments) and seed six fixed roles in InventoryX.Domain/Models/Tenancy/Role.cs + InventoryX.Infrastructure/Data/Seed/RoleSeeder.cs
- [X] T020 Add FluentValidation ValidationBehavior<TRequest,TResponse> MediatR pipeline stage + DI registration in InventoryX.Application/Behaviors/ValidationBehavior.cs
- [X] T021 [P] Add AuditBehavior writing AuditLogEntry for sensitive commands (marker interface IAuditedCommand) in InventoryX.Application/Behaviors/AuditBehavior.cs
- [X] T022 [P] Add IPlanEnforcer + PlanEnforcementBehavior skeleton (feature gates, limit checks → 402 problem) in InventoryX.Application/Services/IServices/IPlanEnforcer.cs and InventoryX.Application/Behaviors/PlanEnforcementBehavior.cs
- [X] T023 [P] Seed the four PlanDefinitions (caps: Free 300 / Standard 3,000 monthly sales; limits per spec FR-009/010) in InventoryX.Infrastructure/Data/Seed/PlanSeeder.cs
- [X] T024 [P] Create TaxTreatment entity + Ghana seed (GH-STD: VAT 15% + NHIL 2.5% + GETFund 2.5% + COVID 1% per research R11; GH-ZERO; GH-EXEMPT) in InventoryX.Domain/Models/Catalog/TaxTreatment.cs + InventoryX.Infrastructure/Data/Seed/TaxSeeder.cs
- [X] T025 Schema-evolution migration with data preservation: InventoryItemType→Category, InventoryItem→Product, RetailStock→StockLevel, SaleGroup→Sale/Sale→SaleLine, Purchase→GoodsReceipt history (research R4) in InventoryX.Infrastructure/Migrations/
- [X] T026 Remove pre-versioned routes; regenerate baseline migration; verify dotnet ef database update from clean DB per quickstart.md

**Checkpoint**: Isolation tests green, seeds applied — user stories can begin (in parallel if staffed)

---

## Phase 3: User Story 1 — Sign Up, Onboard, First Sale (P1) 🎯 MVP

**Goal**: Account → location → products (manual/import/variants) → opening stock → a completed sale that decrements stock

**Independent Test** (spec US1 / quickstart scenario A): fresh account, product with stock 10, sell 2, stock reads 8, sale in history

### Tests (write first, must fail)

- [X] T027 [P] [US1] Failing tests for RegisterTenantCommand (tenant+owner+Trialing sub created, defaults by business type) in tests/InventoryX.Application.Tests/Tenancy/RegisterTenantCommandTests.cs
- [X] T028 [P] [US1] Failing tests for Product/Category/Variant command+query handlers and validators (sku uniqueness, variant matrix) in tests/InventoryX.Application.Tests/Catalog/
- [X] T029 [P] [US1] Failing tests for StockLedger (movement append + StockLevel projection, opening stock) in tests/InventoryX.Application.Tests/Inventory/StockLedgerTests.cs
- [X] T030 [P] [US1] Failing tests for CreateSaleCommand (stock decrement, Ghana tax snapshot math from quickstart A, open-shift invariant) in tests/InventoryX.Application.Tests/Selling/CreateSaleCommandTests.cs
- [X] T031 [P] [US1] Failing end-to-end scenario A test (register→location→product→stock→sale→stock=8) in tests/InventoryX.Presentation.Tests/Scenarios/FirstSaleScenarioTests.cs

### Implementation

- [X] T032 [US1] RegisterTenantCommand + handler + validator (tenant, owner user, Trialing Professional subscription, onboarding checklist init) in InventoryX.Application/Commands/{Requests,RequestHandlers}/Tenancy/
- [X] T033 [US1] AuthController v1: register, login, refresh, google, 2FA enroll/verify per contracts/auth-tenancy.md in InventoryX.Presentation/Controllers/v1/AuthController.cs
- [X] T034 [P] [US1] Location entity + CRUD commands/queries/validators + LocationsController (plan cap via PlanEnforcer) in InventoryX.Domain/Models/Inventory/Location.cs, InventoryX.Application/.../Inventory/, InventoryX.Presentation/Controllers/v1/LocationsController.cs
- [X] T035 [US1] Evolve Category (tree, unique name per parent) + CategoriesController in InventoryX.Domain/Models/Catalog/Category.cs + InventoryX.Presentation/Controllers/v1/CategoriesController.cs
- [X] T036 [US1] Evolve Product (TrackingMode Simple|Variant|Batch, UoM, AllowFractional, prices, tax treatment, reorder fields, custom fields, status) + ProductVariant entity in InventoryX.Domain/Models/Catalog/Product.cs and ProductVariant.cs
- [X] T037 [US1] Product commands/queries/validators (create/update/variants/search/barcode lookup, cost fields redacted without ViewProfit) + ProductsController per contracts/catalog-import.md in InventoryX.Application/.../Catalog/ + InventoryX.Presentation/Controllers/v1/ProductsController.cs
- [X] T038 [US1] StockLevel + StockMovement entities and IStockLedger service (append-only movement + projection update in one transaction, research R5) in InventoryX.Domain/Models/Inventory/ + InventoryX.Application/Services/IServices/IStockLedger.cs + InventoryX.Infrastructure/Services/StockLedger.cs
- [X] T039 [US1] Opening-stock entry via adjustment command (reason Correction) wired to ledger in InventoryX.Application/Commands/.../Inventory/RecordOpeningStockCommand.cs
- [X] T040 [US1] ImportJob entity + two-step product & opening-stock import (upload→mapping→row-preview→commit, ClosedXML, nothing saved before commit) + ImportController per contracts/catalog-import.md in InventoryX.Domain/Models/Catalog/ImportJob.cs, InventoryX.Application/.../Import/, InventoryX.Infrastructure/Services/SpreadsheetImportService.cs, InventoryX.Presentation/Controllers/v1/ImportController.cs
- [X] T041 [US1] Minimal Register + Shift entities with open-shift command (full cash mgmt lands in US6) in InventoryX.Domain/Models/Selling/Register.cs, Shift.cs + InventoryX.Application/Commands/.../Selling/OpenShiftCommand.cs
- [X] T042 [US1] CreateSaleCommand: Completed sale, cash tender, price+tax snapshot per line (Ghana components), ledger decrement, UsageCounter increment + SalesController POST/GET per contracts/pos-sales-sync.md in InventoryX.Application/Commands/.../Selling/ + InventoryX.Presentation/Controllers/v1/SalesController.cs
- [X] T043 [US1] Tenant profile + onboarding checklist GET/PATCH, sample data load/remove per contracts/auth-tenancy.md in InventoryX.Application/.../Tenancy/ + InventoryX.Presentation/Controllers/v1/TenantController.cs
- [X] T044 [US1] AutoMapper profiles + DTOs for tenancy/catalog/inventory/selling areas in InventoryX.Application/DTOs/ and mapping profiles in InventoryX.Application/Extensions/

**Checkpoint**: Scenario A green end-to-end — MVP demonstrable

---

## Phase 4: User Story 2 — Fast Checkout: Payments, Receipts, Returns (P2)

**Goal**: Split tenders, change due, held sales, role-capped discounts, receipts with Ghana levy lines, returns/exchanges/voids

**Independent Test** (spec US2): 3-item sale via scan/search, split cash+card, receipt issued, one item returned against receipt — stock and refund records correct

### Tests (write first, must fail)

- [X] T045 [P] [US2] Failing tests for split tender + change due + tender sum validation in tests/InventoryX.Application.Tests/Selling/SalePaymentTests.cs
- [X] T046 [P] [US2] Failing tests for return/exchange rules (original price+tax, threshold/receiptless → authorization, quarantine disposition) in tests/InventoryX.Application.Tests/Selling/ReturnCommandTests.cs
- [X] T047 [P] [US2] Failing tests for discount cap per role + escalation + audit attribution in tests/InventoryX.Application.Tests/Selling/DiscountPolicyTests.cs

### Implementation

- [X] T048 [US2] SalePayment multi-tender support (Cash|Card|MobileMoney|BankTransfer|Cheque recorded tenders, split, change due; StoreCredit/GiftCard/loyalty/on-account deferred per spec Assumptions — enum values reserved) in CreateSaleCommand + InventoryX.Domain/Models/Selling/SalePayment.cs
- [X] T049 [US2] Held sales: Held status (no stock effect), complete/recall/list endpoints per contracts/pos-sales-sync.md; plus FavouritesLayout entity (per-register configurable grid, FR-038) with GET/PUT /registers/{id}/favourites in InventoryX.Application/.../Selling/ + InventoryX.Domain/Models/Selling/FavouritesLayout.cs + SalesController/RegistersController
- [X] T050 [US2] Line/sale discount validation against role MaxPercent with manager escalation + AuditBehavior coverage in InventoryX.Application/Validators/Selling/
- [X] T051 [US2] Receipt entity (sequential per-tenant numbering, structured fiscal payload with levy lines) + GET receipt + tenant receipt-template endpoints in InventoryX.Domain/Models/Selling/Receipt.cs + InventoryX.Application/Services/IServices/IReceiptBuilder.cs + controllers
- [X] T052 [US2] Receipt delivery: email via existing SendGrid service, SMS/QR delivery log in InventoryX.Infrastructure/Services/ReceiptDeliveryService.cs + POST /sales/{id}/receipt/deliver
- [X] T053 [US2] ReturnTransaction + exchange (return + new sale, difference settled) commands with authorization gates and ToStock|Quarantine disposition; refund destinations Original|Cash in Cycle 1 (StoreCredit deferred with its ledger) in InventoryX.Domain/Models/Selling/ReturnTransaction.cs + InventoryX.Application/.../Selling/ + ReturnsController
- [X] T054 [US2] Void sale command (permission-gated, audited) + sales lookup by receiptNumber/search + product availability endpoint in InventoryX.Application/.../Selling/ + SalesController
- [X] T055 [US2] Typo-tolerant product search (trigram/LIKE fallback strategy) in the products list query in InventoryX.Application/Queries/RequestHandlers/Catalog/GetProductsQueryHandler.cs

**Checkpoint**: US1+US2 = a sellable single-location POS backend

---

## Phase 5: User Story 3 — Multi-Location Stock Operations (P3)

**Goal**: Transfers with in-transit state, approval-gated adjustments, counts with variance approval, immutable ledger queries

**Independent Test** (spec US3): transfer 10 units, verify in-transit, receive 8 with discrepancy flagged, spot-check variance approved — permanent audit trail throughout

### Tests (write first, must fail)

- [X] T056 [P] [US3] Failing tests for transfer state machine (Draft→Dispatched→Received/WithDiscrepancy, InTransit quantities) in tests/InventoryX.Application.Tests/Inventory/StockTransferTests.cs
- [X] T057 [P] [US3] Failing tests for adjustment approval threshold (approver ≠ requester) in tests/InventoryX.Application.Tests/Inventory/AdjustmentApprovalTests.cs
- [X] T058 [P] [US3] Failing tests for count variance calculation + approval posting corrections in tests/InventoryX.Application.Tests/Inventory/StockCountTests.cs

### Implementation

- [X] T059 [US3] StockTransfer entity + dispatch/receive commands (QtyInTransit maintenance, discrepancy reason) + TransfersController per contracts/inventory.md in InventoryX.Domain/Models/Inventory/StockTransfer.cs + InventoryX.Application/.../Inventory/ + InventoryX.Presentation/Controllers/v1/TransfersController.cs
- [X] T060 [US3] AdjustmentReason seeding + adjustment create/approve/reject flow with tenant threshold in InventoryX.Domain/Models/Inventory/AdjustmentReason.cs + InventoryX.Application/.../Inventory/ + StockController
- [X] T061 [P] [US3] Consumption command (internal use write-off) in InventoryX.Application/Commands/.../Inventory/RecordConsumptionCommand.cs
- [X] T062 [US3] StockCount entity + open/submit-lines/submit/approve/reject flow posting CountCorrection movements + CountsController in InventoryX.Domain/Models/Inventory/StockCount.cs + InventoryX.Application/.../Inventory/ + InventoryX.Presentation/Controllers/v1/CountsController.cs
- [X] T063 [US3] Stock queries: levels with business-wide rollup (groupBy=product), paged movement ledger with filters + StockController GETs per contracts/inventory.md in InventoryX.Application/Queries/.../Inventory/
- [X] T064 [US3] Movement correction command creating compensating entries (originals immutable) in InventoryX.Application/Commands/.../Inventory/CorrectMovementCommand.cs
- [X] T065 [US3] Enforce Manager location_scope on all location-bound operations (authorization handler) in InventoryX.Presentation/Middleware/LocationScopeAuthorizationHandler.cs

**Checkpoint**: Multi-location inventory truth with full audit trail

---

## Phase 6: User Story 4 — Offline Selling & Sync (P4)

**Goal**: Catalogue snapshot deltas, idempotent queued-sale upload, conflict flagging, register-scoped PIN tokens

**Independent Test** (spec US4 / quickstart B): replayed ClientSaleId doesn't duplicate; oversell syncs as applied_with_conflict and appears in conflicts list

### Tests (write first, must fail)

- [X] T066 [P] [US4] Failing tests for idempotent ingest (same ClientSaleId replay returns original result, no duplicate stock effect) in tests/InventoryX.Application.Tests/Sync/OfflineSaleIngestTests.cs
- [X] T067 [P] [US4] Failing tests for conflict flagging on negative/contested stock (sale recorded, StockConflictFlag set, no silent overwrite) in tests/InventoryX.Application.Tests/Sync/StockConflictTests.cs

### Implementation

- [X] T068 [US4] Snapshot delta query by rowversion watermark (products, variants, prices, tax, stock for register's location) + GET /sync/snapshot in InventoryX.Application/Queries/.../Sync/ + InventoryX.Presentation/Controllers/v1/SyncController.cs
- [X] T069 [US4] Batch offline-sale ingest command: per-sale idempotent upsert on (TenantId, ClientSaleId), per-sale result applied|applied_with_conflict|rejected, OccurredAt honored + POST /sync/sales in InventoryX.Application/Commands/.../Sync/IngestOfflineSalesCommand.cs
- [X] T070 [US4] Conflict review: GET /sync/conflicts + resolve (acceptAsIs | adjustWithReason → movement) with notification raise in InventoryX.Application/.../Sync/ + SyncController
- [X] T071 [US4] RegisterPin entity + PIN set endpoint + /auth/pin/exchange issuing register-scoped short-lived JWT (research R3) in InventoryX.Domain/Models/Tenancy/RegisterPin.cs + InventoryX.Application/.../Auth/ + AuthController
- [ ] T072 [US4] Mark live-only endpoints in OpenAPI (operation extension) so clients can grey them out offline in InventoryX.Presentation/ Swagger configuration

**Checkpoint**: Quickstart scenario B passes — offline honesty guaranteed

---

## Phase 7: User Story 5 — Subscription & Billing Lifecycle (P5)

**Goal**: Paystack card/MoMo billing, trial→Free fallback, upgrades/downgrades, 7-day grace → read-only, 90-day retention, export

**Independent Test** (spec US5 / quickstart D): trial expiry → Free; 301st Free sale → 402; forced ReadOnly blocks writes but export works

### Tests (write first, must fail)

- [ ] T073 [P] [US5] Failing tests for subscription state machine (all transitions incl. trial expiry → Free/Active, grace → ReadOnly, cancel → purge clock) in tests/InventoryX.Application.Tests/Billing/SubscriptionStateMachineTests.cs
- [ ] T074 [P] [US5] Failing tests for plan enforcement (module gates, 301st sale 402 with upgradeHint, ReadOnly write-block except export/billing) in tests/InventoryX.Application.Tests/Billing/PlanEnforcementTests.cs
- [ ] T075 [P] [US5] Failing tests for Paystack webhook signature verification + event idempotency in tests/InventoryX.Infrastructure.Tests/Services/PaystackWebhookTests.cs

### Implementation

- [ ] T076 [US5] IPaymentGateway abstraction + PaystackGateway (initialize authorization, charge, verify; card + mtn/telecel/at MoMo channels; PaystackOptions from env) in InventoryX.Application/Services/IServices/IPaymentGateway.cs + InventoryX.Infrastructure/Services/PaystackGateway.cs + InventoryX.Application/Options/PaystackOptions.cs
- [ ] T077 [P] [US5] GET /billing/plans and GET /billing/subscription (status, usage vs limits) per contracts/billing.md in InventoryX.Application/Queries/.../Billing/ + InventoryX.Presentation/Controllers/v1/BillingController.cs
- [ ] T078 [US5] Upgrade (immediate, pro-rata charge) / downgrade (period-end, over-limit acknowledgement) / cancel / reactivate commands in InventoryX.Application/Commands/.../Billing/
- [ ] T079 [US5] Payment-method initialization endpoint (card | mobile_money + provider + msisdn) in BillingController + gateway wiring
- [ ] T080 [US5] Paystack webhook endpoint: signature check, idempotent by event id, drives charge success/failure into state machine in InventoryX.Presentation/Controllers/v1/PaystackWebhookController.cs
- [ ] T081 [US5] BillingInvoice generation + SendGrid email + list/PDF endpoints + billing contact PATCH in InventoryX.Domain/Models/Tenancy/BillingInvoice.cs + InventoryX.Application/.../Billing/ + InventoryX.Infrastructure/Services/InvoicePdfService.cs
- [ ] T082 [US5] Outbox table + BillingWorker BackgroundService: renewals, 7-day retry ladder with owner notifications, grace→ReadOnly, trial expiry→Free, purge at 90 days after final warning in InventoryX.Infrastructure/BackgroundJobs/BillingWorker.cs
- [ ] T083 [US5] Complete PlanEnforcementBehavior: UsageCounter maintenance on creates/sales, monthly cap checks, ReadOnly write-block (export/billing exempt) in InventoryX.Application/Behaviors/PlanEnforcementBehavior.cs
- [ ] T084 [US5] Full data export job (all tenant data → downloadable archive; available in every subscription state) + /tenant/export endpoints in InventoryX.Application/.../Tenancy/ + InventoryX.Infrastructure/Services/TenantExportService.cs

**Checkpoint**: Quickstart scenario D passes — the commercial engine works

---

## Phase 8: User Story 6 — Register Shifts & Cash Management (P6)

**Goal**: Opening float, cash in/out, mandatory counted close, variance flags, Z-report

**Independent Test** (spec US6): open with float, mixed-tender sales, petty cash out, close with shortfall — variance computed, flagged, on Z-report

### Tests (write first, must fail)

- [ ] T085 [P] [US6] Failing tests for shift close (uncounted drawer → 400, expected-cash computation from tenders + movements, variance threshold flag) in tests/InventoryX.Application.Tests/Selling/ShiftCloseTests.cs
- [ ] T086 [P] [US6] Failing tests for Z-report aggregation (sales, tenders, refunds, discounts, voids, variance per register+staff) in tests/InventoryX.Application.Tests/Selling/ZReportTests.cs

### Implementation

- [ ] T087 [US6] CashMovement entity + cash in/out command with reasons + shift-close command computing expected cash and variance in InventoryX.Domain/Models/Selling/CashMovement.cs + InventoryX.Application/.../Selling/
- [ ] T088 [US6] Variance-above-threshold manager notification + UnusualVoids detection hook in InventoryX.Application/Commands/RequestHandlers/Selling/CloseShiftCommandHandler.cs
- [ ] T089 [US6] Z-report query + GET /shifts/{id}/z-report in InventoryX.Application/Queries/.../Selling/ + ShiftsController per contracts/pos-sales-sync.md
- [ ] T090 [US6] Registers CRUD with plan cap + concurrent-shift prevention + sale-requires-open-shift enforcement finalized in InventoryX.Presentation/Controllers/v1/RegistersController.cs + validators

**Checkpoint**: Daily POS operating cycle complete (quickstart A step 8 fully honored)

---

## Phase 9: User Story 7 — Purchasing, Suppliers, Batch & Expiry (P7)

**Goal**: Supplier records, PO state machine with approvals, goods receipts creating batches, FEFO issue, recall trace, landed costs, reorder suggestions, alerts

**Independent Test** (spec US7 / quickstart C): PO above threshold needs approval; short delivery keeps order open; sale consumes earliest-expiry batch; batch traces both ways

### Tests (write first, must fail)

- [ ] T091 [P] [US7] Failing tests for PO state machine + value-threshold approval (illegal transition → 409) in tests/InventoryX.Application.Tests/Purchasing/PurchaseOrderStateTests.cs
- [ ] T092 [P] [US7] Failing tests for FEFO batch selection at sale + batch-required validation for batch-tracked products in tests/InventoryX.Application.Tests/Inventory/FefoIssueTests.cs
- [ ] T093 [P] [US7] Failing tests for receipt of short/over/damaged deliveries (order stays open or closes short with reason) in tests/InventoryX.Application.Tests/Purchasing/GoodsReceiptTests.cs

### Implementation

- [ ] T094 [US7] Supplier entity + CRUD + supplier-product links (codes, prices) + SuppliersController per contracts/purchasing.md in InventoryX.Domain/Models/Purchasing/Supplier.cs + InventoryX.Application/.../Purchasing/ + InventoryX.Presentation/Controllers/v1/SuppliersController.cs
- [ ] T095 [US7] PurchaseOrder + lines with full state machine, approval gate, origin tracking + PurchaseOrdersController in InventoryX.Domain/Models/Purchasing/PurchaseOrder.cs + InventoryX.Application/.../Purchasing/ + InventoryX.Presentation/Controllers/v1/PurchaseOrdersController.cs
- [ ] T096 [P] [US7] PO PDF generation + email-to-supplier send in InventoryX.Infrastructure/Services/PurchaseOrderPdfService.cs + send endpoint
- [ ] T097 [US7] Batch entity + GoodsReceipt flow (batch number + expiry capture, ledger Receipt movements, PO Partially/FullyReceived updates, close-short) in InventoryX.Domain/Models/Inventory/Batch.cs + InventoryX.Domain/Models/Purchasing/GoodsReceipt.cs + InventoryX.Application/.../Purchasing/
- [ ] T098 [US7] FEFO batch selection in sale/issue paths (earliest expiry first, explicit batchId override) wired into StockLedger + CreateSaleCommand in InventoryX.Infrastructure/Services/StockLedger.cs
- [ ] T099 [P] [US7] Batch recall trace query (backward: supplier/receipt; forward: sales) + GET /batches/{id}/trace in InventoryX.Application/Queries/.../Inventory/
- [ ] T100 [US7] SupplierInvoice recording with price-variance flag + landed-cost allocation across receipt lines (recalculates unit costs per valuation method) in InventoryX.Domain/Models/Purchasing/SupplierInvoice.cs + InventoryX.Application/.../Purchasing/
- [ ] T101 [US7] Reorder suggestions query (sales rate × lead time grouping by supplier) + apply→draft POs per contracts/inventory.md in InventoryX.Application/Queries/.../Purchasing/ReorderSuggestionsQueryHandler.cs
- [ ] T102 [US7] AlertScanWorker BackgroundService raising via the Foundational Notification entity/service from T014 (low/out-of-stock, expiry horizon, overstock, slow-moving; consolidation by key) in InventoryX.Infrastructure/BackgroundJobs/AlertScanWorker.cs
- [ ] T103 [P] [US7] Supplier performance metrics (on-time rate, achieved lead time, price history) derived query in InventoryX.Application/Queries/.../Purchasing/SupplierPerformanceQueryHandler.cs

**Checkpoint**: Quickstart scenario C passes — replenishment + pharmacy/food readiness

---

## Phase 10: User Story 8 — Dashboard & Essential Reports (P8)

**Goal**: Manager dashboard, sales/profit/stock/purchasing/staff/tax reports with filters + export + scheduling, notification preferences & feed

**Independent Test** (spec US8): dashboard figures match seeded records and drill through; filtered sales report exports; weekly email schedule fires

### Tests (write first, must fail)

- [ ] T104 [P] [US8] Failing tests for dashboard aggregates (today vs same day last week) + ViewProfit redaction (FR-050) in tests/InventoryX.Application.Tests/Reports/DashboardQueryTests.cs
- [ ] T105 [P] [US8] Failing tests for Ghana tax report totals by rate/levy and period in tests/InventoryX.Application.Tests/Reports/TaxReportTests.cs

### Implementation

- [ ] T106 [US8] Dashboard query + GET /dashboard with detailUrl per figure in InventoryX.Application/Queries/.../Reports/ + InventoryX.Presentation/Controllers/v1/DashboardController.cs
- [ ] T107 [US8] Report queries: sales, profit (gated), stock (valuation applied), purchasing, staff/operations, Ghana tax — common filter binding (from/to/location/category/staff) in InventoryX.Application/Queries/.../Reports/ + ReportsController per contracts/reports-notifications.md
- [ ] T108 [US8] Export pipeline: CSV/XLSX (ClosedXML) and PDF streaming; async job + poll (202) for long ranges in InventoryX.Infrastructure/Services/ReportExportService.cs
- [ ] T109 [US8] Report schedules (daily/weekly/monthly, recipients) + ReportScheduleWorker emailing via SendGrid in InventoryX.Domain/Models/Auditing/ReportSchedule.cs + InventoryX.Infrastructure/BackgroundJobs/ReportScheduleWorker.cs
- [ ] T110 [US8] Notification preferences matrix + in-app feed endpoints with consolidation counts + read/read-all per contracts/reports-notifications.md in InventoryX.Application/.../Notifications/ + InventoryX.Presentation/Controllers/v1/NotificationsController.cs
- [ ] T111 [US8] Daily/weekly digest generation in InventoryX.Infrastructure/BackgroundJobs/DigestWorker.cs

**Checkpoint**: All 8 Cycle 1 stories independently functional

---

## Phase 11: Polish & Cross-Cutting

- [ ] T112 [P] Catalogue/stock export endpoints (GET /export/products etc., FR-056) in InventoryX.Presentation/Controllers/v1/ExportController.cs
- [ ] T113 [P] History-retention enforcement per plan HistoryMonths (Free 3mo/Standard 24mo) + FR-060 recoverability: soft-delete (IsDeleted + recovery window) on catalogue aggregates and a documented backup/point-in-time-restore runbook in InventoryX.Infrastructure/BackgroundJobs/RetentionWorker.cs + docs/operations/backup-restore.md
- [ ] T114 [P] Security hardening: auth rate limiting, account lockout, security headers, webhook replay window in InventoryX.Presentation/Program.cs + middleware
- [ ] T115 Swagger accuracy pass against all files in specs/001-inventory-pos-platform/contracts/ (fix drift — constitution Principle III)
- [ ] T116 [P] Update README.md and CHANGELOG.md for Cycle 1 (constitution Principle III doc rule)
- [ ] T117 Performance validation: sale-creation path p95 < 300 ms with seeded 100k-product tenant (plan Technical Context) — test harness in tests/InventoryX.Presentation.Tests/Performance/
- [ ] T118 Run all four quickstart.md scenarios (A–D) end-to-end and record results in specs/001-inventory-pos-platform/quickstart.md notes
- [ ] T119 Verify Serilog tenant/user/trace enrichment on every request + audit-log coverage of all sensitive commands (FR-008 checklist)

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (P1)** → nothing
- **Foundational (P2)** → Setup. **BLOCKS all stories** (tenancy, behaviors, schema evolution)
- **US1 (P3)** → Foundational only
- **US2 (P4)** → US1 (extends Sale pipeline: T048–T050 touch CreateSaleCommand)
- **US3 (P5)** → US1 (needs StockLedger T038); independent of US2
- **US4 (P6)** → US1 (sale ingest reuses CreateSale core); independent of US2/US3
- **US5 (P7)** → Foundational only (behaviors/subscription); T083 touches counters used by US1 sales — coordinate
- **US6 (P8)** → US1 (Register/Shift minimal from T041); independent of US2–US5
- **US7 (P9)** → US1 (products/ledger) + T060 approval pattern from US3 is reused but not required
- **US8 (P10)** → richest with US1–US7 data present; technically runnable after US1
- **Polish (P11)** → all desired stories

### Key cross-story integration points (watch for file conflicts)

- `CreateSaleCommand` evolves in US1 (T042) → US2 (T048–T050) → US4 ingest reuse (T069) → US7 FEFO (T098): sequence these, don't parallelize
- `PlanEnforcementBehavior`: skeleton T022 (Foundational) → completed T083 (US5)
- `StockLedger`: created T038 (US1) → FEFO extension T098 (US7)

### Parallel opportunities

- Phase 2: T008–T009 (tests) together; T011–T014 (entities) together; T021–T024 together
- After Foundational, with three developers: Dev A → US1→US2, Dev B → US5 (billing is nearly story-independent), Dev C → US3 then US4
- Within every story: all test tasks [P] first in one batch; entity tasks [P] next
- US7: T096, T099, T103 are side-branches parallel to the PO/receipt spine

### Parallel example: User Story 3

```bash
# Batch 1 — failing tests together:
T056 StockTransferTests.cs | T057 AdjustmentApprovalTests.cs | T058 StockCountTests.cs
# Batch 2 — independent implementations:
T059 transfers | T060 adjustments | T061 consumption   # then T062–T065 sequentially
```

---

## Implementation Strategy

**MVP first**: Phases 1–3 only (T001–T044) deliver spec US1's independent test — a
tenant that onboards and sells with stock truth. Stop, run quickstart scenario A,
demo.

**Incremental delivery order** (each checkpoint is releasable): US1 → US2 (usable POS)
→ US6 (cash control) → US3 (multi-location) → US4 (offline) → US5 (monetization) →
US7 (purchasing/batch) → US8 (reports) → Polish. US5 can be pulled earlier if
monetization pressure demands; nothing after US1 depends on it.

**TDD discipline per constitution**: within each phase, test tasks run first and MUST
fail; implementation tasks make them green; `dotnet test InventoryX.sln` green +
migration present is the merge gate for every PR.

---

## Notes

- 119 tasks total: Setup 7 · Foundational 19 · US1 18 · US2 11 · US3 10 · US4 7 ·
  US5 12 · US6 6 · US7 13 · US8 8 · Polish 8
- Every task cites its contract/data-model source; consult
  specs/001-inventory-pos-platform/contracts/ before implementing an endpoint
- Commit per task or logical group; constitution commit-message conventions apply
