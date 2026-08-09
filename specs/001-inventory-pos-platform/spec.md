# Feature Specification: Inventory Management & Point of Sale Platform

**Feature Branch**: `feat/saleGroup` (spec directory: `001-inventory-pos-platform`)

**Created**: 2026-07-26

**Status**: Draft

**Input**: User description: "attached is spec for project" — full non-technical product
specification (v1.0, July 2026) for a cloud-based, subscription SaaS combining inventory
management and point of sale for businesses of all types.

## Clarifications

### Session 2026-07-26

- Q: Does this project deliver the backend API only, with web/mobile POS clients built
  separately against it? → A: Backend API only — all functionality is exposed via the
  documented API; POS web/mobile clients are separate projects consuming it, and offline
  sync is supported through API design.
- Q: What should the first planning/implementation cycle target? → A: Phase 1 core plus
  selected Phase 2 items — purchase orders & suppliers and batch/expiry tracking — since
  the existing codebase already has purchasing and sales foundations.
- Q: Which market should the first cycle configure and validate against? → A: Ghana
  first — GHS currency, Ghana VAT/levies, MTN MoMo-led mobile money, English UI.
- Q: What uptime commitment should the platform be designed and measured against? → A:
  99.9% monthly uptime (~43 minutes downtime/month), with planned maintenance announced
  in advance and scheduled outside peak trading hours.
- Q: What are the monthly sales-transaction caps for the Free and Standard plans? → A:
  Free 300 and Standard 3,000 sales transactions per month; Professional and Enterprise
  are unlimited.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sign Up, Onboard, and Make the First Sale (Priority: P1)

A new business owner signs up with email, password, business name, country, currency, and
business type. They set up their first location, add products (typed in, imported from a
spreadsheet with column matching and preview, or scanned via barcode with catalogue lookup),
record opening stock quantities and costs, and complete a sale at the POS. The sale
automatically reduces stock. A guided checklist tracks onboarding progress and sample data
can be loaded and removed with one click.

**Why this priority**: This is the end-to-end core loop of the product — without account
creation, a catalogue, stock, and a working sale that decrements stock, nothing else has
value. It is the smallest slice that proves the integrated inventory + POS promise.

**Independent Test**: Create a fresh account, add one product with opening stock of 10,
sell 2 at the POS, and verify stock on hand reads 8 and the sale appears in sales history.

**Acceptance Scenarios**:

1. **Given** a visitor on the sign-up page, **When** they provide email, password, business
   name, country, currency, and business type, **Then** an account is created with sensible
   defaults for that business type and they land in an onboarding checklist.
2. **Given** a spreadsheet of 500 products, **When** the owner imports it via the guided
   column-matching screen, **Then** a preview is shown and nothing is saved until confirmed.
3. **Given** a product with opening stock recorded, **When** a cashier completes a sale of
   that product, **Then** stock on hand decreases by the quantity sold immediately.
4. **Given** a new account, **When** the owner loads sample data and later removes it,
   **Then** all sample records disappear with one action and no real data is affected.
5. **Given** an owner mid-onboarding, **When** they leave and return later, **Then** the
   checklist shows exactly which steps remain.

---

### User Story 2 - Fast Checkout with Payments, Receipts, and Returns (Priority: P2)

A cashier serves customers at the counter: scanning barcodes (hardware scanner or device
camera), searching with typo tolerance, or tapping a configurable favourites grid. They
adjust quantities, apply permitted discounts, see live totals, park and recall held sales,
and take payment in cash, card, mobile money, bank transfer, cheque, store credit, gift
card, loyalty points, or on-account — including split payments across tender types. Receipts
print, email, SMS, or display as QR. Returns are found by receipt scan or sale search,
refunded at original price and tax to original tender, cash, or store credit; exchanges
settle only the difference.

**Why this priority**: Checkout speed and payment completeness are the daily-use heart of
the POS; the product principle "fast at the counter" lives or dies here.

**Independent Test**: Ring up a three-item sale using scan, search, and favourites; split
payment between cash and card; issue a receipt; then return one item against the receipt
and verify stock and refund records.

**Acceptance Scenarios**:

1. **Given** an in-progress sale, **When** the cashier scans a barcode, **Then** the item
   appears on the sale with no perceptible delay.
2. **Given** a sale total of 100, **When** the customer pays 60 by card and 40 in cash,
   **Then** both tenders are recorded against the one sale and change due is calculated on
   the cash portion.
3. **Given** a completed sale, **When** an item is returned with the receipt, **Then** the
   original price and tax apply automatically and stock returns to on-hand (or quarantine
   if flagged not resaleable).
4. **Given** a refund above the configured threshold or without a receipt, **When** a
   cashier attempts it, **Then** manager authorisation is required and the authoriser is
   recorded.
