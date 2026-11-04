# W4 — purchases and inventory

Issue [108](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/issues/108), branch `feature/108-admin-purchases-stock`. W3 PR107 was squash-merged at `bfbf2f652a4fc91b40c7079e341bc187096a668a`; its [post-merge CI](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41325966422) succeeded before this branch started. No W4 merge is authorized. Exact final-head CI and PR are recorded in the PR verification comment, avoiding a self-referential commit SHA in this file.

## Sources and acceptance boundaries

Downloads `08_CHECKLIST_WINFORMS_ADMIN.md` SHA256 `ae7962fe17cd92aacd1445b203b319c0e6d47859827a9fda6ddbadffca89ae29` has **113 unique checklist rows:48 P0/59 P1/6 P2**. The restored file is the same official08 used previously. Downloads09 SHA256 `3d4fc3bb450801ad00de1c06828dac4759ee4c0c0eb45248e71f9fe9f4428fad` adds the cross-stage acceptance rule. The referenced `10_SO_THEO_DOI_O_CHO_WINFORMS.md` was subsequently supplied in Downloads on2026-11-02, SHA256 `30823d9a3de99fd3f03639ed0eaca07160c8da9ea8121a2f55dff370f2847ccb`. Its24 represented IDs have been reconciled. In addition to BASE13/14/15/16 and A12.01 already held, **A09.02 returns to partial NOT RUN until full order detail in W5**. Prior W3 read-only evidence is retained. A05.04 finishes in W4; BASE17 is evaluated at every step against exact-head successful CI. A22.06, all FLOW/UI/GATE rows remain NOT RUN as before. Cumulative counts are now P0:32 PASS/16 NOT RUN; P1:35 PASS/24 NOT RUN; P2:2 PASS/4 NOT RUN, total69 PASS/44 NOT RUN. No FAIL/BLOCKED/N/A; no whole-phase acceptance.

Actual contract authority remains [API compatibility](API_COMPATIBILITY.md), backend code and existing handovers/decisions/state matrix. No backend contract changes were needed. The previously supplied private Word file is no longer at its named Downloads path; no fresh pixel comparison to its UI09/UI10 images is claimed. Native styling extends the already accepted AdminTheme/Option A. Full DPI/accessibility/device coverage and remaining order/report confirmations are deferred to W5/W6.

## Delivered behavior

Purchases support query/status/page only; active supplier selection; unique editable variant lines; integer positive quantity and positive two-decimal cost; empty drafts; versioned replacement; confirmed/cancelled read-only state. Metadata and totals come from GET. Confirmation states the saved server total, total quantity and stock/price consequences. Unsaved edits must be saved before confirming. Both confirm and cancel send dedicated `{version,operationKey}` DTOs and read back. No delete endpoint or unsupported supplier/date filter exists.

Stock supports actual query/brand/explicit available threshold/page filters. The threshold is visible and only sent when enabled; its non-secret user preference is separate from existing login settings. H/R/available and cost/price are server values. The grid is read-only; counting takes an absolute quantity, reason, **stock row** version and variantId in the path. The dialog labels its delta as projected. Journal uses variantId and paging. Errors clear stale rows and show failure, distinct from a successful no-match.

ProductEditor preserves quantity suggestions across save, requires persisted unchanged product data before handoff, and opens the real PurchaseEditor. Suggestions stay local until explicit draft save. Product cost/stock are refreshed after the dialog closes.

Each intention keeps its immutable key. Unknown transition outcomes require GET reconciliation; unknown counting outcomes require journal lookup by operationKey, then stock readback. A completed journal entry prevents resending. With no known receipt ID, a narrowly scoped creation retry accepts only the identical in-memory JSON and original requestKey, on the fixed purchases route, with a fresh CSRF token. No general blind retry or invented GET-by-requestKey exists. A fresh intention gets a new key. No keys or credentials are persisted.

Public purchase/stock `INVALID_INPUT`400 business descriptions can be shown as bounded plain text to explain stopped suppliers or amount limits. No message parsing guesses fields. Authentication/SQL/stack/500 response text is never displayed;500 retains trace-only handling. Writes returning500/503 also require readback because their outcome may be uncertain.

## Reproduce

