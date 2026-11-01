# WinForms W3 — customers and suppliers

Base: merged W2 PR #105 at `cc4ab8aa4eed4bd752b60a449b189eb8a72e921e`, [develop CI SUCCESS](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41305809128). Issue #106; branch `feature/106-admin-contacts`. The latest supplied08/09 `(2)` remain authoritative: 113 IDs, 48 P0 / 59 P1 / 6 P2. W3 covers11 IDs, 2 P0 / 9 P1. No automatic merge.

Source availability: the supplied08/09 were read at W3 start, but their Downloads paths disappeared later in this task. Final count validation uses the accepted W2 ledger's complete113 ID/priority/criterion snapshot and retained original source SHA, checked against the same W3 scope read at start. No replacement source file or priority labels are invented.

## Delivered behavior

MainForm opens customer and supplier management. Customer list uses actual query/active/page/pageSize, default20/max100; detail is an ADMIN customer projection with no password/hash/role fields. ProfileEditor exposes only fullName, phone, email and address and PUTs that dedicated request; success reads the customer again. Duplicate/validation errors leave the form intact. Server error codes provide form-level messages, not a promised field-error array; no message-string parsing guesses a duplicate field.

Lock/unlock is a separate confirmed POST `/api/admin/customers/{id}/state` with active and expectedActive from the displayed snapshot. A changed state returns CONCURRENT_UPDATE409 with reload feedback; the client does not silently replace the expectation and overwrite. Lock explains that every customer session is revoked; unlock explains that old sessions stay invalid. ADMIN accounts cannot be selected from the server's customer list; reading/locking an ADMIN ID returns404 and the detail view hides its state action on an invalid target.

Customer orders use the real customer-scoped paged route and open an actual read-only order detail GET. Typed OrderHandoff can replace that default later. This is the user's allowed W3 minimal detail, not the full W5 timeline/fulfillment/invoice interface. The Word UI11 reference depicts the full order view; its later actions are explicitly outside this step.

Suppliers have query/state/paging, create and full-field update (code/name/phone/address/active), without delete. There is no supplier-by-id endpoint: POST201 `{id}` and PUT204 are followed by bounded list/query readback. Exact ID is resolved from the code query; if another editor has changed that code, recovery scans bounded list pages for the known ID rather than inventing a route. Duplicate code errors retain the draft. Uncertain writes require authoritative reconciliation; successful writes with failed readback retain the known ID and block a repeated write until a successful read.

SupplierPickerForm is a working reusable selector, requesting active=true across all bounded pages. It refuses an invalid inactive response, does not pretend to apply an unsupported backend filter, and returns only an active SupplierRecord. W3 proves this component. Its purchase-editor integration and creating/saving a receipt remain W4; selecting alone has no persistence/inventory effect. WIN.A05.04 stays NOT RUN. WIN.BASE.15 stays partial: customer confirmation is now wired, but purchase/stock/order confirmations remain W4/W5.

All form constructors receive IAdminApi. HTTP/cookies/CSRF/error/formatting remain in Admin.Client; contact DTOs are independent of backend assemblies. No shipping SQL/EF/Web reference or secret configuration is introduced. Existing Core, Infrastructure, Web, migrations, API and previous PhoneStore.Checks have empty diff. Existing Ubuntu steps are unchanged; the client runner discovers W3 checks, and the Windows upload includes its new capture directory.

## Run and evidence

Configure the existing external matching PHONESTORE_QA_SERVER, PHONESTORE_QA_DATABASE and PHONESTORE_TEST_CONNECTION as described in README/W1. The guard requires a QA target; each check fixture creates and drops only its own random PhoneStore_QA_Admin_ database. No demo/live target is used. Generated credentials stay in memory and temporary child-process environment, not arguments, settings or evidence.

```powershell
dotnet build PhoneStore.slnx -c Release
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release --no-build
dotnet run --project tests/PhoneStore.Admin.Tests -c Release --no-build
# Windows only, after building both runners:
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release --no-build -- --desktop-contacts
dotnet run --project src/PhoneStore.Admin -c Release --no-build
```

Local Release solution build has zero warnings/errors. Client checks retain13 W1 and11 W2 groups and add9 W3 real HTTP/SQL groups. Windows checks retain8 W1 and5 W2 groups and add5 W3 STA fake-API groups (18 total). A separate real WinForms -> Kestrel -> SQL smoke saves a profile, creates/reads a supplier and renders the customer/supplier lists and active picker; SQL verifies the allowed changes, no order/inventory/payment/journal mutation and logout revocation. Exact final-head Ubuntu/Windows CI and unchanged322 backend/MVC regression are bound in the PR verification comment; historical CI is not proof for a new head.

- [Per-ID ledger](WINFORMS_CHECKLIST_RESULTS.md) retains all113 official priorities and reasons for every NOT RUN row; [machine-readable W3 ledger](evidence/admin-w3/checklist-results.json).
- [SQL/HTTP results](evidence/admin-w3/sql-results.json) include safe projections, request/status traces and unchanged rejection digests. Account hashes, passwords, cookies, session IDs and CSRF tokens are not exported. The test privately compares the existing account hash to prove a profile update did not alter credentials.
- The fixture seeds a positive QA stock balance and creates21 orders through actual customer cart/checkout HTTP APIs for page20+1 coverage. [Nested profileSnapshots](evidence/admin-w3/sql-results.json) isolate the profile update: recipient/phone/address snapshots, totals, payments, history, H50/R21 and journal remain byte-for-byte equal. Do not attribute fixture setup/reservation to editing the profile.
- Session proof uses two separately authenticated real customer cookie clients: both session GETs become401 after lock; a cart write with an old session is401; unlock recreates no session and both old clients remain401; only a fresh login succeeds200. A competing authenticated admin session changes the state before the stale expectedActive command, whose409 leaves SQL unchanged.
- Supplier checks cover create/update/stop/reopen, duplicate create/update rollback, query/paging, no invented by-id/delete request and an active-only picker crossing100 rows (103 active, inactive omitted). These extra rows are explicit disposable QA fixtures, not demo seeding.
- Transport probes deliberately lose a committed profile/state/supplier response or inject a readback503. They prove write guards and GET/list reconciliation, not a claim of a live DB outage. Request/status traces retain the real backend read status before that injected client response. Stripped profile-PUT CSRF is rejected400 without SQL changes; every normal write gets a fresh token.
- [Form results](evidence/admin-w3/form-results.json) label11 captures fakeApi=true; [real QA capture metadata](evidence/admin-w3/real-form-results.json) labels5 separately. [Desktop SQL](evidence/admin-w3/desktop-sql-results.json) corroborates that actual UI smoke. [Check output](evidence/admin-w3/checks.txt), scoped visual review/verdict and native design documentation are retained beside the images. The sole copy finding was resolved; the verdict is not a new whole-app/WCAG certification.

No API discrepancy requiring a backend repair was found. Full order operations/invoices, purchase integration, wider DPI/accessibility matrix and phase gates remain their later stages. The private Word/prototype and raw logs remain untracked.