5. **Given** a cashier serving a customer, **When** they park the sale and start another,
   **Then** both sales are recallable and neither affects stock until completed.
6. **Given** a role with a maximum discount of 5%, **When** a cashier attempts a 10% manual
   discount, **Then** the discount is refused or escalated for manager approval, and any
   granted discount is recorded with the staff member's name.

---

### User Story 3 - Manage Stock Across Locations (Priority: P3)

A manager runs any number of locations (shops, warehouses, vehicles, stalls). Stock is held
per location and rolls up business-wide. Staff receive goods, transfer stock between
locations with dispatched/received states so in-transit goods are visible, make reasoned
adjustments (damage, theft, spoilage, expiry, sample, personal use, correction), process
supplier and customer returns, and record internal consumption. Counts run as full
stocktakes, cycle counts, or spot checks by scanning on a phone; variances need manager
approval before stock changes. Every movement is permanently recorded; corrections are new
entries, never silent erasures.

**Why this priority**: Accurate multi-location stock is the inventory half of the product's
promise and the prerequisite for reordering, valuation, and honest reporting.

**Independent Test**: Transfer 10 units between two locations, verify the in-transit state,
receive 8 with a 2-unit discrepancy flagged, then run a spot check and approve a variance —
confirming a permanent audit trail for each step.

**Acceptance Scenarios**:

1. **Given** stock at Location A, **When** a transfer to Location B is dispatched,
   **Then** the goods show as in transit and are counted in neither location's sellable
   stock until received.
2. **Given** an adjustment above the configured value threshold, **When** a stock clerk
   submits it, **Then** it takes effect only after manager approval.
3. **Given** a cycle count where counted differs from expected, **When** the manager
   reviews, **Then** the variance quantity and value are shown and stock changes only on
   approval, with counter identity and variance kept permanently.
4. **Given** any stock movement record, **When** a user corrects it, **Then** the original
   entry remains visible and the correction appears as a new entry.

---

### User Story 4 - Keep Selling When the Internet Fails (Priority: P4)

During an outage the POS keeps selling from product, price, and stock data already on the
device. Sales queue locally as pending with a clear but unobtrusive offline indicator and
pending count. When connectivity returns, queued sales upload automatically and stock
adjusts. Live-data actions (other-location stock checks, credit account charges, online
card payments) are unavailable offline and clearly marked, never silently failing. If
concurrent offline sales produce a negative or contested stock position, the system flags
it for review instead of silently overwriting.

**Why this priority**: Explicitly a firm requirement, not an extra — target markets have
unreliable connectivity, and a POS that stops selling loses the customer their business.

**Independent Test**: Disconnect the device, complete three sales, verify the offline
indicator and pending count, reconnect, and confirm all three sync with correct stock
adjustment and any conflicts flagged.

**Acceptance Scenarios**:

1. **Given** a register that loses connectivity mid-shift, **When** the cashier continues
   selling catalogued items, **Then** sales complete and queue locally with a visible
   pending count.
2. **Given** queued offline sales, **When** the connection returns, **Then** sales upload
   without user action and stock levels adjust.
3. **Given** the same last unit sold at two locations while one was offline, **When** both
   sync, **Then** the contested stock position is flagged for review rather than silently
   resolved.
4. **Given** an offline register, **When** a cashier attempts an on-account charge,
   **Then** the action is clearly marked unavailable rather than failing silently.

---

### User Story 5 - Subscribe, Upgrade, and Leave Without Hostage Data (Priority: P5)

