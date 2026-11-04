# COD checkout and read-only receipt — MVC step4b

Base: PR95 merged at `d4138d80c31c9e5f64d439df3027ed7c8e2582d6`, with [successful develop CI](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41205160444). Issue96 / branch `feature/96-mvc-checkout`. Latest01/05, approved02/03 decisions and private Word/UI06 are the inputs. Official05 still has99 unique IDs:48P0/43P1/8P2.

Review: [PR97](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/97). [Verified implementation CI](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41207499310) passed310 groups at `f9eb42fc6c5285a03d8cdfc58708cfa00452215e`. The documentation-only evidence update requires a fresh final-head run, linked in the PR verification comment; this snapshot is not presented as the final head.

Scope: WEB.K12.01–10 and minimum read-only WEB.K14.01–02. K13, K14.03–04, K15 and complete purchasing FLOW remain step5/later. There is no cancellation action or order list. Existing services, schema/migrations, Program/admin endpoints, authentication implementation and WinForms are unchanged. Only the approved MVC boundary, customer projection, presentation, tests and documentation change.

## Behavior

- `GET /Cart/Checkout` requires CUSTOMER. Empty carts redirect to Cart/Index; each opening generates a fresh hidden GUID requestKey. Recipient/phone/address are editable defaults from the customer's profile. Shipping precedes summary and submit on mobile.
- The form carries a Data Protection confirmation token binding the server's displayed variant/quantity/prices to the account and requestKey. The browser cannot author a confirmed price by adding total, price, Lines, customerId, role or paymentStatus fields. These fields do not bind. Antiforgery protects the POST separately. Confirmation tokens/cookies are not written to logs or evidence.
- `POST /Cart/Checkout` accepts recipient, phone, address, requestKey, protected confirmation and COD only. MVC performs presentation/shape validation; the existing CheckoutService owns live price/availability comparison, permissions, transaction, reservation and idempotency. No controller or adapter calculates business prices or updates stock.
- A PriceChangedException re-renders the current server quote, preserves entered shipping, replaces the protected confirmation and explicitly requests another submit. No order/reservation is created before that second confirmation. The same still-unused requestKey is retained. Input/stock errors re-render safe values and line feedback; failures never show success.
- Success uses PRG/TempData and redirects to `/Orders/Details/{id}`. Repeating the original requestKey/payload resolves the existing order, even after its cart has been cleared. An INVALID_STATE for an already-used own key is reconciled through the existing Resolve service and links back to that receipt; another customer's key cannot pass the account-bound confirmation.
- `GET /Orders/Details/{id}` is CUSTOMER-only and no-store. The service checks ownership; a foreign ID and nonexistent ID return identical404 HTML. The new CustomerOrderSnapshot projection removes account IDs, history actors, command keys and versions before MVC receives data. It retains immutable line/recipient/money/payment snapshots and a safe textual timeline. Times render UTC+7; SQL remains UTC.
- Existing server session validation rejects an expired cookie before checkout. Login returns to Checkout with the server cart intact and a fresh GET key. The expired cookie cannot create an order, including after another session logs in.

The signed-in cart now links to checkout. Guest cart/merge behavior from step4a remains unchanged. Old catalog/cart documents are historical evidence for their PR boundaries; this document describes the new current checkout scope.

## Run and test

Use the root [README](../README.md) to restore/build, configure SQL and explicitly migrate a new application DB. No new credentials, connection keys or test routes are added to the shipping host. Data Protection uses the existing ASP.NET Core provider; retain its normal key ring across host instances/restarts when hosting the site, as required for authentication/form protection.

```powershell
dotnet build tests/PhoneStore.Checks -c Release
$env:PHONESTORE_QA_SERVER = 'localhost'
$env:PHONESTORE_QA_DATABASE = 'PhoneStore_QA_MvcCheckout'
$env:PHONESTORE_TEST_CONNECTION = 'Server=localhost;Database=PhoneStore_QA_MvcCheckout;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --mvc-checkout
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --integration
```

