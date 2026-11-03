# D3 final demo-data acceptance

D3 started on `feature/demo-data-d3` from the latest fetched `develop`, `70a80c4630c62031ebed17fdb0d758ea2b4b63dc`, after merged D2 PR #116. Initial status contained only the user's untracked `.vscode/`, `TAI_LIEU_BACKEND/` and `ux-ui_design_Phuong_an_A/`; these directories remain untouched. Issue [#117](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/issues/117) follows the repository implementation-task template.

The source contains exactly **43 IDs: 24 P0 / 19 P1 / 0 P2**. The initial D2 ledger had 34 PASS and these nine NOT RUN:

| ID | Priority | Reason at D3 start |
|---|---|---|
| DATA.VERIFY.02 | P0 | Existing Web `--audit-db` CLI had not been run explicitly on the full demo |
| DATA.VERIFY.03 | P0 | Real ADMIN/MVC full-demo end-to-end acceptance remained |
| DATA.VERIFY.04 | P1 | Actual Release WinForms full-demo dashboard/report screenshots remained |
| DATA.VERIFY.05 | P1 | Application list/report elapsed times had not been measured |
| DATA.VERIFY.06 | P1 | Complete final regression rerun and runtime diff proof remained |
| DATA.DOC.01 | P1 | Final creation/reset/environment/limitations guide remained |
| DATA.DOC.02 | P1 | Final sanitized summary and actual screenshots remained |
| DATA.GATE.01 | P0 | Other P0s, final-head CI and final scope evidence were incomplete |
| DATA.GATE.02 | P1 | Final per-ID and priority accounting remained |

## Actual evidence

Fresh full seed `2611`, `--as-of 2026-11-03`, completed in 171.890 seconds. Its lifecycle-aware normalized manifest hash matches the merged D2 full runs. Existing Web audit is valid both before and after the real application smoke. Read-only SQL verification after smoke passes all invariants, counts, quarter targets and exact normalized manifest equality; sessions return to zero. No schema or runtime edit was needed.

| Evidence | Result and detail |
|---|---|
| [Full SQL verification](../evidence/demo-data/d3/verification.json) | 601 orders / 813 lines / 56 purchases / 125 variants / 150 images; 31 recent orders, 12 eligible monthly bestseller products, 8 low-stock and 3 physically empty variants |
| [Existing audit before](../evidence/demo-data/d3/audit-before.json), [after](../evidence/demo-data/d3/audit-after.json) | Both `Valid=true`, `Errors=[]`, all 19 counts, sessions zero |
| [Real ADMIN HTTP](../evidence/demo-data/d3/http.json) | Authenticated login/logout, two disjoint pages of 20, nine quarterly revenues nonzero/distinct and equal to SQL, bestsellers, low/zero inventory, completed-order invoice, 150 images HTTP 200 |
| [Actual MVC Chromium](../evidence/demo-data/d3/browser/results.json) | Eight homepage bestsellers, all 46 active products across four pages, each product's real image/description/specifications, form login and owned history/detail for a demo customer, no page errors; five screenshots |
| [Actual Release WinForms](../evidence/demo-data/d3/winforms/results.json) | Real `.exe` through existing Kestrel/API/SQL, SQL-matched dashboard/revenue/bestseller/stock, order paging and invoice; nine screenshots, no fake API or form fixture |
| [Business preservation and application hashes](../evidence/demo-data/d3/smoke.json) | Before/after business hash equal, zero sessions; real Release Web/Admin binary hashes |
| [Regressions](../evidence/demo-data/d3/regression.json) | 16 actual suites: 322 backend checks, 63 client checks, 36 forms, 56 native DPI captures, five MVC browser suites, five desktop integration suites, previous Release walkthrough with 19 SQL checkpoints, 24 D1/D2 groups including six intentional corruptions and three temporal boundaries |
| [Summary and screenshot inventory](../evidence/demo-data/summary.json) | Timings, all table/quarter/status counts, screenshot SHA256 inventory and limitations; no passwords, password hashes, cookies or connection strings |