A customer starts a 14-day Professional trial without card details. At trial end, an
unsubscribed account drops to the Free plan — data retained, over-limit features read-only.
Customers self-serve upgrades (immediate, pro-rata), downgrades (end of period, with an
explicit warning listing exactly what exceeds the lower plan's limits), and cancellation.
Failed payments retry across a 7-day grace period with owner notifications, then the
account turns read-only. Data is retained 90 days post-cancellation for reactivation or
export, then permanently deleted after a final warning. Full data export is available at
any time. Invoices and receipts are automatic; billing details, tax numbers, and past
invoices are self-service.

**Why this priority**: The commercial engine of the SaaS — but the product must first be
worth paying for, so it follows the operational stories.

**Independent Test**: Run an account through trial → Free fallback → upgrade to Standard →
simulated failed payment → grace → read-only → export → reactivate, verifying state and
notifications at each step.

**Acceptance Scenarios**:

1. **Given** a trial reaching day 14 without subscription, **When** the period ends,
   **Then** the account becomes Free-plan: data intact, over-limit features read-only, no
   lockout.
2. **Given** a Standard customer upgrading to Professional mid-cycle, **When** they
   confirm, **Then** the upgrade applies immediately and only the pro-rata difference is
   charged.
3. **Given** a downgrade that would exceed the lower plan's limits, **When** the customer
   initiates it, **Then** the system states exactly what must be reduced before the
   downgrade takes effect at period end.
4. **Given** a failed renewal payment, **When** 7 days of retries and notifications pass
   without success, **Then** the account becomes read-only — visible and exportable, no new
   sales or stock changes.
5. **Given** a cancelled account within 90 days, **When** the owner returns, **Then** they
   can reactivate or export everything; after 90 days and a final warning, data is
   permanently deleted.

---

### User Story 6 - Open, Run, and Reconcile a Register Shift (Priority: P6)

A cashier opens a register by counting and recording an opening float, records cash in/out
during the shift with reasons (petty cash, banking, change orders), and closes with a
counted drawer. The system compares counted against expected cash, records the variance,
and produces a shift (Z) report: sales, payment types, refunds, discounts, voids, and cash
variance for that register and staff member. Registers cannot close with an uncounted
drawer; variances above threshold are flagged to a manager.

**Why this priority**: Cash control and theft prevention are top concerns for retail
customers and complete the daily POS operating cycle.

**Independent Test**: Open a register with a float, make sales in mixed tenders, record a
petty cash withdrawal, close with a deliberate shortfall, and verify the variance is
computed, flagged, and reported on the Z-report.

**Acceptance Scenarios**:

1. **Given** an unopened register, **When** a cashier tries to sell, **Then** they must
   first record an opening float count.
2. **Given** an end-of-shift close, **When** counted cash differs from expected beyond the
   threshold, **Then** the variance is recorded and flagged to a manager.
3. **Given** a closed shift, **When** the Z-report is produced, **Then** it summarises
   sales, tender breakdown, refunds, discounts, voids, and variance for that register and
   staff member.

---

### User Story 7 - Reorder Before Running Out (Priority: P7)

Products carry reorder points and quantities — set manually or suggested from sales rate
and supplier lead time. The system raises low-stock, out-of-stock, expiry, overstock, and
slow-moving alerts, and groups everything due for reorder into a suggested purchase order
by supplier. Purchase orders move draft → awaiting approval → sent → partially received →
fully received → closed (cancellable before closure), with value-based approval gates.
Goods receipt records what actually arrived; short, over, and damaged deliveries keep the
order open for the balance or close it short with a reason. Supplier invoices are matched
to orders with price-difference flags, and landed costs spread across shipment items.

**Why this priority**: Reordering converts accurate stock data into prevented stockouts —
a major retention driver — but requires the stock foundation (P1–P3) to exist first.

**Independent Test**: Set a reorder point, sell below it, verify the alert and the
suggested PO grouped by supplier, approve and send it, receive a short delivery, and
confirm the order stays open for the balance.

**Acceptance Scenarios**:

1. **Given** a product at its reorder point, **When** stock falls to or below it, **Then**
   a low-stock alert fires and the item joins the suggested purchase order for its
   supplier.
2. **Given** a purchase order above the approval threshold, **When** a manager tries to
   send it, **Then** it requires approval first.
3. **Given** an order of 100 units, **When** 80 arrive and 5 are damaged, **Then** receipt
   records reflect reality and the order remains open for the balance or closes short with
   a reason.
4. **Given** a supplier invoice differing from ordered prices, **When** it is recorded
   against the order, **Then** the discrepancy is flagged.

---

### User Story 8 - See the Business at a Glance and in Depth (Priority: P8)

A manager signs in to a dashboard: today's sales versus the same day last week, transaction
count, average basket, cash in drawer, items sold, low-stock and expiry warnings, top
sellers — every figure linking to its detail. Standard reports cover sales, profit, stock,
purchasing, customers, staff/operations, and tax; all filter by date, location, category,
and staff; all export to spreadsheet or PDF and can be scheduled by email. Higher plans add
custom report layouts and demand forecasting that suggests reorder points, flags likely
stockouts, and estimates working capital tied up in stock.

**Why this priority**: Reporting is where owners realise the value of the data the earlier
stories capture; it depends on all of them being in place.

**Independent Test**: After a day of seeded transactions, verify the dashboard figures
match the underlying records, drill into each, filter a sales report by location and staff,
export it, and schedule a weekly email.

**Acceptance Scenarios**:

1. **Given** a manager signing in, **When** the dashboard loads, **Then** today's headline
   figures are shown against the same day last week and each links to its detail.
2. **Given** any standard report, **When** filters for date range, location, category, and
   staff are applied, **Then** results, on-screen view, and exports all reflect the
   filters.
3. **Given** a scheduled report, **When** its schedule fires, **Then** the report arrives
   by email in the chosen format.
4. **Given** a cashier role, **When** they attempt to view profit or margin figures,
   **Then** access is refused per role permissions.

---

### User Story 9 - Sell to Known Customers on Their Terms (Priority: P9)

Businesses record customers (individuals or business accounts with multiple contacts) with
price tiers, credit limits, and payment terms. Pricing supports tiers (retail/trade/
wholesale), volume breaks, and time-bound promotions restricted by location or customer
group. Quotes convert to sales orders that reserve stock, fulfil in one or more
deliveries, and become invoices; backorders track unfulfilled demand. Debt ages into
current/30/60/90+ buckets with reminders; credit customers exceeding their limit are
blocked at the counter subject to manager override.

**Why this priority**: Extends the platform to wholesalers and credit-based trade —
valuable segments, but dependent on core selling and stock being solid.

**Independent Test**: Create a trade-tier customer with a credit limit, sell on account at
tier pricing, verify the debt ages correctly, exceed the limit, and confirm the counter
block and manager override.

**Acceptance Scenarios**:

1. **Given** a customer on the trade tier, **When** they are attached to a sale, **Then**
   trade prices apply automatically.
2. **Given** a credit customer at their limit, **When** a cashier attempts an on-account
   sale, **Then** the sale is blocked unless a manager overrides.
3. **Given** a quote, **When** it is accepted, **Then** it converts to a sales order that
   reserves stock and later becomes an invoice on fulfilment.

---

### User Story 10 - Track Any Kind of Inventory (Priority: P10)

Per product — not per account — businesses enable: variants (size/colour matrices with
per-variant stock, barcode, price), batch/lot tracking (numbers, manufacture and expiry
dates, oldest-expiry-first issue, recall trace to supplier and to every buyer),
serial-number tracking (unit-level custody and warranty dates), fractional
weight/volume/length sales with scale integration, bundles/kits (selling one reduces
components), manufactured items with recipes and production runs, raw materials and
consumables, tracked assets (location, condition, holder, maintenance, depreciation),
returnable deposit items, non-stock items (services, fees, gift cards), and consignment
stock with owner amounts tracked separately.

**Why this priority**: "Works for any kind of inventory" is a core product principle and
the key differentiator for pharmacies, food service, and workshops — but each mode layers
on the simple-item foundation of P1–P3.

**Independent Test**: Create one product of each behaviour type, transact each (sell a
variant, sell from the oldest-expiring batch, sell a serialised unit, sell 1.5 kg of a
weighed item, sell a bundle), and verify stock effects match each mode's rules.

**Acceptance Scenarios**:

1. **Given** a t-shirt in 3 sizes and 4 colours, **When** it is set up as a variant
   product, **Then** twelve variants exist, each with its own stock count, barcode, and
   price.
2. **Given** a batch-tracked product with two batches, **When** it is sold, **Then** stock
   issues from the oldest expiry by default, and a batch can be traced to its supplier and
   every buyer.
3. **Given** a serialised product, **When** a specific serial is sold, **Then** that unit's
   custody history and warranty dates follow it from receipt to sale.
4. **Given** a bundle of three components, **When** one bundle sells, **Then** each
   component's stock reduces accordingly.
5. **Given** a recipe-based item, **When** a production run converts inputs to outputs,
   **Then** raw material stock falls and finished stock rises by the recorded quantities.

---

### Edge Cases

- Two registers sell the last unit of the same product while one is offline: the contested
  stock position is flagged for review, never silently overwritten.
- A downgrade would strand data above the lower plan's limits (e.g. 8 locations dropping to
  a 1-location plan): the customer is told exactly what to reduce; nothing is deleted.
