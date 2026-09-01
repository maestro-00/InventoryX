# Contract: Dashboard, Reports & Notifications

## Dashboard (FR-048)

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| GET | `/dashboard` | Today vs same day last week: sales total, transaction count, avg basket, cash in drawer, items sold, low-stock + expiry warning counts, top sellers. Every figure carries a `detailUrl` | ViewReports (profit fields need ViewProfit, FR-050) |

## Standard reports (FR-049) — Cycle 1 "essential" set

On-screen JSON reports accept `?from&to&locationId&categoryId&staffId`. File export uses
dedicated routes: `GET /reports/{reportType}/export?format=json|csv|xlsx|pdf&from&to&...`
returns the file inline for ranges ≤ 31 days, or `202 Accepted` + `{ jobId }` for longer
ranges; poll with `GET /reports/export-jobs/{id}` (`200` file or `202 Pending`). All
report types are schedulable by email.

| Method | Path | Report |
|--------|------|--------|
| GET | `/reports/sales` | By day/week/month, location, staff, product/category, hour-of-day, payment method |
| GET | `/reports/profit` | Gross margin by product/category/location/period; discount cost (ViewProfit required) |
| GET | `/reports/stock` | On-hand + value (valuation method applied), movement history, ageing, expiry schedule, dead/slow stock, count variance, shrinkage |
| GET | `/reports/purchasing` | Orders outstanding, supplier performance, price changes |
| GET | `/reports/staff` | Sales/discounts/refunds/voids per staff, till variances |
| GET | `/reports/tax` | Ghana: VAT + NHIL/GETFund/COVID levy collected by rate & period, GRA-aligned format |
| GET | `/reports/{reportType}/export` | Export report to file (ViewReports); short ranges stream inline, long ranges async |
| GET | `/reports/export-jobs/{id}` | Poll/download async export job (ViewReports) |
| POST | `/reports/schedules` | Schedule any above: cadence Daily\|Weekly\|Monthly, format, recipients (FR-049) |
| GET | `/reports/schedules?page=1&pageSize=50` | Paged schedule list (`pageSize` 1-200) with `items`, `totalCount`, and navigation metadata |
| GET/DELETE | `/reports/schedules/{id}` | Read or deactivate one schedule |

## Notifications (FR-052/53)

| Method | Path | Purpose | Permission |
|--------|------|---------|-----------|
| GET | `/notifications` | Paged in-app feed; consolidated repeats carry `occurrences` count | any |
| POST | `/notifications/{id}/read` \| `/read-all` | Mark read | any |
| GET | `/notification-preferences` | Per-type channel matrix (InApp/Email/Push/Sms) + thresholds | any |
| PUT | `/notification-preferences` | Update own preferences | any |

Notification types (Cycle 1): LowStock, OutOfStock, ExpiringStock, PoReceived,
PoOverdue, TransferAwaitingReceipt, LargeDiscount, LargeRefund, TillVariance,
UnusualVoids, NegativeStock, BillingFailure, DailyDigest, WeeklyDigest.
