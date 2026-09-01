# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `GET /api/v1/shifts` and `GET /api/v1/registers/{id}/shifts` so a POS can list and resume an open shift.
- `GET /api/v1/transfers` (paged), `PATCH /api/v1/registers/{id}`, and `PATCH /api/v1/suppliers/{id}` to match Cycle 1 contracts.
- Paged list envelopes for `GET /users`, `GET /suppliers`, and `GET /billing/invoices`.
- OpenAPI hardening: `InventoryX API` info block, HTTP Bearer security scheme, controller tags, XML comments.
- Public request DTOs for sale/product/location/PO/auth create flows (offline-only sale flags excluded from Swagger).
- ETag/`If-Match` on products, locations, tenant, purchase orders (draft), registers, and suppliers; users use Identity `ConcurrencyStamp`.
- Typed list DTOs for users, roles, product batches, and alerts.
- Cashiers resume only their own open shift and see only their own sales; managers (Sell + ViewReports) can see and continue others’ still-open shifts. Takeover is implicit (`Sale.CashierId` is the acting user).
- Cycle 1 multi-tenant inventory and POS API under `/api/v1`.
- Tenant onboarding, fixed roles, user invitations, register PINs, audit logging, and plan enforcement.
- Catalogue, variants, spreadsheet import, stock ledger, transfers, counts, adjustments, and exports.
- Checkout, split payments, holds, returns, receipts, shifts, cash movements, and offline synchronization.
- Paystack subscription billing, invoices, grace/read-only transitions, and tenant data export.
- Purchasing, supplier catalogue links, goods receipts, landed costs, FEFO batches, and stock alerts.
- Dashboard, standard/Ghana tax reports, CSV/XLSX/PDF export, schedules, and notification digests.
- CI workflow, health checks, structured Serilog output, RFC 7807 middleware, and security rate limits.
- Render deployment: `Dockerfile`, `render.yaml`, `docs/deploy/render.md`, and `DEMO_MODE` demo seeder (`demo@inventoryx.dev`).
- Npgsql health check on `/health/ready`; `/health/live` remains a lightweight liveness probe.

### Changed
- Database provider switched from SQL Server to PostgreSQL (Npgsql) for portfolio/demo deployments on Render + Supabase.
- Squashed EF migrations into `InitialPostgres`; local dev and CI tests continue using Sqlite in-memory.
- Cycle 1 contract docs aligned with implemented API: documented `DELETE /locations/{id}`, `POST /stock/movements/{id}/correct`, `GET /export/stock`, `GET /suppliers/{id}/performance`, `GET /purchase-orders/{id}/pdf`, `GET /sales/held/{id}`, report export routes, and corrected `/tenant/receipt-template` to `GET/PUT`.
- Evolved the legacy inventory-item/retail-stock model into product and ledger-backed stock aggregates.
- Standardized API routes, pagination, authorization, and Swagger against the Cycle 1 contracts.
- Swagger now documents `/api/v1` only; legacy Identity helpers under `/api/auth/*` remain for cookie/OAuth but are excluded from OpenAPI.

### Deprecated

### Removed

### Fixed

### Security
- Enforced tenant query/write isolation, account lockout, auth/webhook rate limits, security headers, and webhook replay protection.

## [1.0.0] - 2024-09-14

### Added
- Initial release of InventoryX
- Core domain entities (InventoryItem, ItemType, Purchase, Sale, RetailStock)
- Application layer with CQRS commands and queries
- Infrastructure layer with Entity Framework Core
- Presentation layer with ASP.NET Core Web API
- Database migrations for SQL Server
- API documentation with Swagger/OpenAPI
- Authentication and authorization system
- Basic CRUD operations for all entities
- Project documentation (README, LICENSE, CONTRIBUTING)

---

## How to Update This Changelog

When making changes to the project:

1. Add your changes under the `[Unreleased]` section
2. Use the following categories:
   - **Added** for new features
   - **Changed** for changes in existing functionality
   - **Deprecated** for soon-to-be removed features
   - **Removed** for now removed features
   - **Fixed** for any bug fixes
   - **Security** for vulnerability fixes

3. When releasing a new version:
   - Change `[Unreleased]` to the version number and date
   - Create a new `[Unreleased]` section at the top
   - Update the version comparison links at the bottom

### Example Entry Format

```markdown
### Added
- New bulk import feature for inventory items (#123)
- Export to CSV functionality (#145)

### Fixed
- Fixed authentication token expiration issue (#167)
- Resolved database connection timeout in high-load scenarios (#178)
```
