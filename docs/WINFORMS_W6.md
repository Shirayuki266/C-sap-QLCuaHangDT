# W6 — reporting, overview and WinForms closeout

Issue112; `feature/112-admin-reporting-handover`; base W5 merge `f6d80c7d91d99bef06bbe0c9a2c44bc717a5fbc4`. W5 PR111 was merged and [develop CI41356865712](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41356865712) succeeded before branching. The starting worktree had no tracked edits; unrelated `.vscode/`, private `TAI_LIEU_BACKEND/` and optionA archive remain untracked. No backend/MVC/old-test changes, merge, tag or branch cleanup.

Official08/09/10 hashes and the complete113-ID priority mapping are in the [ledger](WINFORMS_CHECKLIST_RESULTS.md).26 NOT RUN rows were reported individually before implementation:8 P0,15 P1,3 P2. W6 adds report/overview surfaces and closes cumulative BASE/flow/UI evidence; it does not change the meaning of those priorities.

## Evidence by ID

|ID|Priority|Result|Evidence|
|---|---|---|---|
|WIN.A22.01|P0|PASS|Real report SQL, UTC+7 start/end boundary fixture: previous tick/next-day boundary excluded, first/last tick included; bad/missing dates blocked before HTTP; field error retained.|
|WIN.FLOW.01|P0|PASS|Actual Release UI creates phone/cost/profit, purchase10 at8M → server9.6M, checkout2 reserves2, confirm/dispatch/complete, report19.2M.19-stage observer and step pictures below.|
|WIN.FLOW.02|P0|PASS|Release UI failed-delivery/receive/cancel flow; H6→8 once; confirmed cancellation R2→0, H8 unchanged, COD uncollected. Client repeat rejection leaves SQL unchanged.|
|WIN.FLOW.03|P0|PASS|Real gated requests from two distinct Admin accounts and SQL connections; one succeeds, loser409 CONCURRENT_UPDATE; authoritative refresh, no overwrite. W5 form conflict feedback remains covered.|
|WIN.GATE.01|P0|PASS|A01–A22 screens and UI/UC links now delivered across W1–W6, with retained previous proofs plus W6 actual UI/report evidence.|
|WIN.GATE.02|P0|PASS|All48 official P0 PASS with executable/SQL/UI evidence and required correct-head CI verification.|
|WIN.GATE.03|P0|PASS|Ubuntu and Windows SUCCESS on the exact head bound in the final PR verification record; old runs do not certify a later commit.|
|WIN.GATE.04|P0|PASS|Diff against base is empty for Core/Infrastructure/Web/old Checks; no schema/migration/API/customer MVC change.|
|WIN.BASE.13|P1|PASS|Every module connected; navigation/status/minimum-window proofs plus actual MainForm/dashboard and56 form/tab DPI captures.|
|WIN.BASE.14|P1|PASS|Cumulative W1–W5 async/busy/empty/error cases retained; new report/overview errors distinguish empty0 from actual errors, metrics load separately. Source search finds no blocking .Result/.Wait calls.|
|WIN.BASE.15|P1|PASS|Cumulative dialogs plus new full dismissal suite: product, brand, category, customer lock/unlock, purchase confirm, count, cancel/failure/return; zero writes when dismissed.|
|WIN.BASE.16|P1|PASS|Retained writing-form error/retention tests plus W6 report dates and errors; password fields deliberately clear; no field guessing from combined error strings.|
|WIN.A22.02|P1|PASS|Actual variant bestsellers and stock; stock request has kind only and ignores invalid/irrelevant dates.|
|WIN.A22.03|P1|PASS|Same AdminReport source instance for table/chart; monthly sums daily; server total34.6M from two variants without double counting; variant IDs remain separate.|
|WIN.A22.04|P1|PASS|Actual empty report0, forced real SQL stock-read error500 with safe trace; error never becomes stock0. Fake form error clears previous totals/rows.|
|WIN.A22.05|P1|PASS|Server status totals, period/today and7 daily points;105 additional failed orders spanning pages, returned flags excluded; injected page2 failure yields error, no partial94; other tasks still succeed.|
|WIN.A22.06|P1|PASS|Production dashboard button opens waiting-order filter or explicit-threshold stock filter; changed filter invalidates old metric.|
|WIN.FLOW.04|P1|PASS|Actual Release draft quantity7, controlled QA session expiry, save401, clear unsaved-draft warning then login; no extra purchase/order/stock/history/payment/journal effect.|
|WIN.FLOW.05|P1|PASS|Real absolute count lowers variant to3, R0, Available3; explicit low-stock3 query/metric includes it.|
|WIN.UI.01|P1|PASS|56 actual STA form/tab renders at local125% and native-event150%, UI01/02/08–12 and auxiliary forms, all1280×800;21 additional Release walkthrough captures.|
|WIN.UI.02|P1|PASS|Automatic control bounds and label/button/date-text+chrome checks, grids scroll; independent review found date clipping, fixed and recaptured successfully.|
|WIN.GATE.05|P1|PASS|User-accepted agent-assisted UI walkthrough: actual built Release app, isolated real backend/SQL,19 checkpoints and21 pictures. No form harness or fake API. User will manually run additionally.|
|WIN.GATE.06|P1|PASS|Official priority count, per-ID ledger, evidence, accepted differences/limits and WINFORMS_HANDOVER provided.|
|WIN.A22.07|P2|PASS|Report20-row pages from cached result, selected bar/daily value label, monthly grouping without refetch; table/chart use identical page rows.|
|WIN.UI.03|P2|PASS|Native Tab username→password, Enter/Cancel defaults, Escape closes modal without a write, CtrlS saves exactly once; geometry-based tab order, named inputs and chart arrow keys.|
|WIN.UI.04|P2|PASS|Vietnamese text states/empty/loading/errors remain alongside color; labels show order state and COD separately, selected chart values are textual.|

