# Database operations and demo dataset

SQL Server is the single persistence backend. [StoreDbContext](../src/PhoneStore.Infrastructure/Data/StoreDbContext.cs) maps 18 business/session tables. The nineteenth table is the single-row migration ledger. There are two account roles, not a role table; invoices and reports read existing data and do not create new tables.

Always configure `ConnectionStrings__Store` explicitly. Normal startup never migrates, seeds, provisions accounts or resets data. `--migrate` creates the new lineage or verifies its existing checksum; it refuses unmanaged/legacy databases. Never edit an applied migration or run the clean lineage against legacy `PhoneStore`.

## Explicit commands

Run after a Release build, using an explicitly configured local target:

```powershell
$env:ConnectionStrings__Store = 'Server=localhost;Database=PhoneStore_Rebuild;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --no-build -c Release --project src/PhoneStore.Web -- --migrate
dotnet run --no-build -c Release --project src/PhoneStore.Web -- --audit-db
```

`--audit-db` prints table counts and invariant errors, exits nonzero on failure and performs no business writes. It takes a transaction under the store business lock for a coherent result. It does not print connection strings, hashes or session IDs.

Provision an ADMIN with externally supplied credentials; existing usernames are rejected without overwriting passwords or promoting CUSTOMER accounts:

```powershell
$env:PHONESTORE_ADMIN_USERNAME = Read-Host 'Admin username'
$env:PHONESTORE_ADMIN_PASSWORD = [Net.NetworkCredential]::new('', (Read-Host 'Admin password' -AsSecureString)).Password
try {
    dotnet run --no-build -c Release --project src/PhoneStore.Web -- --provision-admin
} finally {
    Remove-Item Env:PHONESTORE_ADMIN_PASSWORD
}
```

For a demo database, run `--seed-demo` on the empty migrated database before separately provisioning any accounts. Supply `PHONESTORE_DEMO_ADMIN_PASSWORD` and `PHONESTORE_DEMO_CUSTOMER_PASSWORD` through the environment using secure input as above. There is no default password and secrets are never printed by the command. It creates `demo.admin.v1` and `demo.customer.01` through `.20`, with independently salted hashes. The shared customer credential is only a local demo convenience.

Seeding refuses unrelated/nonempty data. A complete unchanged demo rerun verifies its manifest/audit and returns without duplicating or resetting credentials. Once normal logins/edits change the sample, a rerun refuses to overwrite it. Use a separately named new demo database when another fixture is needed; no reset/delete command is provided. All seed operations are in one SQL transaction, including accounts, purchases, stock, orders, COD and history. A late error rolls back the entire dataset.

## Initial sample manifest

These counts describe the untouched demo immediately after seeding. Login/session expiry and later business operations naturally change counts and balances.

| Table | Rows | Meaning |
| --- | ---: | --- |
| TAI_KHOAN | 21 | 1 ADMIN, 20 CUSTOMER |
| KHACH_HANG | 20 | One profile per CUSTOMER |
| PHIEN | 21 | Demo SQL sessions; fixed eight-hour expiry |
| HANG | 20 | Phone brands |
| DANH_MUC | 20 | Phone categories |
| SAN_PHAM | 20 | Explicitly synthetic demo products |
| SAN_PHAM_ANH | 20 | Product image records referencing a local demo illustration |
| BIEN_THE | 40 | Two variants per product |
| TON_KHO | 40 | One inventory row per variant |
| GIO_HANG_CT | 20 | One persisted sample line per customer |
| NHA_CUNG_CAP | 20 | Synthetic suppliers |
| PHIEU_NHAP | 20 | Confirmed receipts |
| PHIEU_NHAP_CT | 40 | Two receipt lines per product |
| DON_HANG | 20 | Valid COD state scenarios |
| DON_HANG_CT | 40 | Immutable two-line order snapshots |
| THANH_TOAN | 20 | One COD payment per order |
| LICH_SU_DON | 58 | Actual transitions and receipt history |
| NHAT_KY_KHO | 113 | Actual inventory effects including one physical count |
| __PhoneStoreMigrations | 1 | Technical lineage; never padded to 20 |

Orders include 3 pending, 3 confirmed, 3 shipping, 3 completed/paid, 3 cancelled and 5 failed deliveries; 2 failed orders have physically returned goods. Initial receipts add 900 units; dispatch removes 22, physical return restores 4 and a documented count adds 2: physical=884, reserved=12, available=872. Two products and one supplier are disabled after transactions to demonstrate historical preservation.

Names, addresses, contact data, specifications and prices are examples. Product descriptions explicitly identify unverified demo specifications. The shared local SVG is an illustration, not a licensed manufacturer product photo. No fake store contact data is published.

## Integrity evidence

[DataAudit](../src/PhoneStore.Infrastructure/Data/DataAudit.cs) checks trusted foreign/check constraints, unmanaged tables, CUSTOMER/profile mapping, inventory rows, physical/journal balances, reservations versus pending/confirmed order lines, totals, snapshots, COD completion/collector/time, history chain/latest state/allowed actors, signed document effects, complete physical returns, purchase totals/journals and latest confirmed cost/calculated price. Latest cost follows NHAP journal event order, not draft creation order.

Database constraints protect FK/unique/rowversion and local column rules. Cross-table business invariants are enforced transactionally in services and audited explicitly; they are not falsely attributed to SQL CHECK constraints.

[Demo checks](../tests/PhoneStore.Checks/DemoSeedChecks.cs) create their own generated QA database, prove late-failure total rollback, exact manifest/balances, non-destructive rerun, tamper detection and unrelated-data refusal. [Seeded HTTP checks](../tests/PhoneStore.Checks/DemoHttpChecks.cs) exercise real logins, owned queries, checkout retry, confirm/dispatch/complete, invoice and reconciled reports. Only generated QA databases are removed afterward; local demo/legacy databases are preserved.
