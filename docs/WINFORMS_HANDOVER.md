# WinForms Admin handover

## Điều hướng N1

See [navigation design, NAV ledger and reproducible evidence](WINFORMS_NAVIGATION.md). Admin now hosts list/detail/report/invoice/journal forms inside one MainForm. Back/Forward and clickable breadcrumbs retain local filters, page, selection and scroll but reload authoritative data on reactivation. Overview clears both directions of history; capacity is30 and removed pages are disposed. Editors, confirmations, pickers and PrintPreview remain modal. Dirty editors and inline lookup edits have Save/Discard/Stay protection; an order reason without a selected command has Discard/Stay. Expiry discards the old history, closes dialogs without replaying a write, and starts a fresh overview after login.

The pure history source stays in PhoneStore.Admin and is linked into Admin.Client.Checks for Ubuntu; PhoneStore.Admin.Client/API/backend are unchanged. The prior113 WIN IDs are retained. Updated STA harness teardown explicitly discards fixture edits; navigation integration tests assert hosted pages instead of assuming ShowDialog blocks. Native DPI150 remains an event simulation, not physical-monitor certification. Release walkthrough uses page descendants and Back/Home while retaining SQL checkpoints. The final NAV ledger and exact-head CI record govern acceptance; only propose `admin-v1.1`, do not create it.

W1–W6 deliver A01–A22 through the existing HTTP API. [API_COMPATIBILITY](API_COMPATIBILITY.md) is authoritative; the proposed contract does not rename the server routes. Backend, schema, migrations, administrative endpoints, customer MVC and the previous regression runner are unchanged. W6 is issue112, branch `feature/112-admin-reporting-handover`, based on W5 merge `f6d80c7d91d99bef06bbe0c9a2c44bc717a5fbc4`. No merge, release or tag is authorized here.

## Run

Use Windows with .NET10 SDK and Windows Desktop runtime. The API host and SQL Server are separate prerequisites; see [BACKEND_HANDOVER](BACKEND_HANDOVER.md) for host configuration/migration. The Admin applications have no SQL connection or backend assembly dependency.

```powershell
dotnet build src/PhoneStore.Admin/PhoneStore.Admin.csproj -c Release
dotnet run --project src/PhoneStore.Admin -c Release --no-build
```

In **Cấu hình máy chủ**, enter the API origin and optional expected environment/fingerprint. HTTP is permitted only on loopback; remote origins require HTTPS with normal certificate validation. Readiness is checked before credentials are sent. Use an existing active ADMIN account; there is no embedded/default password.

Public user settings live in `%LOCALAPPDATA%/PhoneStore.Admin/settings.json`: `BaseUrl`, `ExpectedEnvironment`, `ExpectedFingerprint` only. `stock-preferences.json` stores the explicit low-stock threshold. Passwords, cookies and CSRF tokens are held in memory, never settings/log files. Every write obtains a fresh CSRF token with the session cookie. Logout/expiry/password change return to login; an uncertain write requires authoritative reconciliation before explicit retry.

## Run checks

Set `PHONESTORE_TEST_CONNECTION`, `PHONESTORE_QA_SERVER`, and `PHONESTORE_QA_DATABASE` externally for a dedicated SQL QA target. The configured database must start with `PhoneStore_QA_` and server/database must match the explicit guard variables. Do not print or commit the configured connection. The fixture creates/removes only its own random `PhoneStore_QA_Admin_<GUID>` database. Never target demo, production or `PhoneStore_Rebuild`.

```powershell
# Cross-platform HTTP + real SQL checks, W1–W6; executable, not dotnet test.
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release
# W6 only, still real backend/SQL.
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release -- --reports-only
# Previous backend/MVC regression, unchanged.
dotnet run --project tests/PhoneStore.Checks -c Release -- --integration

# Windows STA forms with explicitly fake APIs.
dotnet build tests/PhoneStore.Admin.Tests -c Release
dotnet run --project tests/PhoneStore.Admin.Tests -c Release --no-build
dotnet run --project tests/PhoneStore.Admin.Tests -c Release --no-build -- --dpi-closeout

# Interactive Windows desktop: build the real app first, then isolated QA walkthrough.
dotnet build src/PhoneStore.Admin -c Release
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release -- --walkthrough-release
```

The last command launches the actual Release `.exe` and a loopback QA host, controls native windows through Win32/UIAutomation, and observes SQL at19 acknowledged checkpoints. Every Admin write goes through the real application; customer checkout uses the real customer API as permitted by official08. It backs up/restores public application settings in memory and closes its own processes. An interactive desktop is required; do not operate the application concurrently. [Walkthrough manifest](evidence/admin-w6/release-walkthrough/manifest.json) binds the captured executable/DLL hashes. This is distinct from the fake-API form runner.

## Delivered behavior and evidence