- A batch expires while still in sellable stock: expiry alerts fire at configured
  intervals beforehand; expired stock is flagged for adjustment with reason "expiry".
- A return arrives for an item bought before a price change: the original sale's price and
  tax apply, not today's.
- A refund is requested without a receipt: manager authorisation is mandatory and the
  authoriser is recorded.
- A stocktake happens while trading continues (spot check / cycle count): sales during the
  count are reconciled so the variance reflects reality at count time.
- An account in read-only (grace expired or Free fallback) attempts a sale or stock change:
  the action is refused with a clear explanation; viewing and export always work.
- A spreadsheet import contains malformed rows: the preview identifies them; valid rows
  can proceed; nothing is saved until confirmed.
- The valuation method is changed after months of trading: the system warns that historic
  figures will change and requires explicit confirmation.
- A cashier PIN is entered on a shared register: the sale, discounts, and refunds attribute
  to that individual, not the device.

## Requirements *(mandatory)*

### Functional Requirements

#### Accounts, Users & Roles

- **FR-001**: System MUST let a visitor create a business account with email, password,
  business name, country, currency, and business type, and MUST apply business-type
  defaults (retail, food, pharmacy, wholesale, service, other).
- **FR-002**: System MUST completely isolate each customer account's data; no customer can
  ever see another customer's data.
- **FR-003**: System MUST provide the default roles Owner, Administrator, Manager,
  Cashier/Sales, Stock clerk/Warehouse, and Accountant/Read-only, with the access
  boundaries defined in the product description (e.g. cashiers cannot change prices, see
  profit, or refund above a limit; stock clerks cannot sell or see financial data; the
  Owner cannot be removed).
