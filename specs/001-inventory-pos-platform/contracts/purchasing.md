# Contract: Suppliers & Purchase Orders

## Suppliers (FR-029)

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| GET | `/suppliers` | Paged list with performance summary (on-time rate, achieved lead time) | ManagePurchasing |
| POST/PATCH | `/suppliers/{id?}` | Manage supplier, terms, lead time, currency | ManagePurchasing |
| GET | `/suppliers/{id}/products` | Supplied products with supplier codes & price history | ManagePurchasing |
| PUT | `/suppliers/{id}/products` | Link products + supplier codes/prices | ManagePurchasing |
| GET | `/suppliers/{id}/orders` | Order history | ManagePurchasing |

## Purchase orders (FR-030/31) — state machine

`Draft → AwaitingApproval → Sent → PartiallyReceived → FullyReceived → Closed`
(`Cancelled` allowed before Closed; illegal transition → 409)

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| POST | `/purchase-orders` | Create Draft (manual, from reorder suggestion, or from alert); 402 if plan lacks PurchaseOrders flag | ManagePurchasing |
| PATCH | `/purchase-orders/{id}` | Edit while Draft | ManagePurchasing |
| POST | `/purchase-orders/{id}/submit` | → Sent, or → AwaitingApproval when total ≥ tenant threshold (423 hint) | ManagePurchasing |
| POST | `/purchase-orders/{id}/approve` \| `/reject` | Approval gate | ApproveAdjustments |
| POST | `/purchase-orders/{id}/send` | Email PDF to supplier or return download link | ManagePurchasing |
| POST | `/purchase-orders/{id}/cancel` | Any state before Closed | ManagePurchasing |
| POST | `/purchase-orders/{id}/receipts` | Record goods receipt: per-line qty received/damaged, batch number + expiry for batch-tracked lines (creates Batch, FEFO pool), unit costs. Updates PO state Partially/FullyReceived | ManageStock |
| POST | `/purchase-orders/{id}/close-short` | Close with outstanding balance + required reason | ManagePurchasing |
| GET | `/purchase-orders` | Paged; filters: status, supplier, overdue | ManagePurchasing |

## Supplier invoices & landed costs (FR-032)

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| POST | `/supplier-invoices` | Record against PO; response flags line price variances vs ordered | ManagePurchasing |
| POST | `/goods-receipts/{id}/landed-costs` | Allocate freight/duty/clearing/insurance across lines by value; recalculates item true cost & valuation | ManagePurchasing |
