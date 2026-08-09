# InventoryX Backup and Restore Runbook

## Backup

Run a full SQL Server backup before every deployment and at least daily in production.
Retain encrypted backups according to the organisation's recovery policy and keep a copy
outside the application host. Verify each backup by restoring it to an isolated database.

## Point-in-time restore

1. Stop writes by placing the API in maintenance mode.
2. Restore the latest full backup, then apply the required differential and transaction-log backups to the target timestamp.
3. Validate tenant counts, the latest audit entries, and the most recent stock ledger movements.
4. Run the application migration check and smoke-test login, product lookup, stock, and sale creation.
5. Re-enable writes and record the restore timestamp and operator in the incident log.

The retention worker removes expired notification, digest-delivery, and report-export
history according to the active plan's `HistoryMonths`. Audit logs remain append-only;
catalogue aggregates use a 30-day recovery window before physical deletion.