Use the isolated SQL QA setup from [W1](WINFORMS_W1.md). Configure `PHONESTORE_QA_SERVER`, `PHONESTORE_QA_DATABASE` and `PHONESTORE_TEST_CONNECTION` externally; only a QA target is accepted. Every fixture creates and removes its own random QA database. Do not use demo or `PhoneStore_Rebuild`.

```powershell
dotnet build PhoneStore.slnx -c Release
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release --no-build
dotnet run --project tests/PhoneStore.Admin.Tests -c Release --no-build
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release --no-build -- --desktop-supply
dotnet run --project tests/PhoneStore.Checks -c Release --no-build -- --integration
```

The full solution and desktop commands require Windows. On Ubuntu build Web and the original Checks explicitly as in CI, then run the cross-platform Admin.Client.Checks project without `--no-build`. The Windows CI job runs fake-API STA controls and uploads captures; real desktop SQL smoke is local Windows evidence, not attributed to that CI job. Client and form runners have nonzero failure exits. Existing backend runner is unchanged.

## W4 acceptance —17 IDs

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN |
|---|---:|---:|---:|---:|---:|
|P0|9|9|0|0|0|
|P1|8|8|0|0|0|
|P2|0|0|0|0|0|

All IDs below have executed evidence. Source tests are [SupplyChecks](../tests/PhoneStore.Admin.Client.Checks/SupplyChecks.cs), [SupplyRaceGate](../tests/PhoneStore.Admin.Client.Checks/SupplyRaceGate.cs), [SupplyFormsChecks](../tests/PhoneStore.Admin.Tests/SupplyFormsChecks.cs), [real desktop controls](../tests/PhoneStore.Admin.Tests/RealSupplyChecks.cs) and parent [SQL handshake](../tests/PhoneStore.Admin.Client.Checks/DesktopQaChecks.cs). Numbered groups refer to `sql-results.json.records`, in order. Read-only SQL snapshots include purchases/lines, variants, stock/journal, suppliers, orders/lines, payment and history. `before`/`after` SHA256 references resolve to full ordered snapshots in `sql-states.json`.

| ID | Priority | Status | Executed proof |
|---|---|---|---|
|WIN.A05.04|P1|PASS|Group13 reads persisted IDs/SKU/cost and local7/3 hints without writes, then saves draft without stock change; actual default modal handoff control test; real desktop five-stage SQL sequence.|
|WIN.A14.01|P0|PASS|Groups1/2/11: POST requestKey→201→GET, invalid lines atomic, unknown reply blocks changed payload and recovers only identical key/body. Actual editor and same-key recovery controls.|
|WIN.A14.02|P0|PASS|Groups2/3/4: versioned whole-line PUT; stale409, final-state PUT409 with identical SQL snapshots. Final-state controls disabled.|
|WIN.A14.03|P1|PASS|Group1 server creator/time/status/code/total125M; actual editor and real40M draft capture. No client total authority.|
|WIN.A14.04|P1|PASS|Groups4/5: empty draft saves/cancels, empty confirm400, unchanged SQL; form blocks and explains empty confirmation.|
|WIN.A14.05|P1|PASS|Group6: >20 real drafts, page2/query/state/no-match and read-only SQL; actual list controls.|
|WIN.A15.01|P0|PASS|Group3 two-connection barrier, one204/one409; H+10/+5, server cost8M/9M and price9.6M/11.25M, two journals; real confirmation dialog and H0→4 SQL.|
|WIN.A15.02|P0|PASS|Groups4/12: cancel empty/nonempty with version+key, no H/R/cost/price effect, missing key400, uncertain response GET reconciliation.|
|WIN.A15.03|P0|PASS|Group3 real contention; repeat stale409 and fresh-version finalized409; one effect, unchanged SQL on rejected repeats. Held control task proves double click sends one command.|
|WIN.A15.04|P1|PASS|Group5: stopped supplier/empty/overflow-price400, draft retained; failed second line rolls back earlier stock/price updates. Public validation reason asserted; form retains rejected input.|
|WIN.A16.01|P1|PASS|Group7 actual query/brand/available8 threshold8vs7, distinct pages; controls expose explicit threshold and server H/R/available.|
|WIN.A16.02|P0|PASS|Group7 test-only interceptor causes actual SQL THROW51001 on stock SELECT→500 SERVER_ERROR/trace, not stock0; separate no-match success. Actual UI error/empty captures.|
|WIN.A16.03|P1|PASS|Group10 >20 real journal entries, variant and key queries, page2, actor/document/time, unchanged SQL; journal UI capture.|
|WIN.A17.01|P0|PASS|Group8 deliberately distinct productId/variantId and stock/catalog versions, exact absolute count12/path/trimmed reason; wrong version409; actual native confirmation and real H4→5.|
|WIN.A17.02|P0|PASS|Group8 negative/belowR/sameH rejected with identical SQL; form requires reason, rejects belowR and has read-only stock grid.|
|WIN.A17.03|P0|PASS|Groups8/9: same-key old version409, same count fresh version400 INVALID_INPUT, changed count reused key409; lost committed reply journal-key resolution no second write; new intention new key.|
|WIN.A17.04|P1|PASS|Group8 stale stock/catalog version409 and unchanged SQL; actual form retains reason and displays reload instruction.|

