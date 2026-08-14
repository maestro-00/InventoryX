# Offline Contract Readiness (002)

InventoryMS US4 (P4) production integration is blocked until this feature
satisfies the four readiness items in the frontend plan:

1. Register-token policy restricted to sync routes and the token's own register
2. Historical offline price/tax acceptance (reconnect cannot silently change money)
3. Authoritative rejected-sale review, retry, reconciliation, and audit
4. Snapshot completeness: favourites, receipt template, fractional/tracking/
   discount metadata, and deletion/version semantics (or versioned prep bundle)

## Status

Implemented against InventoryX Cycle 1 controllers under
`InventoryX.Presentation/Controllers/v1/SyncController.cs` and related
Application handlers. Consumer contract tests live in InventoryMS
`tests/contract/us4-offline-provider.contract.test.ts`.
