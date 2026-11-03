# Demo data — D2 chronological sales

D2 extends the separate `net10.0` executable with eight complete quarters and the current partial quarter of purchases, COD orders, history, stock and carts. Full uses the original quarter targets, totaling **601 orders**, plus 56 purchases. All business effects go through existing services with a simulated `TimeProvider`. D3 UI acceptance and the final gate remain separate.

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
        --output .artifacts/demo-data/dataset.json
} finally {
    Remove-Item Env:PHONESTORE_DEMO_ADMIN_PASSWORD,Env:PHONESTORE_DEMO_CUSTOMER_PASSWORD
}
```

Set `ConnectionStrings__Store` externally before migration/seed. There are no embedded/default passwords. The two password environment variables must contain 10–128 characters with a letter and a digit. Administrator username is `demo.admin.large`; customers are `demo.customer.01`–`.60` for full. Passwords are hashed independently by the existing ASP.NET Core password hasher. Neither cleartext passwords nor password hashes are exported.

`--as-of` defaults to today's calendar date in UTC+7. Future dates and dates before 2002 are refused. The snapshot cutoff is **midnight at the beginning of that date in UTC+7**, making repeated runs deterministic without today's future timestamps. Persisted business dates are UTC. Account dates span the preceding 24 calendar months, with more registrations toward the end. The founding customer starts at the beginning of the month 24 months earlier so the first quarter can have completed sales even for a quarter-end `--as-of`; other accounts retain their staggered dates. Product launch/retirement dates are retained in the manifest because products have no timestamp column in the existing schema.

## Options and scale

| Option | Meaning |
|---|---|
| `--seed-demo-large --confirm` | Both required when generating data |
| `--seed <integer>` | Deterministic seed; default 2611 |
| `--as-of YYYY-MM-DD` | UTC+7 calendar reference date |
| `--scale full` | Default: 601 orders, 56 purchases, 50 products/125 variants/150 images, 60 customers + ADMIN, 10 brands, 6 categories, 12 suppliers |
| `--scale small` | 60 orders, 22 purchases, 6 products/15 variants/18 images, 6 customers + ADMIN, 10 brands, 6 categories, 3 suppliers |
| `--images-dir <directory>` | Required physical destination corresponding to `/images/demo/` |
| `--output <file>` | Required sanitized manifest/report destination |
| `--reset` | Explicitly clear business/session rows in the allowlisted, current-schema target before rebuilding |
| `--verify-demo-data` | Read-only database acceptance, without seed/confirm/reset or password requirements |
| `--manifest <file>` | Required in verification mode: context and normalized snapshot from the seed output |

Every seed runs independent SQL verification and existing `DataAudit`. To verify the unchanged snapshot later, with only `ConnectionStrings__Store` configured:

```powershell
dotnet run --no-build -c Release --project tools/PhoneStore.DemoData -- `
    --verify-demo-data --manifest .artifacts/demo-data/dataset.json `
    --images-dir src/PhoneStore.Web/wwwroot/images/demo `
    --output .artifacts/demo-data/verification.json
```

Verification takes seed/date/scale/lifecycle facts from the supplied manifest; it does not call Generation or ask for account passwords. It prints counts for all 19 tables, quarter order/revenue targets, statuses, metrics and failed invariant labels, and writes a sanitized report. Exit codes: 0 valid, 2 invalid arguments, 1 operation/invariant failure. It refuses mixed verify/write flags and refuses to overwrite its input manifest with the report. SQL reads run under the existing shared business lock. No SQL business writes occur in verification mode. Errors do not print connection details or secrets.

Full product segments are 14 budget / 16 midrange / 12 premium / 4 gaming / 4 foldable. The sixth category is available for later catalog use. Each product has two or three capacity/color variants, three different SVG views, a unique three-sentence Vietnamese description, and nine specification lines. Prices are calculated by existing `ProductService`/`Rules.Price`; SQL independently checks rounding, latest confirmed cost and the price effective when each order was placed.

Full has four locked customers and small has one, locked after historical activity. Exactly two customers have no orders. Full retains many one-time buyers and customers with five to ten purchases, plus heavier early buyers. Four old products are retired 45 days before the cutoff, preserving their purchases/orders; two suppliers are inactive at the end. Small retains one retired product and one inactive supplier.

## Timeline and seasonality

Quarters are calculated in UTC+7 before conversion to UTC, avoiding month-end arithmetic on the preceding UTC calendar day. For the supplied November 2026 example, order targets are 70 / 62 / 58 / 66 / 84 / 76 / 70 / 80 / 35 = **601**. Moving `--as-of` rotates the complete-quarter targets by calendar quarter; year-two order targets retain the approved growth. Quarter order counts use `CreatedAt`; revenue uses completed, collected COD by `CompletedAt`. Revenue growth also depends on catalog mix, launch timing and depreciation, rather than an imposed revenue multiplier.

Deterministic date weights emphasize early-month purchasing, 11/11, 12/12, the November Friday in days 23–29, and the three weeks preceding the .NET lunisolar spring-new-year reference. This is an illustrative retail season model, not a legal/public-holiday calendar. Budget/midrange purchases are weighted more heavily than premium/foldable baskets. Most orders have one line; a minority have two or three. About one fifth use a separate recipient.

