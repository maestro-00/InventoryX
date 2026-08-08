# InventoryX

InventoryX is a multi-tenant inventory management and point-of-sale platform for
Ghanaian retailers. Cycle 1 delivers tenant onboarding, catalogue management, stock
control, POS checkout, offline sale synchronization, subscription billing, cash
shifts, purchasing, batch/expiry tracking, dashboards, reports, notifications, and
CSV/XLSX/PDF exports.

## Architecture

- `InventoryX.Domain` — tenant-owned entities and domain state machines.
- `InventoryX.Application` — CQRS requests, validation, authorization, audit, and service contracts.
- `InventoryX.Infrastructure` — EF Core/SQL Server, Paystack, SendGrid, exports, and background workers.
- `InventoryX.Presentation` — versioned `/api/v1` controllers, middleware, Swagger, and health checks.
- `tests/` — common fixtures plus application, infrastructure, and presentation tests.

## Cycle 1 Highlights

- Tenant isolation, fixed roles, invitation/PIN access, audit logs, and plan limits.
- Products, variants, categories, locations, ledger-backed stock, transfers, counts, and adjustments.
- POS checkout, split tenders, discounts, holds, returns, receipts, shifts, and Z reports.
- Idempotent offline sync with stock-conflict review.
- Paystack subscriptions, invoices, grace/read-only states, and tenant export.
- Suppliers, purchase orders, receipts, landed costs, FEFO batches, expiry alerts, and reorder suggestions.
- Dashboard, Ghana tax reporting, scheduled reports, notification preferences, and digest delivery.

## Requirements

- .NET 8 SDK
- SQL Server
- `dotnet-ef` 9.x for migration commands

## Configuration

Use environment variables or user secrets for production credentials. Important
sections in `InventoryX.Presentation/appsettings.json` are:

- `ConnectionStrings:DefaultConnection`
- `Jwt:SigningKey`, `Jwt:Issuer`, and `Jwt:Audience`
- `Paystack:SecretKey`
- `SendGrid:ApiKey`
- `Frontend:AllowedOrigins`

Never commit real credentials.

## Run Locally

```bash
dotnet restore InventoryX.sln
dotnet ef database update \
  --project InventoryX.Infrastructure/InventoryX.Infrastructure.csproj \
  --startup-project InventoryX.Presentation/InventoryX.Presentation.csproj
dotnet run --project InventoryX.Presentation/InventoryX.Presentation.csproj
```

Swagger is available at `/swagger`; liveness and readiness probes are available at
`/health/live` and `/health/ready`.

## Validate

```bash
dotnet build InventoryX.sln --configuration Release
dotnet test InventoryX.sln --configuration Release
```

The API contract source of truth is
`specs/001-inventory-pos-platform/contracts/`. Operational backup and point-in-time
restore guidance is in `docs/operations/backup-restore.md`.

## API Conventions

- Base path: `/api/v1`
- Authentication: bearer JWT with `tenant_id`, role, and location scope claims
- Errors: RFC 7807 problem details
- Pagination: `page` and `pageSize` (maximum 200)
- Concurrency: ETag/`If-Match` for mutable aggregates
- Money and quantities: decimal values; timestamps are UTC

## License

See `LICENSE.md`.
