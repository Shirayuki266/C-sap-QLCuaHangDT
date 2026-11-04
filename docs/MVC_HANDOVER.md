# Customer MVC handover

Scope: ASP.NET Core MVC/Razor customer K01–K15, home/catalog, guest/customer carts, COD checkout and owned order tracking/cancellation. This acceptance branch starts from merged PR99 (`3bc247dd79ee3eab7919cdfe60fa34671a448f26`). It does not merge itself or deploy production.

Review: [PR101](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/101). The final verification comment on this PR binds the latest head SHA to its completed CI run; the implementation snapshot below is retained as provenance.

## Run and test

Follow the root README restore/Release build and new-database migration instructions before using `--no-build`. Inject `ConnectionStrings__Store` externally and start:

```powershell
dotnet run --no-build -c Release --project src/PhoneStore.Web -- --urls http://localhost:5080
```

Open `/Home/Index`, then register a customer. Normal startup creates no accounts and performs no migration/seed. Use explicit development/QA data preparation for a nonempty catalog; no default password is provided. Use HTTPS and externally managed credentials outside local development.

For tests, configure the three matching QA variables from README (`PHONESTORE_QA_SERVER`, `PHONESTORE_QA_DATABASE`, `PHONESTORE_TEST_CONNECTION`). The executable checks require an explicit `PhoneStore_QA_` target and create/drop only their own generated databases.

```powershell
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --integration
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --mvc-acceptance
# Install Playwright into an ignored tool directory, or supply an existing installation:
npm install --prefix .artifacts/mvc-browser playwright@1.56.1
node .artifacts/mvc-browser/node_modules/playwright/cli.js install chromium
$env:PHONESTORE_PLAYWRIGHT_MODULE = (Resolve-Path .artifacts/mvc-browser/node_modules/playwright).Path
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --mvc-acceptance-browser
```

The browser runner starts a real loopback Kestrel host against its own disposable SQL QA fixture. Admin changes use actual authenticated admin APIs. A test-process file handshake records SQL checkpoints; there is no added application QA endpoint. Browser-only pauses expose loading/disabled-submit states; separate real POSTs prove duplicate-submit behavior. A production error-page fixture supplies the browser transport-error demonstration without installing a production fault route.

## Accepted differences

| Decision / IDs | Accepted implementation |
|---|---|
| WEB.K01.02, WEB.K04.02 | Duplicate account/profile values produce a summary at the top of the form. Safe submitted values remain; passwords are cleared. No guessing the field from message text. No partial creation/update. |
| WEB.K09.02 | Read-only `(productId, variantId)` association check in the approved MVC adapter. Mismatch is404 before any cart service call. Prices, stock and authorization remain service decisions. |
| WEB.BASE.01, WEB.GATE.04 | Approved new Core IStoreService interface, in-process MVC adapter and MVC host configuration. Existing services/schema/admin API are preserved. |
| WEB.K02.04, WEB.K10.04 / DEC.02 | Guest merge uses service max quantity, caps availability, drops inactive/unavailable lines, reports adjustments, is repeat-safe and clears localStorage only after success. |
| Error mapping, WEB.BASE.11 | Existing backend codes and proposal aliases are translated at the MVC transport boundary; ordinary business errors do not become accidental500s. Backend/API contracts are unchanged. |

See [source boundary audit](MVC_ARCHITECTURE_AUDIT.md), [backend handover](BACKEND_HANDOVER.md) and the earlier function-specific MVC reports for backend decisions and the individual HTTP/SQL/concurrency proofs retained from prior merged PRs.

## Operational and accessibility limits

No production deployment, scheduled backup service or legacy-database upgrade path is delivered. The optional demo seeder still requires an explicit command and externally supplied passwords but is not locked by QA database name; never run it on a live database. Real catalog images/content and production SQL/TLS/secret management remain deployment responsibilities.

Guest carts depend on localStorage and JavaScript; unavailable storage produces a visible recovery message. Customer/checkout/order/profile pages are no-store; the server is authoritative. Cookie sessions are HttpOnly, SameSite=Lax and fixed8-hour expiry. Checkout accepts COD only. Amounts use Vietnamese formatting and displayed times use UTC+7; storage remains UTC.

Keyboard, labels, alt attributes, textual status and responsive layout are checked at the documented browser scope; this is not a formal WCAG certification or an exhaustive browser/assistive-technology matrix. QA SVG images and screenshots are evidence, not production photography.

## Acceptance ledger and CI

The official latest05 contains exactly **99 IDs:48 P0,43 P1,8 P2**. The current per-ID statuses, arithmetic, P0 inventory, remaining reasons and evidence links are maintained in [MVC_CHECKLIST_RESULTS.md](MVC_CHECKLIST_RESULTS.md). No criterion is PASS merely because its UI exists. Final exact-head CI and PR links will be bound in the PR verification after CI completes; any new commit requires rechecking that head.

<!-- final-counts:start -->
| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN | N/A |
|---|---:|---:|---:|---:|---:|---:|
|P0|48|48|0|0|0|0|
|P1|43|43|0|0|0|0|
|P2|8|8|0|0|0|0|
|Total|99|99|0|0|0|0|

Verified implementation CI: [SUCCESS](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41240516979) at `1e7f8f5852bb0638276a2f665e87e221ce5e6798`. The final PR verification binds the latest head after documentation changes.
<!-- final-counts:end -->
