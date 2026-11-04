# Customer carts — MVC step4a

Base: catalog PR93 merged at `38ce221a3ee16a78aadf59653a396b2e7990ac44`, with [successful develop CI](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41200031860). Issue94 / branch `feature/94-mvc-cart`. This extends the accepted in-process MVC interface/adapter; existing services, migrations, schema, authentication implementation and admin endpoints are unchanged. Private Word/prototype inputs remain local. Latest supplied01/05 and approved DEC.02 control behavior; official05 has99 IDs:48P0/43P1/8P2.

Review: [PR95](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/95). The [verified implementation CI](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41203811944) ran299 groups successfully at `f5e3b4dafa4ba14476d530fe9ab84c595b89f732`. This documentation-only evidence update requires a fresh latest-head run, linked in the final PR verification comment; the snapshot is not presented as that final head.

## Behavior

- `GET /Cart/Index` supports guests and customers and sends no-store. Customer rows come from `IStoreService.ReadCartAsync`; guest rows are quoted from browser storage. No physical-stock or private account fields enter the view model/HTML/JSON.
- Customer `POST /Cart/Add` accepts only productId, variantId and quantity. The approved adapter reads the public product and checks the pair before calling CartService; missing/inactive/cross-product selections return404 with zero cart SQL commands. Successful adds accumulate and use PRG plus TempData. Failed edits render the cart with attempted quantity, a summary and the affected variant association.
- Customer `POST /Cart/Update` and `/Cart/Remove` use the current validated session. Client identity/price/total parameters do not bind to commands. Last-line removal is valid.
- JSON `POST /Cart/Quote` is public; `POST /Cart/Merge` requires CUSTOMER. Both require `X-CSRF-TOKEN`, supplied from a server-rendered token and its antiforgery cookie. Global antiforgery also protects form POSTs. Quote calls the existing read-only Preview; Merge delegates unchanged DEC.02 behavior.
- Guest storage key: `phonestore.guest-cart.v1`; content is an array of `{variantId,quantity}`. Other client properties are discarded; names, availability, prices and totals come from Quote. Inputs are structurally validated; rendering uses textContent, not HTML interpolation. Guest additions, edits and removals request a fresh quote before persistence. Duplicate submit events are guarded in flight.
- On authenticated page load, pending guest rows are merged using max(server,guest), duplicate-ID max, current-availability caps and discontinued/missing/zero-stock removal. The response updates the badge and adjustment text. Storage is removed only after a successful merge and only if another tab has not changed the original snapshot. Network/500 failures preserve it and expose Retry. Repeated/parallel merge requests do not multiply quantities. Customer cart HTML is refreshed from the server after merge.
- Cart operations do not reserve stock. Unit price and line/summary amounts are always based on current server values; no client price is accepted. Guest checkout entry signs in with local returnUrl `/Cart/Index`.

Checkout is **step4b**: the signed-in order button is explicitly disabled with explanatory text. This PR does not implement K12, Orders, purchasing FLOW or all-site acceptance.

## Run and verify

Follow the root [README](../README.md) for .NET10, SQL configuration, build and migration. There are no new configuration keys, credentials or shipping QA endpoints. Build before using `--no-build`.

```powershell
dotnet build tests/PhoneStore.Checks -c Release
$env:PHONESTORE_QA_SERVER = 'localhost'
$env:PHONESTORE_QA_DATABASE = 'PhoneStore_QA_MvcCart'
$env:PHONESTORE_TEST_CONNECTION = 'Server=localhost;Database=PhoneStore_QA_MvcCart;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --mvc-cart
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --integration
```

These are local Windows-authentication QA examples, not credentials. TestDatabase validates the explicit QA target, creates a separate `PhoneStore_QA_Rebuild_<GUID>` per fixture, and removes only that DB. Neither the demo/live DB nor the persistent web-preview QA DB is used.

For real Chromium evidence, install Playwright in the ignored artifact directory, then run:

```powershell
npm install --prefix .artifacts/mvc-browser --no-save playwright@1.56.1
node .artifacts/mvc-browser/node_modules/playwright/cli.js install chromium
$env:PHONESTORE_PLAYWRIGHT_MODULE = (Resolve-Path .artifacts/mvc-browser/node_modules/playwright).Path
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --mvc-cart-browser
```

The browser runner owns a fresh SQL fixture, starts the real Kestrel host on loopback with its guarded QA connection, runs `tests/browser/mvc-cart.mjs`, then closes that host and disposes its own DB. Network/500 failures are injected by Playwright only. Fixture images are synthetic QA assets mounted under an artifact webroot, never shipping product assets. Browser checks are a local prerequisite; CI executes the complete HTTP/SQL integration runner.

## Executed evidence

Eight new HTTP/SQL groups pass; the complete integration runner passes299 groups. The focused eight were rerun after extending the integer boundary to2147483647; the existing backend translates overflow to INVALID_INPUT, leaving cart and inventory unchanged. No service fix was necessary. Passing groups are not checklist rows.

| Evidence | Assertions |
|---|---|
| [HTTP groups](evidence/mvc-cart/checks.txt), `MvcCartChecks.cs` | Quote without persistence, malicious price/identity ignored, header/form CSRF rejection,404 pair rejection before cart SQL, accumulation, integer/stock validation, PRG, attempted value retention, owner isolation and empty last removal |
| HTTP SQL snapshots | Ordered inventory including versions/H/R, movement journal, orders and payments are identical before/after all cart operations. Cart snapshots are identical after rejected inputs and merge replays. Full rows/hashes/sessions remain in RAM; raw logs are not published |
| [Browser results](evidence/mvc-cart/browser-results.json), `tests/browser/mvc-cart.mjs` | Guest double-submit/reload/update/last removal, forged local prices, login return to cart, network+500 retention/retry, max/cap/drop, concurrent merge replay, badge and customer edits/removal |
| [Browser SQL oracle](evidence/mvc-cart/sql-results.json) | Inventory/journal/orders/payments unchanged; final own cart is one line, quantity5, server unit price12,000,000 and total60,000,000 |
| [Screenshots](evidence/mvc-cart/) | UI05 guest/customer desktop1440px and mobile390px; merge failure and empty guest mobile; no horizontal overflow or unnamed inputs in measured cart screens |
| [UI finish review](evidence/mvc-cart/finish-review.md), [design comparison](evidence/mvc-cart/design-documentation.md) | Ship at step4a UI scope; inherited design files preserved. Historical PR1 documentation drift reported without repairs |

[Current99-ID ledger](MVC_CHECKLIST_RESULTS.md) contains the official priority labels, complete P0 PASS list, per-ID evidence and remaining scope. WEB.K09.02–04 now include shipping Cart/Add assertions; WEB.K02.04 has real login/merge/header evidence. Future checkout/order/whole-site criteria stay NOT RUN where evidence is incomplete. Exact latest PR-head CI is bound in the PR's final verification comment; any later commit requires a new green run. No automatic merge.
