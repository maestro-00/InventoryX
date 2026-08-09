# API Contracts — Cycle 1

These documents are the source of truth for the `/api/v1` surface per constitution
Principle III. Swagger/OpenAPI generated from code MUST match them; drift is a review
blocker. Files: [auth-tenancy.md](./auth-tenancy.md), [billing.md](./billing.md),
[catalog-import.md](./catalog-import.md), [inventory.md](./inventory.md),
[purchasing.md](./purchasing.md), [pos-sales-sync.md](./pos-sales-sync.md),
[reports-notifications.md](./reports-notifications.md).

## Global conventions

- **Base path**: `/api/v1`. Breaking changes require `/api/v2`; additive changes only
  within v1.
- **Auth**: `Authorization: Bearer <JWT>` on every endpoint unless marked `[anon]`.
  JWT claims: `sub`, `tenant_id`, `role`, `location_scope[]`. Register-scoped tokens
  (from PIN exchange) carry `register_id` and reduced scope.
- **Tenancy**: TenantId is NEVER a request parameter — always resolved from the token.
- **Errors**: RFC 7807 `application/problem+json`:
  `{ "type", "title", "status", "detail", "traceId", "errors": { field: [msgs] } }`.
  Notable codes: `402` plan limit / read-only subscription (body includes
  `upgradeHint`), `409` optimistic-concurrency or state-machine violation, `423`
  approval required.
- **Pagination**: `?page=1&pageSize=50` (max 200) → envelope
  `{ "items": [], "page", "pageSize", "totalCount" }`.
- **Concurrency**: mutable aggregates return `ETag` (rowversion); mutations send
  `If-Match`; mismatch → `409`.
- **Idempotency**: endpoints marked *idempotent-by-key* dedupe on a client-supplied
  UUID; replays return the original result with `200` instead of `201`.
- **Money/quantities**: decimal strings in tenant currency; quantities up to 3 dp.
- **Timestamps**: UTC ISO 8601; `OccurredAt` for business time vs `CreatedAt` server
  time (offline sales differ).
- **Role gates**: each file lists the minimum permission atom per endpoint (see
  data-model Role); `403` on missing permission.