## Evidence and concurrency details

[SQL outcomes and sanitized request bodies](evidence/admin-w4/sql-results.json), [full snapshot dictionary](evidence/admin-w4/sql-states.json), [desktop step snapshots](evidence/admin-w4/desktop-sql-results.json), [fake controls](evidence/admin-w4/forms/results.json), [real QA controls](evidence/admin-w4/real-forms/results.json). Logs are bounded to generated QA fixtures; no session cookies, CSRF values, passwords or live connection strings are published.19 fake captures and10 real captures use actual forms/DrawToBitmap at120DPI,1280×800 main surfaces and native620×330 confirmations. These are captures, not generated product imagery.

The confirmation race intercepts both actual exclusive `PhoneStore:write` lock commands **before execution**; both tasks remain pending, arrival count2 and distinct SQL connection count2 are asserted before release. Both use real authenticated API requests. The loser is `409 CONCURRENT_UPDATE`; the winner returns204. No shipping test hook, schema or lock change exists.

The independent [full UI review](evidence/admin-w4/finish-review.md) required readable disabled-confirm explanations and pending-load pager guards. Both were fixed together and [scored resolved](evidence/admin-w4/finish-verdict.md); the verdict is **ship at the F1/F2 fix scope**. Targeted held-read tests prevent page skips; seven new W4 form groups and25 total pass. [Design documentation](evidence/admin-w4/design-documentation.md) compares the native extension with the incumbent system and preserves historical MVC design files. Two old W2 form assertions that deliberately expected the future purchase handoff to be disabled now expect its W4 availability; no backend or prior backend test was edited.

Repeated POST creation with the same unchanged requestKey returns **201 and the same ID**, exactly one purchase; changing its payload returns409 INVALID_STATE. Repeated counting at the same value with a fresh version returns400 INVALID_INPUT, the already accepted backend behavior. A global operation key with a different count returns409 INVALID_STATE. No backend fix is proposed.

Desktop SQL checkpoints independently observe: product-saved and handoff-open purchases0/H0/R0/journal0; draft-saved purchase1/quantity4/H0; confirmed H4/R0/journal1; counted H5/R0/journal2. Cost10M/price12M, orders0/payment0 throughout. Child controls block at each stage until the parent SQL observer acknowledges it.

## Deferred and scope audit

The [113-ID ledger](WINFORMS_CHECKLIST_RESULTS.md) lists every remaining NOT RUN row and its stage/reason, including cumulative partial criteria. No full WinForms gate is accepted at W4. W5 owns full customer-order drilldown, transitions, returns and invoice; W6 owns reports, whole flows and final UI sweep. File10 reconciliation changes only the cumulative A09.02 status; the17 independently proven W4 IDs remain PASS. The tracker is acceptance guidance, not a request to implement W5/W6 now.

Diff against the W3 merge base is empty for `src/PhoneStore.Core`, `src/PhoneStore.Infrastructure`, `src/PhoneStore.Web` and `tests/PhoneStore.Checks`. Shipping changes are confined to Admin/Admin.Client; tests are confined to the new Admin test projects. CI changes only add W4 to the Windows evidence-upload paths. Neither backend services, API, schema nor migrations are changed. The existing private/untracked document/prototype directories are preserved and not staged.