- **FR-004**: System MUST allow staff invitations by email with role assignment, and MUST
  scope Manager access to assigned locations.
- **FR-005**: System MUST support custom roles (per-permission toggles) on plans that
  include them.
- **FR-006**: System MUST enforce password minimum standards, offer two-factor
  authentication to all users, and let an account owner make it mandatory.
- **FR-007**: System MUST support a short register PIN per staff member, separate from the
  full account password, attributing every POS action to the individual.
- **FR-008**: System MUST log sensitive actions (price changes, refunds, voids, adjustments,
  permission changes) in an audit trail attributing who did what and when.

#### Subscription & Billing

- **FR-009**: System MUST offer self-service monthly and annual subscriptions across four
  plan tiers (Free/Starter, Standard, Professional, Enterprise) with the published limits
  on locations, users, products, registers, features, and data history, monthly
  sales-transaction caps of 300 (Free) and 3,000 (Standard) with Professional and
  Enterprise unlimited, and annual billing discounted (~17%).
- **FR-010**: System MUST enforce plan limits and feature gates (purchase orders, batch &
  expiry, serials, multi-currency, custom roles, advanced reports/forecasting,
  integrations) per the plan matrix, plus paid add-ons for extra locations, users, and
  registers.
- **FR-011**: System MUST provide a 14-day Professional trial without card details; at
  expiry without subscription, the account drops to Free with data retained and over-limit
  features read-only.
- **FR-012**: System MUST apply upgrades immediately with pro-rata charging and apply
  downgrades at period end, warning beforehand exactly which limits would be exceeded.
- **FR-013**: System MUST handle failed payments with a 7-day retry/notification grace
  period, then place the account in read-only (visible and exportable; no new sales or
  stock changes).
- **FR-014**: System MUST retain cancelled accounts' data for 90 days for reactivation or
  export, then permanently delete after a final warning.
- **FR-015**: System MUST allow full data export at any time, including after cancellation.
- **FR-016**: System MUST generate and email invoices and receipts automatically, support
  self-service billing administration (card, billing contact, tax/VAT number, past
  invoices), apply taxes per billing country, and support card payments plus, where
  available in a market, mobile money and bank transfer (in Ghana, mobile money is a
  first-class billing method on all plans and cycles).

#### Onboarding

- **FR-017**: System MUST guide new accounts through a resumable onboarding checklist:
  account, first location, products, opening stock, staff invitations, register setup.
- **FR-018**: System MUST offer three product-entry routes: manual entry, spreadsheet
  import with guided column matching and a pre-save preview, and barcode scanning with
  lookup against a shared product catalogue.
- **FR-019**: System MUST provide sample data loadable for exploration and removable with
  one action.

#### Product Catalogue & Inventory Types

- **FR-020**: System MUST hold product records with name, description, SKU, barcode,
  category, tags, unit of measure, cost and selling price, tax treatment, suppliers with
  supplier codes, reorder point/quantity, photographs, customer-defined custom fields, and
  status (active/inactive/discontinued).
- **FR-021**: System MUST support, selectable per product: simple items; variant items
  (attribute matrices, per-variant stock/barcode/price); batch/lot tracking (batch number,
  manufacture and expiry dates, remaining-per-batch, oldest-expiry-first issue, full
  backward and forward recall traceability); serial-number tracking (unit-level from
  receipt to sale, warranty dates); fractional weight/volume/length quantities with
  weighing-scale integration; bundles/kits (pre-assembled or assembled at sale, component
  stock reduced on sale); manufactured items with recipes/bills of materials and
  production runs; raw materials and consumables (held, never sold); assets (location,
  condition, holder, maintenance schedule, depreciation); returnable deposit items with
  deposits held; non-stock items (services, labour, fees, digital goods, gift cards); and
  consignment stock with third-party ownership and amounts owed tracked separately.

#### Locations, Stock Movements & Counts

- **FR-022**: System MUST support any number of locations per account (subject to plan),
  hold stock per location, and roll up to a business-wide view.
- **FR-023**: System MUST support receiving against purchase orders, two-state transfers
  (dispatched/received with in-transit visibility and discrepancy capture), reasoned
  adjustments from a configurable reason list with value-threshold manager approval,
  returns to supplier with credit expected, customer returns to stock or quarantine, and
  internal consumption.
- **FR-024**: System MUST permanently record every stock movement with date, time,
  quantity, user, and reason; corrections MUST appear as new entries with the original
  preserved.
- **FR-025**: System MUST support full stocktakes, cycle counts, and spot checks performed
  by scanning on a phone or tablet, showing expected vs counted, calculating variance
  quantity and value, requiring manager approval before stock changes, and permanently
  recording who counted what.

