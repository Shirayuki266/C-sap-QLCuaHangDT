# W5 — order fulfillment, COD and invoice preview

Issue [110](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/issues/110), branch `feature/110-admin-orders-invoices`, based on develop `3dce8cec3eebcb60ea8a22ebbadde0c8cb3eb0ec`. W4 PR109 is merged; its [post-merge CI](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41353755133) succeeded before branching. Tracked worktree was clean; unrelated local `.vscode`, source-document and prototype directories were preserved. No W5 merge is authorized. Exact W5 head/CI are bound in the PR verification comment to avoid a self-referential commit SHA.

## Authority and scope

Official08 remains113 unique IDs:48 P0/59 P1/6 P2 (SHA256 `ae7962fe17cd92aacd1445b203b319c0e6d47859827a9fda6ddbadffca89ae29`). Sources: current Downloads08/09/10, actual [API compatibility](API_COMPATIBILITY.md), backend endpoints/DTOs, backend/MVC handovers and private02 section4. No backend discrepancy requiring repair was found. The old02 explanatory sentence saying return reason still requires a backend fix predates accepted T9: current code and this prompt already make it optional; no backend change is made.

This stage covers A18–A21 **16 IDs:8 P0/7 P1/1 P2**, plus file10 A09.02/A12.01 full customer-order drilldown. Conditional buttons are presentation policy, never server authority. The matrix's8 actions include customer cancellation and invoice reading: customer cancellation is tested with a real customer session and is never offered as an Admin button. All48 backend cells use an independent literal section4 oracle;11 allowed/37 denied. Full DPI/device/accessibility, reports/overview, end-to-end whole-phase flows and gates stay W6. BASE13/14/15/16 retain cumulative partial NOT RUN evidence; BASE17 is evaluated against current-head CI each step.

## Delivered behavior

The order list has one query, status, optional from/to date and paging. DateOnly values are sent as yyyy-MM-dd and interpreted by the server as UTC+7, including the entire final day; no client-side page filtering or invented code AND customer filter. Loading disables paging before page mutation. Empty/error/forbidden states are distinct.

OrderDetailForm displays recipient/contact/address, snapshot lines, separate order/COD state, server total, derived collected amount/time, return flag, rowversion and actor history. Current `OrderView` supplies `payment`, `total`, `completedAt`, `returnReceived`, `version`, `lines`, `history`; no separate paymentMethod/amount/collectedAt fields are assumed. COD presentation follows the accepted invariant: DA_THU→total/completedAt, otherwise0/no collection time.

Dedicated commands send version, immutable per-intention operationKey and optional trimmed reason. Cancel/fail require a reason beside the input; return reason is optional and limited to1000 characters. Native confirmation describes consequences. On204 the client GETs the order. On409 the form keeps the reason, explains the conflict and GETs fresh state. A failed readback leaves actions locked; a failed refresh clears actionable stale data. Unknown outcomes are reconciled by GET before any retry; an unapplied intention can retry with the same key, an applied/changed version cannot repeat that intention.

The production customer detail→customer orders event now opens the same full OrderDetailForm. The older standalone W3 read-only component remains compatible; its historical tests are preserved. The shell's foundation placeholder test skips the newly delivered Orders item and W5 tests open its actual modal list instead. No old backend test is changed.

InvoiceForm displays server title, mandatory internal/non-tax notice, recipient/line/price snapshots, server total and COD. PrintPreview uses native PrintDocument/PrintPreviewDialog with paginated text; it does not call a business write. No installed queue/driver produces explicit guidance while preserving the invoice; driver faults are caught without revealing internals. The no-printer CI test injects the printer boundary and proves Preview is not called; local real preview uses the installed PDF driver without printing. No OS printer is installed/uninstalled by tests.

## Acceptance and evidence

| Priority | W5 IDs | PASS | FAIL | BLOCKED | NOT RUN |
|---|---:|---:|---:|---:|---:|
|P0|8|8|0|0|0|
|P1|7|7|0|0|0|
|P2|1|1|0|0|0|