## SQL and actual Release walkthrough

[ReportChecks](../tests/PhoneStore.Admin.Client.Checks/ReportChecks.cs) adds12 groups to the real backend runner. [SQL before/after](evidence/admin-w6/sql-results.json) records H/R/Available, orders/payments/history/journal per group; HTTP path trace excludes credentials. The105-failed-order pagination fixture is built with actual customer/admin operations. Only timestamp boundary fixtures and deliberate QA expiry use direct SQL updates; read-error injection and race gates are test-host-only, never shipping hooks.

[Release coordinator](../tests/PhoneStore.Admin.Client.Checks/ReleaseWalkthroughChecks.cs) launches the built application, not `PhoneStore.Admin.Tests`; [native driver](../tests/PhoneStore.Admin.Tests/Walkthrough.Release.ps1) finds visible controls, enters fields, edits grids and invokes real buttons/dialogs. Precondition setup creates only QA accounts/lookups/NCC; product and stock are not preseeded. Every Admin business write is made through the actual app. Customer checkout uses the actual customer API as official08 permits. Images are captured after SQL acknowledgement and UI readback readiness; customer checkpoints refresh the real Admin dashboard.

[Manifest](evidence/admin-w6/release-walkthrough/manifest.json), [all21 picture names](evidence/admin-w6/release-walkthrough/captures.json), and [19 SQL checkpoints](evidence/admin-w6/release-walkthrough/sql-results.json) retain before/after data. FLOW01: H0/R0 → receipt H10/R0 → checkout H10/R2 → dispatch H8/R0 → completed COD19.2M/report19.2M. FLOW02: second dispatch H6/R0 → failure unchanged → receive H8/R0; third confirmed order R2 → cancel R0/H8. Every snapshot includes payments, history and stock journal; no password/hash/session token is exported. Expired unsaved draft warns and returns to login, purchase count remains1. The acceptance label is **agent-assisted UI walkthrough**, not a human-operated test.

## Native UI, architecture and validation

Reports use GDI+ under decision6 after genuine pinned ScottPlot restore/build failures; [details and logs](evidence/admin-w6/chart-restore.md). Charts have native mouse/arrow selection and textual values; no generated imagery. [Form checks](../tests/PhoneStore.Admin.Tests/ReportFormsChecks.cs), [cumulative BASE/keyboard checks](../tests/PhoneStore.Admin.Tests/BaseCloseoutChecks.cs), [DPI checks](../tests/PhoneStore.Admin.Tests/DpiCloseoutChecks.cs), [DPI manifest](evidence/admin-w6/dpi/results.json). Fake APIs are labelled; they prove controls, not SQL.125% is actual desktop120DPI;150% sends native WM_DPICHANGED144, records the delivered event/font change, keeps1280×800 and checks layout/text without image enlargement. CI records its actual desktop baseline separately. OS print-preview DPI is outside this simulation; W5 installed/no-printer proofs remain valid.

The [fresh finish review](evidence/admin-w6/finish-review.md) opened all65 initial captures and found two material issues. [Verdict](evidence/admin-w6/finish-verdict.md): **ship for both scored fixes**, dates fully readable and duplicate login eyebrow removed. It also checked the populated invoice recaptures. This limited verdict does not certify API/SQL/CI. [Design documentation](evidence/admin-w6/design-documentation.md) preserves the native incumbent and historical root MVC design files. [Release evidence review](evidence/admin-w6/release-evidence-review.md) separately validates walkthrough pictures and SQL-visible facts, with scope stated.

Local results:0 build warnings/errors,322 unchanged regression,63 Admin.Client groups (13/11/9/13/5/12),36 form groups,56 DPI captures,19 Release SQL checkpoints/21 pictures. All113 official rows are closed, including retained WIN.BASE.17 revalidated for this stage. The [final verification record](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/113#issuecomment-6569365089) must confirm both required jobs SUCCESS on the actual final head; initial source CI a223fb6 succeeded and the documentation amendment is checked again. [Handover](WINFORMS_HANDOVER.md) carries current counts, commands, differences and limits. No W6 merge is performed.
