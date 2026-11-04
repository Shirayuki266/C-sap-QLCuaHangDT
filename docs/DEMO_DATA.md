# Demo data — creation, verification and D3 acceptance

Current closeout evidence uses `--seed 2611 --as-of 2026-11-04`. The [closeout decisions](demo-data/RELEASE_CLOSEOUT.md) require a76-record exact-head ledger:75 executed PASS records and one separately counted `ACCEPTED_EXCEPTION` for CLEAN.FILES.04. The correction PR publishes that ledger after clean-checkout tests and exact-head CI; the committed summary labels its pre-commit development evidence explicitly. No merge or tag is authorized in this PR phase; `data-v1.0` also requires the user's later literal “DUYỆT PHÁT HÀNH”.

The current requested catalog is **real manufacturers/models and licensed real photographs**, selected with `--catalog real`. This follows the user's instruction after D3 and supersedes the earlier fictional option A for the new preview. The separate tool now defaults to `real` and rejects `--catalog fictional`; archived D1–D3 runs are historical records, not commands for the current tool. The unchanged legacy Web `--seed-demo` is a user-approved compatibility exception and must not be used for the current real dataset. See [release closeout decisions](demo-data/RELEASE_CLOSEOUT.md). See [image attribution/license register](demo-data/IMAGE_CREDITS.md) and [approved price assumptions](demo-data/PRICE_ASSUMPTIONS.md). See [REAL_CATALOG.md](demo-data/REAL_CATALOG.md) for provenance and synchronization evidence.

The separate `net10.0` executable creates eight complete quarters and the current partial quarter of purchases, COD orders, history, stock and carts. Full uses the original quarter targets, totaling **601 orders**, plus 56 purchases. All business effects go through existing services with a simulated `TimeProvider`. D3 verifies the resulting full database through the unchanged applications; the evidence below records actual browser and Release WinForms runs.