#### Reordering, Alerts & Valuation

- **FR-026**: System MUST support per-product reorder points and quantities, manually set
  or suggested from recent sales rate and supplier lead time.
- **FR-027**: System MUST raise low-stock, out-of-stock, configurable-interval expiry,
  overstock, and slow-moving alerts, and produce a suggested purchase order grouped by
  supplier for manager review.
- **FR-028**: System MUST support one stock valuation method per account — weighted average
  cost, FIFO, or specific cost for serialised items — explained in plain language at
  selection; changing method later MUST require confirmation that historic figures change.

#### Purchasing & Suppliers

- **FR-029**: System MUST hold supplier records (contacts, addresses, payment terms, lead
  time, currency, supplied products with supplier codes and prices, order history) and a
  performance summary (on-time rate, achieved lead time, price changes).
- **FR-030**: System MUST move purchase orders through draft → awaiting approval → sent →
  partially received → fully received → closed, with cancellation available before
  closure, value-threshold approval gates, and creation manually, from reorder
  suggestions, or from a low-stock alert.
- **FR-031**: System MUST send purchase orders to suppliers as emailed PDFs or downloads,
  record actual receipts including short/over/damaged deliveries, and allow closing short
  with a reason.
- **FR-032**: System MUST record supplier invoices against orders, flag price differences,
  and spread landed costs (freight, duty, clearing, insurance) across shipment items.

#### Customers, Pricing & Credit

- **FR-033**: System MUST support anonymous walk-in sales and recorded customers
  (individuals or business accounts with multiple contacts) holding contact details,
  addresses, tax number, price tier, credit limit, payment terms, and purchase history.
- **FR-034**: System MUST support standard prices, per-customer price tiers, volume
  breaks, and promotions (percentage, fixed, BOGO, bundle prices) with start/end dates and
  optional location or customer-group restrictions.
- **FR-035**: System MUST cap manual counter discounts per role and record every discount
  with the staff member's name.
- **FR-036**: System MUST support quotes converting to sales orders that reserve stock,
  fulfil in one or more deliveries, become invoices, and track backorders.
- **FR-037**: System MUST track receivables with due dates, age debt into
  current/30/60/90+ buckets, send reminders, and automatically block over-limit credit
  customers at the counter subject to manager override.

#### Point of Sale

- **FR-038**: System MUST provide a sale screen built around add items → take payment →
  finish, with barcode scanning (hardware or camera) as primary input, typo-tolerant
  search, a customer-configurable favourites grid, quantity/weight/price entry with scale
  integration, line-level actions (quantity, discount, note, remove), live totals, stock
  visibility at this and other locations, and multiple concurrently held sales.
- **FR-039**: System MUST support tender types cash, card, mobile money, bank transfer,
  cheque, store credit, gift card, loyalty points, and on-account, with split payment
  across multiple tenders in one sale, change-due calculation for cash, provider/terminal
  integration where available, and manual recording where not.
- **FR-040**: System MUST issue receipts by print, email, SMS/messaging, or QR display,
  with customisable layout (logo, business details, tax registration, footer, return
  policy) and per-country accommodation of fiscalisation and tax-format rules.
- **FR-041**: System MUST process returns located by receipt scan or sale search at
  original price and tax; refunds to original tender, cash, or store credit per business
  policy; exchanges as return-plus-sale settling only the difference; and require recorded
  manager authorisation for refunds above threshold or without receipt.
- **FR-042**: System MUST manage register cash: recorded opening float, reasoned cash
  in/out during shift, mandatory closing count comparing counted to expected, variance
  recording with above-threshold manager flags, and a Z-report per register and staff
  member covering sales, tenders, refunds, discounts, voids, and variance.
- **FR-043**: System MUST run on computers, tablets, and phones, in a browser or as an
  installed app, and operate with common POS hardware (scanners, receipt printers, cash
  drawers, card terminals, scales, customer displays, label printers) or with no hardware
  at all.

#### Offline Operation

- **FR-044**: POS MUST continue selling during connectivity loss using locally held
  product, price, and stock data; queue sales as pending; show an unobtrusive offline
  indicator with pending count; and sync automatically on reconnection with stock
  adjustment.
- **FR-045**: System MUST clearly mark live-data actions (remote stock checks, credit
  charges, online card payments) as unavailable offline rather than failing silently.
- **FR-046**: System MUST flag negative or contested stock positions arising from
  concurrent offline sales for review rather than silently overwriting.
- **FR-047**: Mobile apps MUST function offline for stock counting and selling.

#### Reports, Dashboard & Forecasting

- **FR-048**: System MUST show a manager dashboard on sign-in: today's sales vs same day
  last week, transaction count, average basket, cash in drawer, items sold, low-stock and
  expiry warnings, top sellers — each figure linking to its detail.