These are local Windows-authentication QA examples, not production credentials. TestDatabase checks explicit QA configuration, creates a separate `PhoneStore_QA_Rebuild_<GUID>` and removes only its own DB. No test uses PhoneStore_Rebuild, a demo/live DB or the persistent web-preview DB.

For Chromium, use the ignored Playwright installation described in [MVC_CART](MVC_CART.md), then:

```powershell
$env:PHONESTORE_PLAYWRIGHT_MODULE = (Resolve-Path .artifacts/mvc-browser/node_modules/playwright).Path
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --mvc-checkout-browser
```

The browser fixture starts real Kestrel on loopback, serves synthetic QA imagery from an artifact webroot, and owns the QA DB. A parent-process file protocol changes a fixture price/expires fixture sessions and verifies SQL checkpoints; there is **no shipping QA endpoint, controller, environment toggle or test database hook**. Tokens/cookies stay in memory. The runner closes its host and disposes its generated DB.

## Evidence

| Evidence | Actual assertions |
|---|---|
| [HTTP groups](evidence/mvc-checkout/checks.txt), MvcCheckoutChecks.cs |11 new SQL-backed WAF groups: access/empty/fresh GUID; CSRF and protected quote rejection; required fields/COD; forged money/identity/status ignored; exact successful deltas; repeat/reload; ownership404; whole receipt/timeline snapshot equality; reprice then explicit confirmation; empty/inactive/shortage; late payment failure rollback; expired session; gated final-unit race |
| [Race](evidence/mvc-checkout/race-results.json), CheckoutRaceGate.cs | Both HTTP requests wait immediately before the real exclusive SQL application-lock command. The test observes2 arrivals,2 independent SQL connections and both requests incomplete before releasing the gate. Exactly1 success/1 stock failure; H3→3, R2→3; order/payment/history/journal each+1; losing cart retained. No sleep substitutes for overlap |
| MvcCheckoutSql.cs | Every success checks own-cart clearing, unchanged other carts, exact order/line/payment/history/GIU movement deltas, H unchanged and R increment once; unrelated rows remain identical. Every failure/replay compares ordered full commerce snapshots in RAM. No private full rows are exported |
| [Browser](evidence/mvc-checkout/browser-results.json), tests/browser/mvc-checkout.mjs | Fresh GET keys, changed price/reconfirmation, one browser POST from double-submit, receipt F5, Back/replay, UTC+7, expired-session login return/cart retention, old-cookie denial and successful new-session order |
| [Browser SQL oracle](evidence/mvc-checkout/browser-sql-results.json) | Reprice before confirmation leaves commerce SQL unchanged; first order quantity2 at13m has total26m. All original-key replays leave exactly that order. Expired-session attempts leave SQL unchanged; new-session order quantity1 at13m has total13m. Both commits have exact stock/order/payment/history/journal deltas |
| [Screenshots](evidence/mvc-checkout/), [UI review](evidence/mvc-checkout/finish-review.md), [design comparison](evidence/mvc-checkout/design-documentation.md) | UI06/receipt1440px and390px, repricing desktop/mobile, resumed checkout mobile; no overflow/unnamed controls, mobile summary follows shipping. UI ship verdict excludes backend certification; incumbent design files preserved |

**Back qualification:** Chromium returned `ERR_CACHE_MISS` when going Back to the previous no-store POST. It did not restore a stale DOM form. The test then replays the captured original form/key through the browser's HTTP context and verifies the same receipt and unchanged SQL. This is actual Back navigation plus HTTP replay evidence, not a claim that Chromium restored a DOM form and a button was clicked there. Concurrent HTTP replay and F5 are separately exercised. No cache policy was weakened to alter this browser behavior.

The complete local integration suite passes310 groups; focused checkout passes11. Browser runs are local evidence; CI runs the full SQL/HTTP suite. These are test groups, not checklist-row counts. [The99-ID ledger](MVC_CHECKLIST_RESULTS.md) contains exact priorities, current statuses, complete P0 PASS inventory and per-ID NOT RUN reasons. Latest exact-head CI is bound in the PR verification comment; any later commit requires a new green run. No automatic merge.