Actual WinForms first/second order pages took 697.0/307.1 ms. Revenue/bestseller/stock reports took 186.9/186.7/181.9 ms. HTTP first order page took 333.7 ms; maximum quarterly-report call was 121.3 ms. MVC first catalog page took 291.7 ms. All measured HTTP operations are below 2 seconds, browser/native below 5 seconds. These are local observations with full data, not a load benchmark. [DEMO_DATA.md](../DEMO_DATA.md) explains timing boundaries, warm samples and the midnight snapshot.

Selected actual screens: [dashboard](../evidence/demo-data/d3/winforms/winforms-dashboard.png), [orders](../evidence/demo-data/d3/winforms/winforms-orders.png), [revenue](../evidence/demo-data/d3/winforms/winforms-revenue.png), [bestsellers](../evidence/demo-data/d3/winforms/winforms-bestsellers.png), [stock](../evidence/demo-data/d3/winforms/winforms-stock-report.png), [invoice](../evidence/demo-data/d3/winforms/winforms-invoice.png), [MVC home](../evidence/demo-data/d3/browser/mvc-home.png), [catalog](../evidence/demo-data/d3/browser/mvc-catalog.png), [product description](../evidence/demo-data/d3/browser/mvc-product.png), [customer history](../evidence/demo-data/d3/browser/mvc-customer-history.png).

## Ledger snapshot and final external gate

The committed [per-ID ledger](../evidence/demo-data/d3-checklist.json) is captured **before remote CI**. It retains all original IDs and priorities. The final PR verification must bind the newest head SHA to both required green CI jobs, then close `DATA.GATE.01` and publish **43 PASS (24 P0 / 19 P1), 0 FAIL, 0 BLOCKED, 0 NOT RUN**. Until that evidence exists, Gate 01 is explicitly NOT RUN; this document does not claim a future run passed.

| Group | P0 PASS / total | P1 PASS / total | FAIL | BLOCKED | NOT RUN |
|---|---:|---:|---:|---:|---:|
| SAFE | 4 / 4 | 2 / 2 | 0 | 0 | 0 |
| SCALE | 1 / 1 | 1 / 1 | 0 | 0 | 0 |
| TIME | 3 / 3 | 1 / 1 | 0 | 0 | 0 |
| CATALOG | 4 / 4 | 3 / 3 | 0 | 0 | 0 |
| CUST | 1 / 1 | 2 / 2 | 0 | 0 | 0 |
| PURCH | 2 / 2 | 1 / 1 | 0 | 0 | 0 |
| ORDER | 5 / 5 | 3 / 3 | 0 | 0 | 0 |
| VERIFY | 3 / 3 | 3 / 3 | 0 | 0 | 0 |
| DOC | 0 / 0 | 2 / 2 | 0 | 0 | 0 |
| GATE | 0 / 1 | 1 / 1 | 0 | 0 | 1 |

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN |
|---|---:|---:|---:|---:|---:|
| P0 | 24 | 23 | 0 | 0 | 1 |
| P1 | 19 | 19 | 0 | 0 | 0 |
| P2 | 0 | 0 | 0 | 0 | 0 |
| Total | 43 | 42 | 0 | 0 | 1 |

Only pending ID in this pre-CI snapshot: **DATA.GATE.01 — exact latest-head remote CI has not yet run at snapshot time**. Final gate results live in the linked PR verification, rather than embedding a commit's own future CI result into itself. Human review and merge remain with the user.

## Scope and source wording

The D3 prompt ends with an abbreviated tool/images-only sentence, while explicitly requiring documentation and evidence. The precise source `DATA.GATE.01` and file 12 section 2b decision 8 permit new tool/tests, docs, solution registration, additive CI and new static images. D3 uses only new demo-data test helpers, a dispatch in that new test project, and docs/evidence. No `src/`, schema, migration, API, MVC, WinForms, old regression, solution or workflow change is present. Historical D1/D2 ledger files remain unchanged. No merge, tag, deployment or branch deletion is performed.