Confirmation follows creation by 12 hours; dispatch follows confirmation by one day; completion/failure is five days after placement. Failed deliveries mostly return two days later, with nonempty reasons. Recent pending/confirmed/shipping orders are confined to the last few days. Eight active products complete at the start of the current month using late previous-month placements, including the first day of a month/quarter. Some intended future actions are omitted at the cutoff, leaving valid current states.

Full has 52 confirmed receipts, three drafts (one empty) and one cancelled receipt. Five distinct variants per nonempty receipt yield 275 lines. Initial eligible variants are stocked first and later low-stock variants replenished. Cost falls by 5% per elapsed calendar quarter of model age at receipt confirmation; no direct variant-price update bypasses the purchase service. Purchases and orders are selected from the chronological stock model, then existing services independently enforce availability and effects.

Full performs two intervening physical counts and eight closing counts (10 journal rows); small performs one intervening and five closing counts. Reasons are recorded, and physical counts never cut below reserved quantities. Closing full stock includes at least eight variants with `OnHand <= 5`, including three with zero stock; most variants retain 5–40 units. Full ends with 25 cart lines across 12 active customers; small has six across three. Carts do not reserve stock. Simulation sessions are removed.

## Safety, reset and failure recovery

Stop application writers while preparing or resetting the dedicated demo database. The tool's session lock serializes tool instances; it is not a maintenance mode for arbitrary web/API writers. Services retain their existing business locks and transactions.

Nonempty targets are refused unless `--reset` is present. Reset rechecks the allowlist/current migration and trusted schema, deletes only the known 18 application/session tables in FK order within a transaction, and retains the migration ledger. It does not drop databases, disable constraints, reseed identity counters, change migrations or alter tables. Never use reset on valuable data merely because a database name passes the allowlist.

Account registration/provisioning own transactions, so the entire seed is **not one atomic transaction**. Catalog creation is transactional. A failure can leave accounts or other completed stages; the next run refuses the nonempty database. Use a new dedicated database or explicitly reset the failed demo target after inspecting it. Simulation sessions are removed when catalog execution finishes or fails.

Image export preflights every collision and only adds absent files. Existing files with different content cause refusal; they are never overwritten. Images and database changes are not one distributed transaction. Use the committed 150 full-scale assets in `src/PhoneStore.Web/wwwroot/images/demo/`; `UseStaticFiles` serves them without website code changes. Tests request every committed image from a real Kestrel host and compare its bytes and SVG content type. The test adds no endpoint and needs no browser library.

## Reproducibility and independent verification

Generation builds an in-memory plan. Persistence calls existing services; direct account updates are limited to approved historical `CreatedAt`, plus explicit reset/session cleanup. Verification references persisted data and SQL, not Generation's arithmetic. No runtime service, old test, schema or UI source is modified.

The manifest uses stable natural keys rather than identity IDs. It includes lifecycle context, all catalog/profile/account/supplier data, receipts/lines, orders/lines, COD, history, movements, carts and asset hashes. Salted password hashes, rowversions, simulation sessions and migration application times are excluded. Elapsed time and the verification report are outside the canonical hash. The same seed/date/scale and generator version must produce the same normalized hash, including after reset or on another new database. This does not claim byte-identical SQL backups or deterministic password salts. Real application edits after seeding deliberately invalidate snapshot equality; verify before the later UI walkthrough or generate a fresh snapshot.

```powershell
dotnet run --project tests/PhoneStore.DemoData.Checks -c Release
# For real SQL + CLI + Kestrel proof, configure matching QA variables externally:
# PHONESTORE_TEST_CONNECTION, PHONESTORE_QA_SERVER, PHONESTORE_QA_DATABASE
dotnet run --project tests/PhoneStore.DemoData.Checks -c Release -- --integration
```

The integration runner creates a unique `PhoneStore_QA_DemoD1_<GUID>` database (the retained D1 fixture prefix), injects random test-only passwords through the environment and removes only its database. It retains D1 safety/SQL/image coverage and adds standalone verification without secrets, six deliberate QA corruption cases and real first-of-month/quarter service runs. The historical stock corruption is balanced at the end: old `DataAudit` passes while the new prefix-balance SQL correctly rejects the invalid history. Every mutation is restored. The existing demo-data CI step runs the extended small tests; no old CI step or earlier backend/MVC/Admin regression test is changed.

## Evidence and remaining D3 work

See the historical [D1 checklist](demo-data/D1_CHECKLIST.md), current [D2 checklist](demo-data/D2_CHECKLIST.md), [SQL coverage map](demo-data/D2_VERIFICATION.md), and [D2 evidence](evidence/demo-data/d2-summary.json). Build/run measurements refer to data generation and SQL acceptance, not the later application performance smoke.

Correction to the D1 report: the source quarter rows sum to **601**, exactly as the user stated. The earlier note saying 701 was an arithmetic mistake. D2 uses every original target exactly, without raising the total to 630 or altering the source documents.

D3 still owns the explicit Web `--audit-db` CLI evidence, real Admin/MVC/WinForms walkthroughs, application performance measurements and final acceptance. No deployment, merge, release or tag is included. The legacy Web `--seed-demo` command and `DemoSeeder` are unchanged.
