# Data Model: Inventory + POS Platform — Cycle 1

**Date**: 2026-07-26 | **Plan**: [plan.md](./plan.md) | **Research**: [research.md](./research.md)

Conventions: every tenant-owned entity inherits `BaseModel` (Id GUID, TenantId,
CreatedAt/By, UpdatedAt/By, `rowversion` concurrency token) and is covered by the EF
global tenant query filter (research R2). Monetary values are `decimal(18,4)` in tenant
currency (GHS default). Quantities are `decimal(18,3)` to support fractional units.
`StockMovement` and `AuditLogEntry` are append-only (no update/delete). Catalogue
aggregates (Product, Category, Location) use soft delete (`IsDeleted` + recovery
window) so accidental deletions are recoverable (FR-060).

## Tenancy & Commercial

### Tenant
Business account (the isolation root — not itself tenant-filtered).
- Name, Country (ISO 3166), Currency (ISO 4217, default GHS), BusinessType
  (Retail|Food|Pharmacy|Wholesale|Service|Other), ValuationMethod (WeightedAverage in
  Cycle 1; FIFO and Specific reserved enum values — deferred per spec Assumptions),
  OnboardingChecklist (JSON step flags), SampleDataLoaded (bool)
- Business-type defaults applied at registration (FR-001): Retail → Simple tracking,
  GH-STD tax; Food/Pharmacy → Batch tracking default on, expiry required at receipt,
  expiry alerts at 90/30/7 days; Wholesale → Simple tracking + AllowFractional default
  on, box/kg UoM favoured; Service/Other → Simple tracking, minimal alerts. All
  defaults are per-product overridable
- 1→N: Users, Locations, Products, Subscriptions

### PlanDefinition (global, not tenant-owned)
- Tier (Free|Standard|Professional|Enterprise), MonthlyPrice, AnnualPrice (≈17% off)
- Limits: MaxLocations, MaxUsers, MaxProducts, MaxRegisters, MonthlySaleCap
  (Free=300, Standard=3000, null=unlimited), HistoryMonths (3|24|null)
- FeatureFlags: PurchaseOrders, BatchExpiry, Serials, MultiCurrency, CustomRoles,
  AdvancedReports, Integrations
- Seeded from configuration; changes versioned

### Subscription
- Tenant →1, PlanDefinition →1, BillingCycle (Monthly|Annual)
- Status: **Trialing → Active → PastDue → ReadOnly → Cancelled → PurgePending**
  - Trialing: 14 days, no card (FR-011); expiry w/o payment → downgrade to Free + Active
  - Active → PastDue on failed charge; retries ≤7 days with notifications (FR-013)
  - PastDue → Active on successful retry; → ReadOnly after grace
  - Cancelled: retains data 90 days (FR-014) → PurgePending → hard delete after final warning
- TrialEndsAt, CurrentPeriodStart/End, GraceExpiresAt, CancelledAt, PurgeAt
- 1→N: BillingInvoice (number, amount, tax, status, PDF pointer, emailed-to)
- PaymentMethodRef (Paystack authorization code — never raw card data, FR-059)

### UsageCounter
- Tenant →1, Metric (SalesThisMonth|Products|Users|Locations|Registers), PeriodKey
  (e.g. `2026-07`), Count — maintained transactionally; consulted by PlanEnforcer (FR-010)

## Identity & Access

### User (extends ASP.NET Identity)
- TenantId, DisplayName, Role →1, LocationScope (N↔N UserLocation for Managers, FR-004)
- TwoFactorEnabled (Identity TOTP), IsOwner (exactly one irremovable per tenant, FR-003)
- Status: Invited → Active → Deactivated

### Role
- Cycle 1: fixed set — Owner, Administrator, Manager, Cashier, StockClerk, ReadOnly —
  as seeded permission bundles. PermissionSet stored as flags so custom roles (Cycle 3)
  add rows, not schema. Key permission atoms: Sell, Refund (MaxUnauthorizedAmount),
  Discount (MaxPercent per FR-035), VoidSale, ViewProfit, ManageStock, ManagePurchasing,
  ManagePricing, ManageUsers, ViewReports, ApproveAdjustments

### RegisterPin
- User →1, PinHash (Identity hasher), per-register enablement; exchanges for
  register-scoped short-lived JWT (research R3, FR-007)

### AuditLogEntry (append-only)
- Actor, Action, EntityType/Id, Before/After (JSON), Timestamp, Ip — written by
  AuditBehavior for sensitive commands (FR-008)

## Catalogue

### Category
(was `InventoryItemType`) — Name, Parent →0..1 (tree), unique (TenantId, Name, Parent)

