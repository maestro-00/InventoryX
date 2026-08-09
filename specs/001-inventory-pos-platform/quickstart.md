# Quickstart: Validate Cycle 1 End-to-End

**Plan**: [plan.md](./plan.md) | **Contracts**: [contracts/](./contracts/) |
**Data model**: [data-model.md](./data-model.md)

## Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB, or container):
  `docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<YourStrong!Pass>' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest`
- Environment (never committed — constitution Principle V): `ConnectionStrings__Default`,
  `Jwt__Key`, `Paystack__SecretKey` (test key), `SendGrid__ApiKey` (optional locally)

## Build & test (the constitution gate)

```bash
dotnet restore InventoryX.sln
dotnet build InventoryX.sln --no-restore
dotnet test InventoryX.sln --no-build          # all green or nothing merges
```

## Run

```bash
dotnet ef database update --project InventoryX.Infrastructure --startup-project InventoryX.Presentation
dotnet run --project InventoryX.Presentation   # Swagger at https://localhost:<port>/swagger
```

Health probes: `GET /health/live`, `GET /health/ready`.

## Validation scenario A — the P1 core loop (spec User Story 1)

1. `POST /api/v1/auth/register` — new tenant (country GH, currency GHS, type Retail).
   Expect 201, Trialing subscription, JWT returned.
2. `POST /api/v1/locations` — "Main Shop".
3. `POST /api/v1/products` — simple product, price 10.00, GH-STD tax.
4. Opening stock via `/api/v1/import/opening-stock` flow (or an adjustment with reason
   Correction): qty 10 @ cost 6.00.
5. `POST /api/v1/registers/{id}/shifts` — open with float 100.00.
6. `POST /api/v1/sales` — qty 2, cash 25.00. Expect grandTotal 24.38 and changeDue
   0.62: base 20.00 + levies 6% (NHIL 2.5 + GETFund 2.5 + COVID 1) = 21.20, then VAT
   15% on the levy-inclusive amount = 24.38 per [research R11](./research.md); receipt
   payload lists each levy line.
7. `GET /api/v1/stock?productId=...` — **QtyOnHand must read 8**.
8. `POST /api/v1/shifts/{id}/close` with counted cash — Z-report shows the sale and
   zero variance.

## Validation scenario B — offline sync honesty (User Story 4)

1. `GET /api/v1/sync/snapshot` — capture watermark.
2. `POST /api/v1/sync/sales` with two sales sharing one `clientSaleId` (replay) —
   expect one applied, replay returns the same result, no duplicate.
3. Upload an offline sale that oversells remaining stock — expect
   `applied_with_conflict`, `GET /api/v1/sync/conflicts` lists it, stock NOT silently
   corrected.

## Validation scenario C — purchasing + batch/expiry (User Story 7 + batch scope)

1. Create supplier, then PO above the approval threshold — `submit` must return the
   AwaitingApproval state, approve with a second user.
2. `POST /purchase-orders/{id}/receipts` for a batch-tracked product with two batches,
   nearer expiry second — PO goes PartiallyReceived/FullyReceived.
3. Sell the product — the sale line must consume the **earlier-expiry** batch (FEFO).
4. `GET /api/v1/batches/{id}/trace` — shows supplier backward and the sale forward.

## Validation scenario D — plan limits & read-only

1. On a Free-plan tenant, complete 300 sales in a month (seed script) — sale 301
   returns 402 with `upgradeHint`.
2. Force subscription to ReadOnly (test hook / clock shift) — any POST returns 402
   `subscription_read_only`; `POST /tenant/export` still succeeds.

## Expected outcomes summary

Every scenario maps to spec acceptance criteria: A → US1/US2/US6, B → US4 (FR-044..46),
C → US7 + FR-021 batch clauses, D → FR-010/013. All four passing plus a green
`dotnet test` is the Cycle 1 definition of working software.

## Validation notes

Validated on 2026-08-09 with .NET 8 using the real application handlers and SQLite
integration database. Scenario A additionally exercised the HTTP API through
`WebApplicationFactory`.

| Scenario | Executable coverage | Result |
|----------|---------------------|--------|
| A | `FirstSaleScenarioTests` | PASS (1/1) |
| B | `SyncSnapshotTests`, `OfflineSaleIngestTests`, `StockConflictTests`, `ConflictReviewTests` | PASS |
| C | `PurchaseOrderStateTests`, `GoodsReceiptTests`, `FefoIssueTests`, `BatchTraceTests` | PASS |
| D | `PlanEnforcementTests`, `SubscriptionStateMachineTests` | PASS |

The targeted B-D integration run completed with 38/38 tests passing. Together with
the Scenario A HTTP test, the run verified idempotent offline replay and conflict
review, approval-gated purchasing and FEFO batch traceability, the 301st-sale plan
limit, and ReadOnly export/billing exemptions.