The historical approved sources are preserved with an explicit current-catalog override in [file 11](demo-data/sources/11_DAC_TA_DU_LIEU_DEMO.md) and [file 12](demo-data/sources/12_PROMPT_DU_LIEU_DEMO.md), including section 2b. Their original option A uses fictional brand/model names, original Vietnamese descriptions and programmatically drawn SVG illustrations. The new explicit `real` profile uses frozen manufacturer facts and real product images instead. People/contact/address records remain fictional; email is `example.com` or NULL. Phone-format validity does not assert that a generated number is allocated or contactable. Orders, costs, prices and stock are illustrative simulated transactions rather than actual manufacturer sales or current retail quotes.

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
        --seed-demo-large --confirm --catalog real --seed 2611 --as-of 2026-11-04 --scale full `
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
| `--catalog real` | Current requested catalog: genuine models/specifications and locally packaged licensed photographs; requires `--as-of` from 2026-10-01. The2025 nubia model has a separate conservative shop-introduction lower bound of2026-01-01 |
| `--catalog fictional` | Rejected by the current separate tool; reproduce historical runs only at their archived commits |
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

## Environment variables

Configure these externally. This table intentionally contains names and purposes only, without values or credentials.

| Name | Purpose |
|---|---|
| `ConnectionStrings__Store` | Explicit dedicated demo/QA SQL target for migration, generation, verification and the real Web host |
| `PHONESTORE_DEMO_ADMIN_PASSWORD` | Required administrator password during generation and authenticated D3 smoke |
| `PHONESTORE_DEMO_CUSTOMER_PASSWORD` | Required customer password during generation and authenticated D3 smoke |
| `ASPNETCORE_ENVIRONMENT`, `DOTNET_ENVIRONMENT` | Environment selection for application commands; the D3 child host selects its isolated test environment |
| `PHONESTORE_TEST_CONNECTION`, `PHONESTORE_QA_SERVER`, `PHONESTORE_QA_DATABASE` | Matching allowlisted QA configuration for existing regression and demo-data integration runners |
| `PHONESTORE_PLAYWRIGHT_MODULE` | Optional path to the locally installed Playwright module |
| `PHONESTORE_DEMO_SMOKE_OUTPUT` | Optional destination for D3 sanitized results and screenshots |

The D3 parent supplies `PHONESTORE_DEMO_CONTEXT`, `PHONESTORE_WALK_APP`, `PHONESTORE_WALK_OUTPUT`, `PHONESTORE_WALK_SIGNALS`, `PHONESTORE_ADMIN_QA_URL`, `PHONESTORE_ADMIN_QA_USERNAME` and `PHONESTORE_ADMIN_QA_PASSWORD` to its child observers. They are implementation inputs, not saved configuration. Never capture environment dumps, login HAR files, cookie jars or browser storage states as evidence.

## D3 real application smoke and performance

Use an interactive Windows desktop, .NET 10, SQL Server, Node and the same local Playwright/Chromium installation used by the existing MVC browser checks. First run the existing `--mvc-catalog-browser` check if that installation is missing. Build the solution in Release. Create and migrate a fresh dedicated database whose name starts with `PhoneStore_Demo_D3_`; seed it with **full** scale and today's default `--as-of`. Keep the two passwords in the parent environment for the authenticated smoke, then clear them. Do not use the databases owned by earlier test suites.

```powershell
dotnet build PhoneStore.slnx -c Release
# Connection and password variables must already be supplied externally.
# Run the migration and full seed commands above, with today's default --as-of.
dotnet run --no-build -c Release --project tests/PhoneStore.DemoData.Checks -- --demo-smoke
# Then run the read-only --verify-demo-data command above against the seed manifest.
```

`--demo-smoke` belongs to the new demo-data test executable, not to the shipping application or seeder. It requires 601 orders, the current schema, no existing sessions and a dedicated D3 target. It starts the real Release Web DLL on loopback, calls the existing `--audit-db` before and after, and checks authenticated ADMIN paging, all nine quarterly revenues against independent SQL, bestsellers, low/zero stock, a completed-order invoice and all 150 image URLs. Chromium visits all 46 active products, verifies their descriptions/specifications/images, and signs in through the MVC form to view a customer's own history and detail.

The native observer launches the actual Release `PhoneStore.Admin.exe`; it does not inject an API or construct test forms. Standard Win32 button input and UI Automation inspect dashboard values, two order pages, a completed order/invoice, revenue/bestseller/stock reports and the stock list. Application-only `PrintWindow` captures avoid photographing unrelated desktop windows. The helper temporarily points public Admin server/stock settings at this demo, restores their original bytes afterwards, and refuses to disturb an already running Admin process. Normal UI logout removes the real sessions. Independent normalized business hashes before/after and SQL verification establish that these reads did not alter the seeded business data.

Recorded on the local Windows/SQL machine with 601 orders, 813 order lines, 125 stock variants and 150 SVGs. HTTP timings include response-body parsing, browser timings include navigation through two animation frames, and WinForms timings include button input and observation of rendered server data. Five extra warm HTTP list/report pairs and five warm browser catalog samples are retained in JSON. Limits were selected before measurement: HTTP < 2 seconds; browser/native < 5 seconds. These are local acceptance measurements, not concurrent-load or remote-network benchmarks.

| Actual operation | Recorded elapsed time |
|---|---:|
| ADMIN HTTP first / second order page | 333.7 / 62.8 ms |
| ADMIN HTTP quarterly revenue, maximum of nine | 121.3 ms |
| MVC catalog first page, 46 active products across four pages | 291.7 ms |
| WinForms first / second order page | 697.0 / 307.1 ms |
| WinForms revenue / bestsellers / stock report | 186.9 / 186.7 / 181.9 ms |
| WinForms stock list | 405.6 ms |

The real dashboard shows zero revenue **today** because the deterministic snapshot stops at the beginning of `--as-of`; its 30-day revenue and current-month bestsellers are populated. The eight displayed homepage bestsellers are the application's display limit; SQL finds twelve eligible products in this recorded month. The physical low-stock metric uses `OnHand <= 5`, while the UI uses available stock `OnHand - Reserved <= 5`; both happen to be eight in this snapshot, including three physically empty variants. Four retired products remain in historical orders and inventory but are absent from the 46-product public catalog. Recreate with the current date before a later demonstration, otherwise time-based homepage/report windows naturally move beyond this historical snapshot.

## Evidence and final gate

See the historical [D1 checklist](demo-data/D1_CHECKLIST.md), [D2 checklist](demo-data/D2_CHECKLIST.md), [SQL coverage map](demo-data/D2_VERIFICATION.md), [D2 evidence](evidence/demo-data/d2-summary.json), and final [D3 checklist](demo-data/D3_CHECKLIST.md). [summary.json](evidence/demo-data/summary.json) links the full SQL report, audit results, per-operation measurements, real screenshots and regression results. The D1/D2 documents remain historical records.

Correction to the D1 report: the source quarter rows sum to **601**, exactly as the user stated. The earlier note saying 701 was an arithmetic mistake. D2 uses every original target exactly, without raising the total to 630 or altering the source documents.

D3 reran the unchanged backend/SQL suites, all five previous MVC browser suites, Admin Client W1–W6 checks, native forms/DPI checks, all five real desktop integration modes, the earlier actual Release walkthrough, and the 24-group D1/D2 demo-data integration suite. That suite again proved nonzero verification exits for six deliberate QA corruptions, restored each mutation, and exercised three month/quarter boundary dates. The fresh full D3 seed and its post-UI verification retain the D2 normalized manifest hash.

The repository evidence is an explicitly **pre-CI snapshot**: 42 PASS, with `DATA.GATE.01` NOT RUN until remote CI is green on the final PR head. The PR's final verification supplies that exact head SHA, both CI results/URL and the final 43-ID counts; it supersedes this single pre-CI status without pretending a commit can embed its own future CI result. A failed or superseded head cannot pass the gate. Scope follows file 11's precise gate and file 12 section 2b decision 8 (new tool/tests, docs, solution registration, additive CI and new static images), which clarify the D3 prompt's abbreviated scope sentence. This D3 diff changes only demo-data tests and documentation/evidence; no runtime source, old regression, schema, migration, API, MVC, WinForms or CI step changes. No deployment, merge, release or tag is included. The legacy Web `--seed-demo` and `DemoSeeder` are unchanged.
