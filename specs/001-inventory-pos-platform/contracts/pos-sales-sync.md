# Contract: Registers, Shifts, Sales, Returns & Offline Sync

## Registers & shifts (FR-042)

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| GET | `/registers` | Registers per location (plan-capped) | Sell |
| POST/PATCH | `/registers/{id?}` | Manage registers; 402 over plan limit | Owner/Admin |
| GET/PUT | `/registers/{id}/favourites` | Configurable POS favourites grid layout (pages/categories of product buttons, FR-038); included in sync snapshot | Sell (GET) / Manager (PUT) |
| POST | `/registers/{id}/shifts` | Open shift; body: openingFloat (counted). 409 if a shift is already open on this register | Sell |
| POST | `/shifts/{id}/cash-movements` | Cash in/out with reason (PettyCash\|Banking\|ChangeOrder\|Other) | Sell |
| POST | `/shifts/{id}/close` | Body: closingCounted (required — 400 without it). Computes expected cash, variance; flags manager when \|variance\| > threshold | Sell |
| GET | `/shifts/{id}/z-report` | Z-report: sales, tender breakdown, refunds, discounts, voids, variance for register + staff member | Sell (own) / ViewReports |

## Sales (FR-038/39/40)

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| POST | `/sales` *(idempotent-by-key `clientSaleId`)* | Create Completed or Held sale. Lines: productId/variantId, qty (fractional allowed per UoM), unitPrice override flag, lineDiscount, note. Batch lines auto-FEFO unless batchId given. Payments: array of tenders summing to grandTotal (split allowed) — Cycle 1 tenders: Cash, Card, MobileMoney, BankTransfer, Cheque (StoreCredit/GiftCard/LoyaltyPoints/OnAccount deferred per spec Assumptions); cash → changeDue returned. Server snapshots prices + Ghana tax components per line. 402 over monthly sale cap; 409 shift not open | Sell |
| GET | `/sales` | Paged; filters: date, location, register, cashier, status | Sell (own) / ViewReports |
| GET | `/sales/{id}` | Detail incl. payments, receipt ref | Sell |
| POST | `/sales/{id}/void` | Void (audit-logged; permission-gated) | per role |
| GET | `/sales/held` | Held sales for recall (multi-hold, FR-038) | Sell |
| POST | `/sales/{id}/complete` | Complete a Held sale (stock applies now) | Sell |
| GET | `/products/{id}/availability?locationId=` | Live stock at this + other locations for POS (FR-038); marked live-only for offline clients | Sell |

## Receipts (FR-040)

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| GET | `/sales/{id}/receipt` | Structured receipt payload (fiscal fields, Ghana levy lines) for client rendering | Sell |
| POST | `/sales/{id}/receipt/deliver` | Body: channel Email\|Sms\|Qr + destination; logs delivery | Sell |
| GET/PATCH | `/tenant/receipt-template` | Logo, business details, tax registration, footer, return policy | Owner/Admin |

## Returns & exchanges (FR-041)

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| GET | `/sales/lookup?receiptNumber=` \| `?search=` | Find original sale for return | Sell |
| POST | `/returns` | Body: originalSaleId, lines (saleLineId, qty, disposition ToStock\|Quarantine), refundTender (Original\|Cash in Cycle 1; StoreCredit deferred with its balance ledger), authorizedBy? Above threshold or receiptless → 423 until manager authorization attached. Original price + tax applied automatically | Sell |
| POST | `/returns/exchange` | Return + new sale in one transaction; settles difference only | Sell |

## Offline sync (FR-044/45/46, research R6)

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| GET | `/sync/snapshot?since={watermark}` | Delta of products, variants, prices, tax treatments, favourites, stock levels for the register's location; returns new watermark. Full snapshot when `since` omitted | Sell (register token) |
| POST | `/sync/sales` | Batch upload of queued offline sales, each with `clientSaleId`, `occurredAt`, register/shift refs. Idempotent per sale: replays return prior results. Response per sale: `applied` \| `applied_with_conflict` (StockConflictFlag raised, notification created) \| `rejected` (validation detail). Never silently adjusts stock | Sell (register token) |
| GET | `/sync/conflicts` | Open stock-conflict flags for review | ManageStock |
| POST | `/sync/conflicts/{id}/resolve` | Resolution: acceptAsIs \| adjustWithReason (creates movement) | ApproveAdjustments |
| GET | `/sync/rejected` | Open rejected offline sales for manager review | Owner/Admin/Manager |
| POST | `/sync/rejected/{id}/resolve` | Resolution: retryRelease \| reconcileLinked (+ linked sale id) | Owner/Admin/Manager |

Offline rules: endpoints above are the only ones a register token needs; everything
else (availability at other locations, on-account, online card) is live-only and the
contract marks it so clients can grey it out (FR-045). Register-scoped tokens are
restricted to `/sync/snapshot` and `/sync/sales` for their own `register_id`. Offline
ingest accepts historical `UnitPrice` + `TaxComponentsJson` fiscal evidence.
Snapshots include favourites, receipt template, fractional/tracking metadata,
`bundleVersion`, and product deletion refs.
