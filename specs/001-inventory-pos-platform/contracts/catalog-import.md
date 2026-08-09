# Contract: Catalogue & Import/Export

## Categories & products

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| GET | `/categories` | Tree list | any |
| POST/PATCH/DELETE | `/categories/{id?}` | Manage categories | ManageStock |
| GET | `/products` | Paged list; filters: `search` (name/sku/barcode, typo-tolerant), `categoryId`, `status`, `trackingMode`, `belowReorderPoint` | any |
| GET | `/products/{id}` | Full product incl. variants, stock summary per location | any (cost/margin fields omitted without ViewProfit, FR-050) |
| POST | `/products` | Create (FR-020); body includes trackingMode Simple\|Variant\|Batch; 402 over plan MaxProducts | ManageStock |
| PATCH | `/products/{id}` | Update; price changes audit-logged | ManageStock (price: ManagePricing) |
| POST | `/products/{id}/variants` | Add variant(s) with attribute values, sku, barcode (FR-021) | ManageStock |
| GET | `/products/barcode/{code}` | Fast lookup for POS/scanning | any |
| GET | `/catalogue/shared/lookup?barcode=` | **Deferred post-Cycle 1** — shared-catalogue metadata suggestion (FR-018); Cycle 1 barcode scans resolve via `/products/barcode/{code}` against the tenant's own catalogue | ManageStock |
| GET | `/tax-treatments` | Country tax treatments (Ghana seeded: GH-STD, GH-ZERO, GH-EXEMPT with levy components) | any |

## Spreadsheet import (two-step, FR-018) & export

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| POST | `/import/products` | Upload CSV/XLSX → 201 ImportJob with detected columns | ManageStock |
| PUT | `/import/products/{jobId}/mapping` | Set column mapping → returns full per-row preview: parsed values + row errors; **nothing saved** | ManageStock |
| POST | `/import/products/{jobId}/commit` | Persist valid rows; response: created/updated/skipped counts + row errors | ManageStock |
| DELETE | `/import/products/{jobId}` | Abandon job | ManageStock |
| POST | `/import/opening-stock` (+ same mapping/commit flow) | Opening quantities & costs per location (FR-017 step 4) | ManageStock |
| GET | `/export/products` | Stream CSV/XLSX of catalogue (FR-056) | any |

Validation highlights: sku unique per tenant (row-level error on import, not batch
abort); variant attribute values must match parent's attribute schema; barcode
duplicates flagged as warnings; import commit is transactional per row batch, resumable.