- **FR-049**: System MUST provide the standard report families — sales, profit, stock,
  purchasing, customer, staff/operations, and tax (formatted for the customer's
  jurisdiction) — all filterable by date range, location, category, and staff; viewable on
  screen; exportable to spreadsheet and PDF; and schedulable by email daily, weekly, or
  monthly.
- **FR-050**: System MUST restrict financial figures (profit, margins, costs) to roles
  permitted to see them.
- **FR-051**: System MUST, on higher plans, let customers build and save custom report
  layouts, and project per-product demand from sales history (seasonality and growth) to
  suggest reorder points and quantities, flag likely stockouts ahead of time, and estimate
  working capital tied up in stock.

#### Notifications

- **FR-052**: System MUST let each user choose which notifications they receive and by
  which channel (in-app, email, mobile push, SMS for critical items) from the catalogue:
  low/out of stock, expiring stock, PO received or overdue, transfer awaiting receipt,
  large discount or refund, till variance, unusual voiding, credit customer over limit,
  negative stock, subscription payment failure, and daily/weekly digests.
- **FR-053**: System MUST keep notifications useful: configurable thresholds, grouping of
  related alerts, and consolidation of repeats for the same unresolved issue.

#### Integrations & Data Portability

- **FR-054**: System MUST provide an open interface from the outset so customers and third
  parties can build their own connections.
- **FR-055**: System MUST support integrations in priority order: accounting (sales,
  purchases, stock values pushed without re-entry), payment providers per launch market,
  e-commerce storefronts with stock kept in step to prevent overselling, marketplaces,
  shipping/delivery, and communication services.
- **FR-056**: System MUST support spreadsheet import and export as a universal fallback
  across products, stock, customers, and suppliers.

#### Localisation & Compliance

- **FR-057**: System MUST support multiple interface languages extensible without
  redevelopment, and follow the customer's country for date, number, currency formats and
  tax rules; higher plans MUST support trading in more than one currency.
- **FR-058**: System MUST meet the data protection obligations of its operating markets:
  customers own their data, can export it in full at any time, and can have it deleted on
  request.
- **FR-059**: System MUST never store payment card details on the platform itself.
- **FR-060**: System MUST back up customer data continuously with restore capability, and
  keep accidental deletions recoverable for a reasonable period.
- **FR-061**: System MUST meet recognised accessibility standards: full keyboard
  operation, screen reader compatibility, contrast compliance, and legibility on small
  screens in bright sunlight.

### Key Entities

- **Account (Tenant)**: A customer business; owns all data within it; carries country,
  currency, business type, valuation method, plan, and subscription state.
- **Subscription & Plan**: The commercial relationship — tier, billing cycle, limits,
  feature gates, trial/grace/read-only lifecycle states, invoices.
- **User & Role**: A named individual with credentials, optional register PIN, an assigned
  role (default or custom) and location scope.
- **Location**: A shop, warehouse, storeroom, stall, or vehicle holding per-location stock.
- **Product**: Catalogue record with pricing, tax, suppliers, reorder settings, custom
  fields, and an inventory-behaviour type; parent of **Variants**.
- **Batch/Lot**: A received group of a batch-tracked product — number, manufacture and
  expiry dates, remaining quantity, supplier trace.
- **Serial Unit**: An individually tracked physical unit with custody history and warranty
  dates.
- **Stock Level**: Quantity and value of a product (or variant/batch/serial) at a location.
- **Stock Movement**: Immutable record of any change — receipt, transfer (dispatch/receive),
  adjustment, return, consumption, sale — with user, time, and reason.
- **Stock Count**: A full, cycle, or spot count with expected vs counted, variance, counter,
  and approver.
- **Supplier**: Vendor record with terms, lead times, supplied products, order history, and
  performance metrics.
- **Purchase Order**: Supplier order with state machine, approvals, receipts, invoice
  matching, and landed costs.
- **Customer**: Walk-in or recorded buyer (individual or business) with tier, credit terms,
  balance, and history.
- **Sale**: A POS or invoiced transaction with lines, discounts, taxes, tenders, receipt,
  and offline-sync status; related **Return/Exchange** records reference the original.
- **Register & Shift**: A selling station and its operating session — float, cash
  movements, closing count, variance, Z-report.
- **Quote / Sales Order / Invoice**: The pre-sale-to-receivable chain with stock
  reservation, fulfilment, and debt ageing.
- **Promotion / Price Tier**: Pricing rules with validity windows and scope restrictions.
- **Notification**: A user-subscribed alert instance with channel, threshold, and
  consolidation state.
- **Audit Log Entry**: Immutable record of a sensitive action and its actor.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new customer can complete onboarding and record their first sale within 30
  minutes of signing up, without assistance.
- **SC-002**: At least 60% of trial accounts record a first sale within 24 hours of
  sign-up.