### Product
(was `InventoryItem`)
- Name, Description, Sku (unique per tenant), Barcode, Category →0..1, Tags (JSON array)
- UnitOfMeasure (Each|Box|Kg|g|Litre|ml|Metre|Hour — extensible lookup), AllowFractional
- CostPrice (maintained by valuation method), SellingPrice, TaxTreatment →1
- ReorderPoint?, ReorderQuantity?, LeadTimeDays?
- Photos (JSON array of blob refs), CustomFields (JSON per-tenant schema)
- Status: Active | Inactive | Discontinued
- **TrackingMode**: Simple | Variant | Batch  *(Serial, Bundle, Recipe, Asset,
  Consignment, NonStock reserved enum values — Cycle 2/3, keeps migrations additive)*
- If Variant: 1→N ProductVariant; attributes schema (e.g. Size, Colour) on Product

### ProductVariant
- Product →1, AttributeValues (JSON, e.g. `{"Size":"M","Colour":"Red"}`), own Sku,
  Barcode, SellingPrice?, CostPrice? (null = inherit) — stock is held at variant level
  when present (FR-021)

### TaxTreatment (per-country config, seeded for Ghana per research R11)
- Code (GH-STD|GH-ZERO|GH-EXEMPT), Components (JSON: VAT 15%, NHIL 2.5%, GETFund 2.5%,
  COVID HRL 1% with compounding rules)

## Inventory

### Location
- Name, Address, Kind (Shop|Warehouse|Both|Vehicle|Stall), IsActive

### StockLevel (projection — research R5)
- Product →1, Variant →0..1, Location →1, Batch →0..1
- QtyOnHand, QtyInTransit, QtyQuarantine, AvgUnitCost
- Unique (Product, Variant, Location, Batch)

### Batch
- Product →1, BatchNumber, ManufactureDate?, ExpiryDate?, ReceivedVia (GoodsReceipt →1),
  Supplier →1 (recall trace backward, FR-021); forward trace via SaleLine.BatchId
- FEFO: issue order = earliest ExpiryDate first

### StockMovement (append-only ledger — research R5)
- Type: Receipt | Sale | ReturnIn | ReturnToSupplier | TransferOut | TransferIn |
  Adjustment | Consumption | CountCorrection
- Product/Variant/Batch, Location, QtyDelta (signed), UnitCost, User, Reason →0..1,
  CorrelationId (links the two legs of a transfer, sale lines, etc.), OccurredAt
- Corrections = new compensating entries; originals immutable (FR-024)

### AdjustmentReason
- Seeded: Damage, Theft, Spoilage, Expiry, Sample, PersonalUse, Correction; tenant may
  extend (FR-023). ApprovalThresholdValue on tenant settings — adjustments above it
  enter PendingApproval

### StockTransfer
- FromLocation, ToLocation, Lines (Product/Variant/Batch, QtyDispatched, QtyReceived?)
- Status: **Draft → Dispatched → Received | ReceivedWithDiscrepancy → Closed**
  (Cancelled from Draft) — in-transit qty visible on StockLevel.QtyInTransit (FR-023)

### StockCount
- Scope: Full | Cycle | Spot; Location →1; Lines (Product/Variant/Batch, ExpectedQty,
  CountedQty, VarianceValue); CountedBy, ApprovedBy
- Status: **Open → Counting → AwaitingApproval → Approved(applied) | Rejected**
- On approve: CountCorrection movements post; permanent record retained (FR-025)

## Purchasing

### Supplier
- Name, Contacts (JSON), Addresses, PaymentTerms, LeadTimeDays, Currency
- SupplierProducts: N↔N with Product (SupplierCode, LastPrice)
- Derived performance: OnTimeRate, AvgLeadTime, price history (from receipts, FR-029)

### PurchaseOrder
- Supplier →1, DeliverTo Location →1, Lines (Product/Variant, QtyOrdered, UnitPrice,
  QtyReceived, QtyDamaged), ExpectedDate, Notes
- Status: **Draft → AwaitingApproval → Sent → PartiallyReceived → FullyReceived →
  Closed**; Cancelled allowed pre-Closed; ClosedShortReason? (FR-030/31)
- ApprovalThresholdValue on tenant settings gates Draft→Sent
- Origin: Manual | ReorderSuggestion | LowStockAlert

### GoodsReceipt
- PurchaseOrder →0..1, Supplier →1, Location →1, Lines (Product/Variant, Qty, UnitCost,
  Batch? created here with expiry), ReceivedBy/At — posts Receipt movements
- LandedCostAllocations (Freight|Duty|Clearing|Insurance amounts spread across lines by
  value, FR-032)