Two additional P1 rows A09.02/A12.01 complete their existing W3 evidence. Whole113-ID ledger: **P0 40 PASS/8 NOT RUN; P1 44 PASS/15 NOT RUN; P2 3 PASS/3 NOT RUN =87 PASS/26 NOT RUN**, no FAIL/BLOCKED/N/A. [Every ID and remaining reason](WINFORMS_CHECKLIST_RESULTS.md) is listed individually. No full WinForms gate is passed.

| ID | Priority | Status | Actual execution |
|---|---|---|---|
|WIN.A18.01|P1|PASS|SQL group4 query code/recipient/customer/phone, distinct pages, status and exact inclusive UTC+7 boundary ticks; actual filter/list/pending-pager controls.|
|WIN.A18.02|P1|PASS|Actual server DTO and all6 rendered states; snapshot lines, version/return flag, separate COD, actor timeline; real history capture.|
|WIN.A18.03|P1|PASS|Group4 list/detail/invoice snapshots unchanged; real desktop invoice/preview full data unchanged.|
|WIN.A18.04|P1|PASS|Real customer GET admin orders403; Client safe403 mapping, actual form forbidden message and no crash.|
|WIN.A19.01|P0|PASS|Independent literal oracle verifies all6 action sets, actual controls hide forbidden buttons and customer-cancel entirely; failed/returned hides receive.|
|WIN.A19.02|P0|PASS|Waiting confirm204→GET confirmed, unchanged H/R and unpaid COD, version/history advanced; real desktop checkpoint.|
|WIN.A19.03|P0|PASS|Both cancellation states in matrix: H unchanged/R−q, unpaid0. Required reason, protected confirmation dismissal sends0; real cancelled order capture.|
|WIN.A19.04|P0|PASS|Two requests gated on distinct SQL connections: customer cancel204 wins, admin confirm409 CONCURRENT_UPDATE; GET cancelled. Actual form conflict refresh/retained reason/readback-failure guard.|
|WIN.A20.01|P0|PASS|All valid actions in48-cell API matrix validate two-variant H/R, COD, history/journal counts; real desktop dispatch/complete/fail/return, correct request DTO.|
|WIN.A20.02|P0|PASS|Complete SQL payment=total, CollectedAt=CompletedAt, actor set; other states unpaid0. Six-state derived UI display and real24M COD.|
|WIN.A20.03|P0|PASS|Null/whitespace/trimmed return reason succeeds once; overlong rejected atomically. Repeat old version409/fresh returned409 changes no data; real physical-return confirmation and hidden returned button.|
|WIN.A20.04|P0|PASS|48 actual API cells:37 forbidden return409 INVALID_STATE with identical full snapshots;11 allowed correct effects/read-only. UI policy independent oracle and forced invalid form requests report server errors.|
|WIN.A20.05|P1|PASS|Actual cancel/fail missing reason400 unchanged SQL; field-adjacent form errors, retained input. Return blank remains optional.|
|WIN.A21.01|P1|PASS|Invoice allowed only confirmed/shipping/completed across matrix, server title/notice retained; actual invoice form and installed-driver native PrintPreview.|
|WIN.A21.02|P1|PASS|Matrix/group4 read-only SQL and desktop completed→invoice-read→preview-closed byte-identical full snapshots; no money/stock/state mutation.|
|WIN.A21.03|P2|PASS|STA no-installed-printer boundary test never calls Preview, preserves invoice and explains missing queue/driver; driver-exception recovery separately tested on CI.|
|WIN.A09.02|P1|PASS|W3 contact/detail evidence retained; actual production customer→orders→full order modal shows history/actions without write.|
|WIN.A12.01|P1|PASS|W3 real20+1 customer orders/empty evidence retained; new production handoff reaches full detail with snapshot/COD/version/history.|

[OrderChecks](../tests/PhoneStore.Admin.Client.Checks/OrderChecks.cs) supplies five W5 SQL scenario groups including48 cells. [OrderRaceGate](../tests/PhoneStore.Admin.Client.Checks/OrderRaceGate.cs) pauses both actual exclusive PhoneStore:write lock commands before acquisition, asserts two arrivals/two pending tasks/two connections, releases customer, observes its204 commit, then releases admin to get409. This is real contention with a controlled winner, not two sequential requests. The hook exists only in the new QA runner.