- **SC-003**: Adding an item to a sale by barcode scan registers with no perceptible wait
  (under 1 second), and average time per item at the POS stays under 5 seconds.
- **SC-004**: Ordinary screens load in about 2 seconds; long-period reports show progress
  rather than appearing frozen.
- **SC-005**: 99.9%+ of offline sales sync successfully once connectivity returns, and
  100% of sync conflicts are surfaced for review rather than silently resolved.
- **SC-006**: A single account handles hundreds of thousands of products, dozens of
  locations, and hundreds of transactions per minute at peak without degradation.
- **SC-007**: Stock variance found at stocktake averages below 2% of stock value across
  active accounts after three months of use.
- **SC-008**: Customers can export 100% of their data at any time, including within the 90
  days after cancellation.
- **SC-009**: The service meets 99.9% monthly uptime (no more than ~43 minutes of
  unplanned downtime per month), with planned maintenance announced in advance and
  scheduled outside peak trading hours across supported regions.
- **SC-010**: Trial-to-paid conversion, monthly customer and revenue retention, support
  tickets per hundred accounts, and customer satisfaction are all measurable from day one
  via the platform's own records.

## Assumptions

- This repository delivers the backend API only. Every capability in this spec —
  including POS flows, offline sale queuing and sync, receipts, and dashboards — is
  delivered as documented API behaviour; web and mobile client applications are separate
  projects consuming that API. Client-side concerns (screen layouts, on-device storage,
  hardware drivers) are out of scope here, but the API MUST provide everything those
  clients need (e.g. catalogue snapshots for offline use, idempotent queued-sale upload,
  conflict flagging).
- Ghana is the first launch market: GHS as the default currency, Ghana VAT and levies as
  the first tax rule set, mobile money (MTN MoMo first, then Telecel Cash and AT Money)
  as the priority payment integrations alongside card, and English as the launch
  language. Country-specific tax rules, payment providers, fiscalisation formats, and
  languages remain configurable per country rather than hard-coded, so later markets add
  configuration, not redevelopment.
- The Free plan is assumed permanent (not an expiring trial), per the plan matrix; the
  source document leaves this open for commercial review.
- Hardware is purchased by the customer; the platform targets commonly available
  peripherals and never requires them. Resold hardware bundles are a commercial add-on
  decision, out of product scope.
- The first accounting integration target is a pending commercial decision; the
  requirement is the generic capability plus the open interface.
- Customers have at least intermittent internet access; permanent offline operation is not
  supported.
- Customers are responsible for the accuracy of data they enter, including opening stock;
  legal, tax, and regulatory compliance in each market remains the customer's
  responsibility — the platform supplies the records and formats to meet it.
- The platform records financial transactions but is not an accounting system; it
  integrates with accounting software instead.
- Explicitly out of scope for version 1: full accounting/general ledger, payroll and staff
  scheduling, a built-in customer-facing online shop (integration only), manufacturing
  planning beyond simple recipes and assembly, route planning and fleet management,
  loyalty beyond simple points accrual, and franchise-level consolidated reporting across
  separate legal entities.
- Delivery follows the source document's phasing: Phase 1 core (accounts, billing, roles,
  catalogue with simple and variant items, multi-location stock and movements, basic POS
  with cash and card, receipts, returns, shift and cash management, offline selling,
  essential reports, spreadsheet import/export); Phase 2 depth (purchasing, batch/expiry/
  serial, counts, bundles and recipes, customers and credit, price tiers and promotions,
  accounting integration, mobile apps); Phase 3 scale (advanced reporting, forecasting,
  e-commerce/marketplace, multi-currency, custom roles, open interface, landed costs,
  consignment and assets). User story priorities above reflect this.
- Cycle 1 implementation deferrals within otherwise in-scope requirements (each remains
  a full-platform requirement; reserved extension points exist for all): stock valuation
  ships weighted average cost only, with FIFO and specific cost deferred — batch receipt
  costs are captured from day one so FIFO can layer on without data loss (FR-028);
  POS tenders ship Cash, Card, MobileMoney, BankTransfer, and Cheque, with StoreCredit,
  GiftCard, loyalty points, and on-account deferred until their balance ledgers arrive
  with customers/credit in Cycle 2 (FR-039), and refunds accordingly go to original
  tender or cash (FR-041); the shared-catalogue barcode enrichment lookup is deferred —
  scanning resolves against the tenant's own catalogue in Cycle 1 (FR-018); interface
  languages beyond English are deferred alongside multi-currency (FR-057).
- The first planning/implementation cycle targets Phase 1 core PLUS two Phase 2 items
  brought forward — purchase orders & suppliers (story P7, FR-029–FR-032) and batch/expiry
  tracking (the batch/lot portions of FR-021, FR-027) — because the existing codebase
  already provides purchasing and sales foundations. Remaining Phase 2 and Phase 3 scope
  is planned in later cycles.