### SupplierInvoice
- Supplier →1, PurchaseOrder →0..1, InvoiceNumber, Lines, PriceVarianceFlag (FR-032)

## Selling & POS

### Register
- Location →1, Name, IsActive; plan-capped count
- 1→1 FavouritesLayout: customer-configurable POS grid (FR-038) — pages/categories of
  product buttons as JSON layout, included in the offline sync snapshot

### Shift
- Register →1, OpenedBy/At, OpeningFloat, ClosedBy/At?, ClosingCounted?, ExpectedCash
  (derived), Variance (derived), Status: **Open → Closed** (close requires counted
  drawer, FR-042); VarianceFlagged when |Variance| > tenant threshold
- 1→N CashMovement (In|Out, Amount, Reason: PettyCash|Banking|ChangeOrder|Other)
- ZReport: materialized summary (sales, tenders, refunds, discounts, voids, variance)

### Sale
(was `SaleGroup`; old `Sale` becomes SaleLine)
- Location →1, Register →1, Shift →1, Cashier →1, **ClientSaleId** (client UUID,
  idempotency key — unique per tenant, research R6), Channel (Pos|Api), OfflineOrigin
  (bool), StockConflictFlag (bool, FR-046)
- Lines: Product/Variant →1, Batch →0..1 (FEFO-assigned), Qty, UnitPrice,
  LineDiscount (+ authorizing user if above role cap), TaxComponents (JSON snapshot),
  Note
- Totals: Subtotal, DiscountTotal, TaxTotal, GrandTotal (all persisted snapshots)
- Status: **Held → Completed → PartiallyReturned | Returned; Voided** (Held sales don't
  touch stock, FR-038)
- 1→N SalePayment: Tender (Cash|Card|MobileMoney|BankTransfer|Cheque — recorded
  tenders in Cycle 1, gateway-processed later; StoreCredit, GiftCard, LoyaltyPoints,
  OnAccount reserved enum values deferred with their balance ledgers per spec
  Assumptions), Amount, Ref; split-tender = multiple rows (FR-039); ChangeGiven for cash

### ReturnTransaction
- OriginalSale →1 (located by receipt/search), Lines (SaleLine →1, Qty, Disposition:
  ToStock|Quarantine), RefundTender, Amount (original price+tax, FR-041)
- AuthorizedBy? (required above threshold or receiptless), ExchangeSale →0..1 (return +
  new sale settling difference)

### Receipt
- Sale →1, Number (per-tenant sequential, gap-tracked), Payload (structured JSON with
  fiscal fields per research R11), DeliveredVia (Print|Email|Sms|Qr) log

## Sync, Import, Alerts

### SyncSnapshot (logical, not stored per-tenant beyond watermark)
- Delta feed of catalogue/price/stock by `rowversion` watermark (research R6)

### ImportJob
- Kind (Products|OpeningStock), FileRef, ColumnMapping (JSON), RowResults (per-row
  parsed/error state), Status: **Uploaded → Previewed → Committed | Abandoned** —
  nothing persists to catalogue until commit (FR-018)

### NotificationPreference / Notification
- Preference: User →1, Type (LowStock|OutOfStock|Expiry|PoReceived|PoOverdue|
  TransferAwaiting|LargeDiscount|LargeRefund|TillVariance|UnusualVoids|NegativeStock|
  BillingFailure|Digest), Channel (InApp|Email|Push|Sms), Threshold config
- Notification: instance with ConsolidationKey — repeats for same unresolved issue
  merge (FR-052/53); digests batched by background job

## Deferred entities (reserved, not built in Cycle 1)

Customer/CreditAccount, PriceTier/Promotion, Quote/SalesOrder/BackOrder, SerialUnit,
BundleComponent/Recipe/ProductionRun, Asset, ConsignmentAgreement, LoyaltyAccount,
IntegrationConnection. `Sale.CustomerId` (nullable) and `Product.TrackingMode` reserved
values are the extension points; no Cycle 1 migration touches them.

## Key validation rules (enforced by FluentValidation + domain)

- Sku unique per tenant; Barcode unique per tenant when present
- Sale must reference an Open shift; Completed sale immutable except via Return/Void
- Void requires permission + audit entry; discount ≤ role MaxPercent else authorization
- Adjustment/PO above threshold → approval state, approver ≠ requester
- Batch-tracked product movements must carry BatchId; expiry required on batch create
  for pharmacy/food business types (default on, configurable)
- Transfer receive quantities ≤ dispatched; discrepancy requires reason
- Shift close requires ClosingCounted; register cannot open two concurrent shifts
- Plan enforcement: creating entity or completing sale beyond plan limit → 402-style
  problem response with upgrade hint (FR-010); ReadOnly subscription blocks all writes
  except export/billing
