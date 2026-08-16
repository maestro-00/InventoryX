# Contract: Auth, Tenants, Users & Roles

## Sign-up & session

| Method | Path | Purpose | Notes |
|--------|------|---------|-------|
| POST | `/auth/register` `[anon]` | Create tenant + owner (FR-001) | Body: email, password, businessName, country, currency, businessType. Creates Trialing subscription (Professional, 14d). 201 → tenant summary + tokens. Sets `inventoryx_refresh` (HttpOnly; Secure; SameSite=Lax; Path=/api/v1/auth) and readable `inventoryx_session=1` (same lifetime) |
| POST | `/auth/login` `[anon]` | Email/password login | 200 → `{ accessToken, refreshToken, expiresIn }`; 401 on bad creds; 423 if 2FA required → complete via `/auth/2fa/verify`. Sets the same session cookies as register when tokens are issued |
| GET/POST | `/auth/google` `[anon]` | Google OAuth challenge | Callback sets session cookies and redirects to `returnUrl` with `accessToken`, `refreshToken`, `accessTokenExpiresAt` query params |
| POST | `/auth/refresh` `[anon]` | Rotate tokens | Body `{ refreshToken }` optional when the httpOnly refresh cookie is present. Rotates cookies on success; clears both cookies on 400/401/403. Cold loads without `inventoryx_session` stay anonymous by design (SPA skips this call) |
| POST | `/auth/logout` `[anon]` | End SPA session cookies | Clears `inventoryx_refresh` and `inventoryx_session` |
| POST | `/auth/2fa/enroll` / `verify` | TOTP setup & challenge (FR-006) | |
| POST | `/auth/pin/exchange` | Register PIN → register-scoped token (FR-007) | Requires device token; body: userId, pin, registerId. 200 → short-lived scoped JWT |

## Tenant & onboarding

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| GET | `/tenant` | Tenant profile + onboarding checklist state (FR-017) | any |
| PATCH | `/tenant` | Update profile, thresholds (adjustment/PO approval, refund, variance), valuation method (confirmation flag required, FR-028) | Owner/Admin |
| POST | `/tenant/sample-data` | Load sample data (FR-019) | Owner/Admin |
| DELETE | `/tenant/sample-data` | Remove all sample data in one action | Owner/Admin |
| POST | `/tenant/export` | Full data export job (FR-015); works in ReadOnly state | Owner/Admin |
| GET | `/tenant/export/{jobId}` | Export status/download link | Owner/Admin |

## Users & roles

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| GET | `/users` | List users (paged) | ManageUsers |
| POST | `/users/invitations` | Invite by email with role + location scope (FR-004) | ManageUsers |
| POST | `/users/invitations/{id}/accept` `[anon+inviteToken]` | Accept, set password | — |
| PATCH | `/users/{id}` | Change role, locations, deactivate | ManageUsers; 409 if target is Owner (FR-003) |
| PUT | `/users/{id}/pin` | Set/replace register PIN | Self or ManageUsers |
| GET | `/roles` | List roles + permission atoms (fixed set in Cycle 1) | any |
| GET | `/audit-log` | Paged sensitive-action log (FR-008) | Owner/Admin |

State/validation highlights: exactly one Owner; deactivating a user with an open shift
→ 409; invitation counts toward plan MaxUsers at creation (402 when exceeded).
