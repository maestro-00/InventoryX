# Contract: Subscription & Billing

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| GET | `/billing/plans` `[anon]` | Plan matrix with limits, prices, feature flags (FR-009) | — |
| GET | `/billing/subscription` | Current subscription: plan, status, period, trial/grace deadlines, usage vs limits | Owner |
| POST | `/billing/subscription/upgrade` | Immediate upgrade, pro-rata charge via Paystack (FR-012) | Owner |
| POST | `/billing/subscription/downgrade` | Schedule downgrade at period end; response lists every limit the tenant currently exceeds (FR-012); 409 until acknowledged flag sent | Owner |
| POST | `/billing/subscription/cancel` | Cancel at period end; starts 90-day retention clock on expiry (FR-014) | Owner |
| POST | `/billing/subscription/reactivate` | Within 90 days of cancellation | Owner |
| POST | `/billing/payment-method` | Init Paystack authorization (card or MoMo); body: channel (`card`\|`mobile_money`), for MoMo: provider (`mtn`\|`telecel`\|`at`), msisdn | Owner |
| GET | `/billing/invoices` | Paged invoice history (FR-016) | Owner |
| GET | `/billing/invoices/{id}/pdf` | Download invoice | Owner |
| PATCH | `/billing/contact` | Billing contact, tax/VAT number | Owner |
| POST | `/billing/webhooks/paystack` `[anon+signature]` | Paystack events: charge.success, charge.failed, transfer events. Signature-verified; idempotent by event id | — |

## Subscription state semantics (research R7, FR-011..016)

- `Trialing` (14d, Professional features): expiry without payment → Free plan, status
  `Active`; over-limit features become read-only, data intact.
- Failed renewal → `PastDue`: retries + owner notification daily up to 7 days.
- Grace exhausted → `ReadOnly`: every mutating endpoint returns `402` problem
  (`reason: "subscription_read_only"`); GET + `/tenant/export` + billing endpoints stay
  available.
- `Cancelled` + 90 days → purge after final warning email.
- Plan limit checks return `402` with `{ "limit", "current", "upgradeHint" }` (FR-010);
  monthly sale caps: Free 300 / Standard 3,000.
