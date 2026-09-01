# Contract: Locations, Stock, Transfers, Counts, Alerts

## Locations & stock levels

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| GET | `/locations` | List (plan-capped) | any |
| POST/PATCH | `/locations/{id?}` | Manage; 402 over plan limit | Owner/Admin |
| DELETE | `/locations/{id}` | Soft-delete location (30-day recovery window per data-model); sets `IsActive=false` | Owner/Admin |
| GET | `/stock` | Paged stock levels; filters: locationId, productId, categoryId, `belowReorder`, `expiringWithinDays`; business-wide rollup with `groupBy=product` (FR-022) | ManageStock or Sell (availability only) |
| GET | `/stock/movements` | Paged append-only ledger; filters: product, location, type, date range, user (FR-024) | ManageStock |
| GET | `/products/{id}/batches` | Batches with remaining qty, expiry, FEFO order (FR-021) | ManageStock |
| GET | `/batches/{id}/trace` | Recall trace: supplier/receipt backward, sales forward | ManageStock |

## Movements

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| POST | `/stock/adjustments` | Reasoned adjustment; body: lines, reasonId, note. Above tenant threshold → 202 `AwaitingApproval` (FR-023) | ManageStock |
| POST | `/stock/adjustments/{id}/approve` \| `/reject` | Approver ≠ requester → else 409 | ApproveAdjustments |
| POST | `/stock/consumption` | Internal use write-off | ManageStock |
| GET | `/stock/adjustment-reasons` | Seeded + tenant reasons | any |
| POST | `/stock/movements/{id}/correct` | Append-only correction: original movement preserved, delta applied as new entry (spec US3 scenario 4) | ManageStock |

## Transfers (two-state, FR-023)

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| POST | `/transfers` | Draft with lines | ManageStock |
| POST | `/transfers/{id}/dispatch` | → Dispatched; stock leaves source into InTransit | ManageStock |
| POST | `/transfers/{id}/receive` | Body: per-line QtyReceived; full match → Received, else ReceivedWithDiscrepancy + required reason | ManageStock (at destination) |
| GET | `/transfers` | Paged; filter status=Dispatched → "awaiting receipt" | ManageStock |

## Counts (FR-025)

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| POST | `/counts` | Open count: scope Full\|Cycle\|Spot, location, product/category filter | ManageStock |
| PUT | `/counts/{id}/lines` | Submit counted quantities (scan-driven, incremental) | ManageStock |
| POST | `/counts/{id}/submit` | → AwaitingApproval with variance qty + value per line | ManageStock |
| POST | `/counts/{id}/approve` \| `/reject` | Approve posts CountCorrection movements | ApproveAdjustments |
| GET | `/counts/{id}` | Permanent record: who counted, variances | ManageStock |

## Reorder & alerts (FR-026/27)

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| GET | `/reorder/suggestions` | Items at/below reorder point grouped by supplier, with suggested qty (sales-rate × lead time) | ManagePurchasing |
| POST | `/reorder/suggestions/apply` | Create draft POs from selection | ManagePurchasing |
| GET | `/alerts` | Active alerts: low/out-of-stock, expiry (configurable horizon), overstock, slow-moving | per NotificationPreference |