- Accounts/session/readiness: W1; products/lookups: W2; customers/suppliers: W3; purchase/stock/counting: W4; orders/invoices: W5; reports/overview/integrated flows/accessibility: [W6](WINFORMS_W6.md). [Per-ID ledger](WINFORMS_CHECKLIST_RESULTS.md) retains every official priority.
- Revenue/bestsellers require both UTC+7 dates, including the final day. Stock sends no period. One `StoreReport` supplies table/chart; monthly grouping sums daily rows only, while the overall total remains server-authoritative. Bestsellers remain per variant. Report display pages20 rows without inventing server filters.
- Overview loads six metrics and seven-day daily revenue separately, with timestamps. Unreceived returns scan every failed-order page; a later-page error produces an error, never a partial count. Threshold/date changes invalidate the corresponding old metric. Waiting/low-stock actions open filtered lists. These calls are not an atomic snapshot.
- ScottPlot.WinForms5.1.59 and5.0.56 failed a real build with NU1701 from legacy OpenTK/framework fallback. Design decision6's permitted GDI+ fallback is used without warning suppression or new application dependencies; [restore evidence](evidence/admin-w6/chart-restore.md). This does **not** claim ScottPlot ran successfully.
- Local checks: unchanged322 regression;63 real Admin.Client groups including12 W6 groups and the retained48-cell order matrix;36 STA form groups;56 native DPI form/tab captures with bounds, labels, rendered date/chrome and label/button text checks. [W6 SQL](evidence/admin-w6/sql-results.json), [Release SQL](evidence/admin-w6/release-walkthrough/sql-results.json), [DPI results](evidence/admin-w6/dpi/results.json), [form results](evidence/admin-w6/forms/results.json).

## Accepted differences

|Subject|Accepted behavior / ownership|
|---|---|
|Actual API names/DTOs/errors|Use API_COMPATIBILITY and official08 R1–R4. No backend rename to match proposal01. Requests use separate allowlisted DTOs, not response serialization.|
|Duplicate profile/account values|Backend combined validation appears at form level; entered non-password data is retained. No field guessing by parsing message text.|
|Backend decisions inherited|T9 return reason optional; repeated unchanged inventory count returns INVALID_INPUT; empty purchase drafts can be saved/cancelled. DEC.01/03/06 and A03.02/FE.04 remain the accepted backend behavior documented in BACKEND_HANDOVER.|
|MVC boundary|The accepted IStoreService interface/adapter and product–variant read-only pair check belong to MVC. Admin remains HTTP-only and does not reference them.|
|WIN.GATE.05|User accepted an **agent-assisted UI walkthrough** using the actual built Release application, real isolated QA backend, per-step pictures and SQL for FLOW01/02. The user will also run the application manually. This is not claimed as a human-operated walkthrough.|
|DPI proof|125% is the local machine's actual120DPI.150% is simulated by native WM_DPICHANGED144 on real WinForms controls; it changes layout/font, not bitmap dimensions. The actual desktop remains120DPI. CI may use96DPI baseline and records its actual value, never calls it125%.|
|Session loss with an unsaved purchase|A clear native warning states that unsaved content will be lost, then returns to login. No old-session write is automatically replayed or partly saved. Official FLOW04 permits this warning alternative.|

## Limits and operation

The backend limitations remain: seeder requires an explicit command, is for QA/dev only and has no default password; it is not yet locked by database name. There is no automatic backup schedule, production deployment or old-database upgrade path. See BACKEND_HANDOVER; this phase does not resolve those limits.

Report endpoints return an unpaged complete report; display paging is client-side. Overview totals may differ across concurrent loads; timestamps are visible. Native grids retain horizontal/vertical scroll and cell accessibility/tooltips; long text is available without changing server data. Main navigation is1280×800 minimum; some auxiliary dialogs retain their smaller native default, and all required form captures are additionally tested at1280×800. These screenshots are QA records, not private Word/prototype uploads.

150% DPI has **not** been certified by changing Windows display settings or testing another physical monitor. The accepted native-event simulation is identified in every manifest. DPI checks cover the forms/tabs, not the operating-system print-preview dialog. Real installed-driver preview and no-printer recovery evidence from [W5](WINFORMS_W5.md) remain applicable. No printer output or production environment was exercised. User manual follow-up remains planned after the accepted AI-operated walkthrough.

## Acceptance and CI

The official08 source SHA256 is `ae7962fe17cd92aacd1445b203b319c0e6d47859827a9fda6ddbadffca89ae29`:113 IDs,48 P0 /59 P1 /6 P2. Final ledger, subject to the exact-head verification record below:

|Priority|Total|PASS|FAIL|BLOCKED|NOT RUN|N/A|
|---|---:|---:|---:|---:|---:|---:|
|P0|48|48|0|0|0|0|
|P1|59|59|0|0|0|0|
|P2|6|6|0|0|0|0|

No remaining NOT RUN IDs: WIN.BASE.17/GATE.03 are closed by required Ubuntu/Windows SUCCESS and WIN.GATE.02 by all48 P0 PASS. No remaining implementation/QA case is silently promoted on an old CI run. [Workflow](../.github/workflows/backend.yml) keeps the Ubuntu backend/MVC job, adds W6 to the existing Admin.Client runner, and runs Windows forms/DPI separately. [Branch CI listing](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/workflows/backend.yml?query=branch%3Afeature%2F112-admin-reporting-handover) is discovery only: the [final verification record](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/113#issuecomment-6569365089) binds the exact final SHA and immutable successful run. It must say SUCCESS for the current head; a later commit invalidates that assertion. Source implementation CI at a223fb6 succeeded before this ledger amendment; final-head CI is checked again. No merge is performed.
