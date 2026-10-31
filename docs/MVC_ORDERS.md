# Customer orders and cancellation — MVC step5

Base: PR97 merged at `41398139b8bbbb151fb48e27fc864e742a6494d6`, with [successful develop CI](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41233361210). Issue98 / branch `feature/98-mvc-orders`. Latest01/05, approved02/03 and private Word/UI07 remain the inputs. Official05:99 unique IDs,48P0/43P1/8P2.

Review: [PR99](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/99). [Verified implementation CI](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41235864145) passed318 groups at `6a873ce7454797cedee918489478247e89efaeca`. This documentation-only evidence update requires a fresh final-head CI run linked in the PR verification comment; the snapshot is not presented as the final head.

This implements WEB.K13.01–03, WEB.K14.03–04 and WEB.K15.01–05. Existing services, schema/migrations, Program/admin endpoints, authentication implementation and WinForms have empty diff. Changes are confined to the approved MVC boundary/projections, controller/models/Razor/assets, tests and documentation. No automatic merge.

## Routes and mapping

| MVC input/action | Existing service / behavior |
|---|---|
| `GET /Orders/Index?status&from&to&code&page&pageSize` | In-process adapter calls OrderQueryService.List with the validated actor; no client customerId is bound. `code` maps to existing Query, which searches code/recipient/contact. UI labels this broader existing behavior truthfully; no new backend search is introduced |
| status/time/pagination | Existing service validates status/date windows/page bounds and applies UTC+7 inclusive calendar dates. MVC renders gentle form errors for INVALID_INPUT and a friendly no-match state. DB/unexpected errors are not converted to an empty list |
| `GET /Orders/Details/{id}` | Existing owned read; safe DTO strips history actors/admin account names. Version is added solely for the required hidden optimistic-concurrency field. Textual timeline contains time, from/to and reason; Razor encodes user text |
| `POST /Orders/Cancel` id/reason/version | Controller reads through the ownership boundary first, including invalid forms, so foreign/missing IDs both404. Required reason is a field error; safe attempted values/version are retained. The adapter creates its internal operation key and delegates to existing OrderWorkflow.Cancel. Extra customerId/status/operationKey fields never bind |
| success | PRG to Details with notice. Existing transaction changes pending order to DA_HUY, releases each reserved quantity, keeps physical stock/unpaid COD unchanged and writes cancellation history/journal once |
| CONCURRENT_UPDATE / STALE_VERSION / INVALID_STATE | PRG to current owned Details with “Đơn đã thay đổi, vui lòng tải lại. Yêu cầu hủy chưa được thực hiện.” No second reservation release |

The cancel control exists only on CHO_XAC_NHAN details. An expandable reason/confirmation panel requires an explicit “Xác nhận hủy đơn” submit; JS adds a native confirm dialog before POST, then the existing busy/double-submit guard. Dismissing sends no request. The service independently enforces ownership, reason, version and state on direct HTTP requests. Long unbroken reasons wrap inside the timeline instead of overflowing mobile panels.

## Run and test

Use the root [README](../README.md) for application build/configuration/new-DB migration. No new application configuration, credentials or shipping QA routes/hooks are required.

```powershell
dotnet build tests/PhoneStore.Checks -c Release
$env:PHONESTORE_QA_SERVER = 'localhost'
$env:PHONESTORE_QA_DATABASE = 'PhoneStore_QA_MvcOrders'
$env:PHONESTORE_TEST_CONNECTION = 'Server=localhost;Database=PhoneStore_QA_MvcOrders;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --mvc-orders
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --integration
```

These are local Windows-authentication QA examples, not production credentials. The runner validates explicit QA settings, creates `PhoneStore_QA_Rebuild_<GUID>` and disposes only its own database. It never uses PhoneStore_Rebuild, a demo/live database or the persistent web-preview QA database. The race observer requires a QA login allowed to inspect SQL lock-wait DMVs in addition to fixture DB creation; existing local QA and isolated CI logins supported this. No server permissions are changed by the tests.

For real Chromium, use the ignored Playwright installation from [MVC_CART](MVC_CART.md):

```powershell
$env:PHONESTORE_PLAYWRIGHT_MODULE = (Resolve-Path .artifacts/mvc-browser/node_modules/playwright).Path
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --mvc-orders-browser
```

The browser fixture owns its QA database, starts real Kestrel on loopback, verifies parent-process SQL checkpoints, closes its host and drops only its fixture DB. No test protocol is added to the shipping host. Fixtures use actual service transitions for all six order states; only deterministic search-date boundaries are assigned directly in QA before assertions.

## Executed evidence

| Evidence | What was proved |
|---|---|
| [Focused groups](evidence/mvc-orders/checks.txt), MvcOrdersChecks.cs |8 new WAF/SQL groups: own11-order pagination without overlap/foreign rows, combined code/status/UTC+7 date filters, invalid/no-match/search encoding, identical404 cancellation, CSRF/field errors, exact cancellation, repeat/post-confirm-state rejection, safe timeline/control across six states, real admin API race |
| [Cancellation SQL](evidence/mvc-orders/cancel-sql-results.json), MvcOrdersAssertions.cs |DA_HUY; H7→7/R6→5; COD CHUA_THU amount0 unchanged; history+1 and HUY_GIU journal+1, reason trimmed. Line/recipient/money snapshots and unrelated rows unchanged. Failed/repeated attempts compare full ordered commerce snapshots in RAM |
| [Race](evidence/mvc-orders/race-results.json), OrderCancelRaceGate.cs |Real API login/token/read/POST `/api/admin/orders/{id}/confirm` versus MVC Cancel. Both requests reach the SQL-write boundary on separate sessions. Admin acquires the actual application lock; customer is then released and SQL DMVs observe it blocked by that admin session. Only then may admin commit. Admin204, customer302/reload notice; final DA_XAC_NHAN, one admin history event, no cancellation journal and unchanged inventory/payment |
| [Browser](evidence/mvc-orders/browser-results.json), tests/browser/mvc-orders.mjs |Actual login return/list, filters/pagination, empty/reversed-date states, confirmed/failed control absence, server reason validation, native confirm dismiss/accept, two submit events yielding one accepted POST, reload, encoded script-like reason and mobile1000-character wrapping |
| [Browser SQL](evidence/mvc-orders/browser-sql-results.json) |List/filter/timeline/error/dismiss operations leave all commerce rows unchanged. Accepted cancellation matches exact H/R/order/payment/history/journal deltas; F5 does not write again |
| [Screenshots](evidence/mvc-orders/), [finish review](evidence/mvc-orders/finish-review.md), [design comparison](evidence/mvc-orders/design-documentation.md) |Eleven1440px/390px captures, desktop table/mobile labeled cards, empty/error/pending/confirmed/cancelled/long-reason states. One material timeline-wrapping fix was resolved and the follow-up verdict is ship at that fix's scope. Native browser dialogs are proved by Playwright events and SQL checkpoints, not claimed visible in page screenshots |

Full local integration passed318 groups; focused8 and Chromium were rerun after the wrapping fix/long-reason fixture. CI runs the full HTTP/SQL suite; browser evidence is local. Groups are not checklist rows. [The99-ID ledger](MVC_CHECKLIST_RESULTS.md) retains official priorities, complete P0 PASS inventory and per-ID remaining scope. All K01–K15 criteria now have individual evidence; end-to-end WEB.FLOW and final whole-site/accessibility acceptance remain separate step6 work. Latest exact-head CI is bound in final PR verification; later commits require a new green run.
