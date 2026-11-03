# Demo data — D1 foundation

D1 adds a separate `net10.0` executable. It creates the catalog, images, suppliers and customer accounts only. **It does not create purchases, orders, payments, carts or historical inventory movements.** Empty stock and empty sales reports are expected until the separately approved D2. No D2/D3 acceptance is claimed.

The approved sources are preserved verbatim in [file 11](demo-data/sources/11_DAC_TA_DU_LIEU_DEMO.md) and [file 12](demo-data/sources/12_PROMPT_DU_LIEU_DEMO.md), including section 2b. Option A uses fictional brand/model names, original Vietnamese descriptions and programmatically drawn SVG illustrations. All people/contact/address records are fictional; email is `example.com` or NULL. Phone-format validity does not assert that a generated number is allocated or contactable.

## Prepare and run

Run from the repository root with .NET 10 and SQL Server. Choose a **new, dedicated** `PhoneStore_Demo_...` database; CI uses a generated `PhoneStore_QA_...` database. The tool refuses all other names, including `PhoneStore_Rebuild`, attached database files, missing flags and missing/invalid passwords. It neither creates schema nor runs automatically with the web host or migration.

```powershell
dotnet build tools/PhoneStore.DemoData -c Release
dotnet build src/PhoneStore.Web -c Release

# Configure this externally for YOUR dedicated demo target; never print a secret-bearing connection.
# ConnectionStrings__Store must specify an explicit server and database.
# Migrate the new database separately with the existing application command:
dotnet run --no-build -c Release --project src/PhoneStore.Web -- --migrate

$env:PHONESTORE_DEMO_ADMIN_PASSWORD = [Net.NetworkCredential]::new('', (Read-Host 'Demo admin password' -AsSecureString)).Password
$env:PHONESTORE_DEMO_CUSTOMER_PASSWORD = [Net.NetworkCredential]::new('', (Read-Host 'Demo customer password' -AsSecureString)).Password
try {
    dotnet run --no-build -c Release --project tools/PhoneStore.DemoData -- `
        --seed-demo-large --confirm --seed 2611 --as-of 2026-11-03 --scale full `
        --images-dir src/PhoneStore.Web/wwwroot/images/demo `
        --output .artifacts/demo-data/foundation.json
} finally {
    Remove-Item Env:PHONESTORE_DEMO_ADMIN_PASSWORD,Env:PHONESTORE_DEMO_CUSTOMER_PASSWORD
}
```

Set `ConnectionStrings__Store` externally before migration/seed. There are no embedded/default passwords. The two password environment variables must contain 10–128 characters with a letter and a digit. Administrator username is `demo.admin.large`; customers are `demo.customer.01`–`.60` for full. Passwords are hashed independently by the existing ASP.NET Core password hasher. Neither cleartext passwords nor password hashes are exported.

`--as-of` defaults to today's calendar date in UTC+7. Future dates and dates before 2002 are refused. Persisted account dates are UTC and spread across the preceding 24 months, with more registrations toward the end. Product launch dates are retained in the manifest because products have no timestamp column in the existing schema.

## Options and scale

| Option | Meaning |
|---|---|
| `--seed-demo-large --confirm` | Both required for every invocation |
| `--seed <integer>` | Deterministic seed; default 2611 |
| `--as-of YYYY-MM-DD` | UTC+7 calendar reference date |
| `--scale full` | Default: 10 brands, 6 categories, 12 suppliers, 50 products, 125 variants, 150 images, 60 customers + one ADMIN |
| `--scale small` | 10 brands, 6 categories, 3 suppliers, 6 products, 15 variants, 18 images, 6 customers + one ADMIN |
| `--images-dir <directory>` | Required physical destination corresponding to `/images/demo/` |
| `--output <file>` | Required sanitized manifest/report destination |
| `--reset` | Explicitly clear business/session rows in the allowlisted, current-schema target before rebuilding D1 |

`--verify-demo-data` is deliberately unavailable in D1; the complete historical SQL verifier belongs to D2. D1 automatically runs an independent foundation verifier and existing `DataAudit` after seeding. Exit codes: 0 success, 2 invalid input, 1 operation/verification failure. Errors do not print SQL connection details or secret values.

Full product segments are 14 budget / 16 midrange / 12 premium / 4 gaming / 4 foldable. The sixth category is available for later catalog use and has no assigned D1 product. Each product has two or three capacity/color variants, three different SVG views, a unique three-sentence Vietnamese description, and nine specification lines. Prices are calculated by existing `ProductService`/`Rules.Price`; the verifier checks them independently using SQL `ROUND`.

Full has four locked customers; small has one. Customer purchase-frequency distribution and exactly two customers without orders are **not yet accepted**: all customers have zero orders at D1. Product discontinuation, supplier retirement and stock scenarios likewise await D2.

## Safety, reset and failure recovery

Stop application writers while preparing or resetting the dedicated demo database. The tool's session lock serializes tool instances; it is not a maintenance mode for arbitrary web/API writers. Services retain their existing business locks and transactions.

Nonempty targets are refused unless `--reset` is present. Reset rechecks the allowlist/current migration and trusted schema, deletes only the known 18 application/session tables in FK order within a transaction, and retains the migration ledger. It does not drop databases, disable constraints, reseed identity counters, change migrations or alter tables. Never use reset on valuable data merely because a database name passes the allowlist.

Account registration/provisioning own transactions, so the entire seed is **not one atomic transaction**. Catalog creation is transactional. A failure can leave accounts or other completed stages; the next run refuses the nonempty database. Use a new dedicated database or explicitly reset the failed demo target after inspecting it. Simulation sessions are removed when catalog execution finishes or fails.

Image export preflights every collision and only adds absent files. Existing files with different content cause refusal; they are never overwritten. Images and database changes are not one distributed transaction. Use the committed 150 full-scale assets in `src/PhoneStore.Web/wwwroot/images/demo/`; `UseStaticFiles` serves them without website code changes. Tests request every committed image from a real Kestrel host and compare its bytes and SVG content type. The test adds no endpoint and needs no browser library.

## Reproducibility and independent verification

Generation builds an in-memory plan. Persistence calls existing services; direct account updates are limited to approved historical `CreatedAt`, plus explicit reset/session cleanup. Verification references persisted data and SQL, not Generation's arithmetic. No runtime service, old test, schema or UI source is modified.

The manifest uses stable natural keys rather than identity IDs. It includes product launch plans, persisted account/profile/catalog/supplier/variant/stock/image data, and asset hashes. Salted password hashes, rowversions, simulation sessions and migration application times are excluded. Elapsed time is outside the canonical hash. The same seed/date/scale and generator version must produce the same normalized hash, including after reset or on a different new database. This does not claim byte-identical SQL backups or deterministic password salts.

```powershell
dotnet run --project tests/PhoneStore.DemoData.Checks -c Release
# For real SQL + CLI + Kestrel proof, configure matching QA variables externally:
# PHONESTORE_TEST_CONNECTION, PHONESTORE_QA_SERVER, PHONESTORE_QA_DATABASE
dotnet run --project tests/PhoneStore.DemoData.Checks -c Release -- --integration
```

The integration runner creates a unique `PhoneStore_QA_DemoD1_<GUID>` database, generates test-only passwords in memory, injects them through environment variables, and removes only its generated database. It proves nonempty refusal, explicit reset, canonical hash equality, missing-secret/forbidden-target refusal, independent price-tamper detection, actual SQL unique/length constraints, independently salted credentials, zero final sessions and all 150 HTTP image responses. Existing CI steps are preserved; one new step runs these checks.

## D1 evidence and deferred decisions

See [D1 checklist](demo-data/D1_CHECKLIST.md) and [sanitized evidence](evidence/demo-data/d1-summary.json). A successful D1 does not complete the 43-item dataset acceptance gate.

The supplied quarter row values arithmetically sum to **701**; section 2b labels them 601. The user's explicit instruction is to keep approximately **600 orders**, never raise the target to 630. D1 creates no orders and does not silently edit either source. D2 must allocate approximately 600 within the accepted per-quarter tolerance and document the allocation when that step is requested.

No production deployment, D2/D3 implementation, merge, release or tag is included. The legacy Web `--seed-demo` command and `DemoSeeder` are unchanged and retain their documented limitations.