[SQL results](evidence/admin-w5/sql-results.json) contain cell outcomes, sanitized request bodies, facts and before/after hashes; [snapshot dictionary](evidence/admin-w5/sql-states.json) resolves all68 states to current order/lines/payment/history/journal, global stock/variants and global commerce counts. Denied cells have identical before/after keys. Account/password/session data is excluded. Other cases cover optional reason length, repeated receipt, genuine403, committed lost reply and an unsent request: reconcile unchanged GET first, then reuse the same intention key exactly once.

[OrderFormsChecks](../tests/PhoneStore.Admin.Tests/OrderFormsChecks.cs) supplies6 new STA groups/18 images with fake API; all31 form groups pass. Fake controls prove state/retention/confirmation/recovery, not SQL effects. [RealOrderChecks](../tests/PhoneStore.Admin.Tests/RealOrderChecks.cs) runs actual controls through loopback Kestrel into another QA database; [parent observer](../tests/PhoneStore.Admin.Client.Checks/OrderDesktopQaChecks.cs) checks9 acknowledged SQL checkpoints. [Desktop data](evidence/admin-w5/desktop-sql-results.json), [fake manifest](evidence/admin-w5/forms/results.json), [real manifest](evidence/admin-w5/real-forms/results.json) and34 actual PNGs are provided. Main forms are1280×800 at120DPI; confirmations620×330; the native maximized PrintPreview capture is1886×1266 as declared, not falsely1280×800.

Desktop aggregate stock H/R: opened18/4, confirmed18/4, dispatched16/2, completed16/2 with COD24M; invoice/preview unchanged; failed16/2, returned18/2, cancelled18/0. Journal count5→6→7→8 and history5→11 match the steps. Child controls wait at each stage until parent SQL acknowledgement. Actual child logout revokes its Admin session; fixtures remove only their own generated QA databases.

Full local results: build0 warnings/errors, unchanged322 regression, all51 Admin.Client groups (13 W1/11 W2/9 W3/13 W4/5 W5), all31 form groups (8/5/5/7/6), real9-checkpoint smoke. CI Ubuntu runs the real SQL groups plus original regression; Windows runs synthetic controls and uploads captures. Installed-driver real preview is local evidence, not attributed to CI's synthetic printer test. Current-head CI must be successful before handoff.

## Run and boundaries

Use [W1 QA configuration](WINFORMS_W1.md). Set PHONESTORE_QA_SERVER, PHONESTORE_QA_DATABASE and PHONESTORE_TEST_CONNECTION externally to an explicit QA target; fixtures generate/drop only their own random databases. Never use demo or PhoneStore_Rebuild. No credentials/connection strings are provided here.

```powershell
dotnet build PhoneStore.slnx -c Release
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release --no-build
dotnet run --project tests/PhoneStore.Admin.Tests -c Release --no-build
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release --no-build -- --desktop-orders
dotnet run --project tests/PhoneStore.Checks -c Release --no-build -- --integration
```

Full solution/desktop commands require Windows; Ubuntu uses the explicit backend/new Client commands already in CI. `--orders-only` runs just new SQL scenarios when investigating W5. Desktop normal preview requires an existing installed driver; no-printer recovery is deterministically tested independently.

The [independent full review](evidence/admin-w5/finish-review.md) opened all34 captures and returned **ship within W5 scope**, with no material findings. [Design documentation](evidence/admin-w5/design-documentation.md) preserves incumbent native AdminTheme and historical root MVC design files. Full W6 accessibility/DPI/device acceptance is unclaimed. Private report UI11 was used as local context, not copied or treated as an exact pixel comp; screenshots are QA evidence, no generated product rasters.

Diff against develop3dce8cec is empty for Core/Infrastructure/Web/old Checks, covering backend services/API/schema/migrations/MVC. Shipping changes are confined to Admin.Client/Admin; new tests are in the Admin projects. Workflow only adds W5 to existing Windows evidence-upload paths. No backend code, dependency, contract or migration change; no merge/tag/branch cleanup in W5.
