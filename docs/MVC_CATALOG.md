# Public catalog, product details and home — MVC step3

This PR adds HOME/K06–K09 **public read and selection** to the approved customer MVC frame. Its base is Account PR91's verified merge **342741f71111e8e1f9e6d415073222f920ddeb55** ([post-merge CI277](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41185732045)). Latest supplied01/05 and accepted02/03 decisions control the scope; official05 still has99 unique IDs:48P0/43P1/8P2. The private Word/prototype inputs remain local.

**The user explicitly chose to keep Cart/Add in step4.** Details renders the real POST form with productId, variantId, quantity and one antiforgery token, but this PR implements no Cart/Add action and is **not usable to run a purchase flow**. WEB.K09.02, WEB.K09.03 and WEB.K09.04 stay **NOT RUN — “bằng chứng một phần, phần POST ở bước 4”**. GET/adapter/UI assertions below do not replace the absent shipping POST assertions. Existing foundation test probes are not production endpoints or acceptance of those missing branches.

## Behavior and boundary

| Surface | Implemented behavior |
|---|---|
| Home / Home/Index | Existing configured introduction/contact/footer plus current-month UTC+7 bestsellers from PublicBestsellerService; empty sales section hidden without fallback products |
| Catalog/Index | Public-only catalog under DEC.03; q/brand/price/storage/color/sort filters, exact total and query-preserving pagination |
| Filter reset | Removing a single condition keeps the others; clearing filters retains search/sort/pageSize and returns to page1 |
| Input errors | Invalid pagination returns400 HTML; syntactically valid positive page beyond last returns200 empty with the exact total. Invalid filter syntax/range/sort returns a soft200 form error, never a fabricated zero-result query |
| Catalog/Details/{id} | Public variants, gallery, server prices and availability; missing/discontinued/hidden-parent products return404; foreign selected variant returns404 through the approved adapter guard |
| Variant controls | Storage/color radios display matching server-supplied values and disable impossible combinations; no-JS GET selection remains functional; quantity UI has integer/min/max constraints; zero availability disables quantity/Add and says Hết hàng |

Controllers call IStoreService directly in the same process. The MVC interface/adapter gains delegating catalog, public-brand and bestseller reads. ReadProductAsync and the existing AddToCartAsync share the approved read-only product/variant association guard before any cart-service call. The adapter does not price products, calculate physical/reserved stock, decide authorization or transition orders. Card projection selects a display row from the service's already-filtered public variants; it never computes a selling price or locally sorts/filter products. Sorting, eligibility, coherent reads and authoritative availability stay in existing services.

No existing Infrastructure/service/schema/migration, API Program/Endpoints, session validator or WinForms file changes. Only the MVC boundary/presentation, QA tests and documents change. Catalog/home responses are no-store, Razor encodes text/attributes, and public ViewModels contain no cost, OnHand, Reserved, password hash, ActorId or OperationKey. JavaScript uses textContent and public DOM attributes for display only; it does not calculate totals, alter stock or call the application's own API. No guest localStorage or fake cart action is added.

The optionA frame is retained: navy/blue, white/pale surfaces, three product columns at1440px, two at768px, one at390px; mobile filters become a native expandable panel. Gallery URLs come from the service; absent/broken images use an explicit placeholder. QA evidence uses clearly labelled synthetic image panels in a runner-owned webroot, not production product assets or copied private prototype illustrations.

## Executed checks and limits

Release build: zero warnings/errors. **291 full SQL/API/MVC checks pass**, including the277 inherited groups and14 new catalog groups. Group counts differ from checklist-row counts; two groups intentionally report only partial K09 evidence. The generated SQL QA fixture has visible and inactive-product/brand/category/variant cases, split variants, distinct filtered prices, current completed COD sales and held orders. Blue256 has positive physical stock8 with all8 reserved, so public availability is0; other selections expose6/7, not physical balances.

| Evidence | What ran |
|---|---|
| WEB.K06.01–03 | Actual anonymous HTML pages, exact public totals/disjoint pagination, empty/whitespace/no-match, invalid pagination400; controlled QA table-name fault produces real500/trace and is restored in finally |
| WEB.K07.01–02 | Special/SQL-like/HTML searches remain literal, retained and encoded; actual product name/spec/description script-like data also encoded |
| WEB.K08.01–03 | Same-variant trap excluded; combined filters show24m rather than unfiltered12m, next page22m; all conditions retained; removing a condition/reset totals; soft range/sort errors |
| WEB.K06.04 / WEB.K08.04 | Real Chromium3/2/1 column metrics, labels/no overflow, image/fallback, mobile filter opening and visible errors |
| WEB.K09.01 | Server variant GET and actual browser radio changes show exact supplied price/availability; no-JS fallback works |
| WEB.K09.02–04 PARTIAL | GET404 and direct adapter mismatch rejection before cart SQL; form fields/token, integer/min/max UI, invalid combination and disabled Hết hàng state. **No real Cart/Add POST assertion; rows remain NOT RUN** |
| WEB.HOME.01 | Real empty and populated home sections, exact service ranking/prices, configured content and footer |

Full before/after snapshots compare inventory, variant prices, products/images, accounts/profiles/sessions, carts, orders/lines, payments, history and movements. Snapshots remain in memory; hashes/session IDs/cookies/tokens are not exported. Read requests and rejections leave those business rows unchanged. Test-only metadata edits and table-name failure injection restore in finally and run only inside the fixture's guarded generated QA DB. No shared, demo or PhoneStore_Rebuild database is used.

- [Browser measurements and scope](evidence/mvc-catalog/browser-results.json)
- [Home desktop](evidence/mvc-catalog/home-desktop.png), [390px](evidence/mvc-catalog/home-mobile.png)
- [Catalog desktop](evidence/mvc-catalog/catalog-desktop.png), [768px](evidence/mvc-catalog/catalog-tablet.png), [390px](evidence/mvc-catalog/catalog-mobile.png)
- [Empty](evidence/mvc-catalog/catalog-empty-mobile.png), [filter error](evidence/mvc-catalog/catalog-error-mobile.png)
- [Details desktop](evidence/mvc-catalog/details-desktop.png), [390px](evidence/mvc-catalog/details-mobile.png), [selected variant](evidence/mvc-catalog/details-selected-mobile.png), [sold out](evidence/mvc-catalog/details-sold-out-mobile.png)
- [Review and scored correction](evidence/mvc-catalog/finish-review.md), [design comparison](evidence/mvc-catalog/design-documentation.md)
- [All99 official IDs](MVC_CHECKLIST_RESULTS.md)

## Reproduce

Follow README restore/Release build and externally supplied matching PHONESTORE_QA_SERVER, PHONESTORE_QA_DATABASE and PHONESTORE_TEST_CONNECTION first. The runner verifies the explicit QA target, creates a new PhoneStore_QA_Rebuild_GUID database and removes only the one it created.

```powershell
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --mvc-catalog
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --integration
```

Browser evidence uses the same guarded QA fixture and real Kestrel rather than production preview switches. Install Playwright locally in ignored artifacts, then run the dedicated test-runner mode:

```powershell
npm install --prefix .artifacts/mvc-browser playwright@1.56.1
& .artifacts/mvc-browser/node_modules/.bin/playwright.cmd install chromium
$env:PHONESTORE_PLAYWRIGHT_MODULE = (Resolve-Path .artifacts/mvc-browser/node_modules/playwright).Path
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --mvc-catalog-browser
```

The browser helper seeds via existing services inside the disposable QA DB, copies shipping static files to a runner-owned webroot, adds labelled QA SVG fixtures, starts its own loopback host and closes its own processes before DB disposal. It exports only loopback URL/public fixture IDs to the browser; connection strings/passwords never enter that file. No fixture route, password or preview flag is shipped. Browser assertions cover public reading/selection only and never submit Cart/Add. Normal CI includes all new WAF/SQL checks through --integration; browser capture is a separate local command.

## Review and next step

The independent finish review found one duplicated filter indicator; its final verdict pass scores that fix resolved. This is the scoped review outcome, not whole-site approval. Existing design-sidecar drift is recorded without repairing the design system. No new backend defect or unresolved document contradiction was found after the user settled the Cart/Add boundary.

Cart/guest merging/checkout/orders/WEB.FLOW and full UI05–UI07 acceptance remain future PRs. No auto-merge: require successful CI on the exact latest PR head, then wait for the user.

Delivery is [PR93](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/93). Its [Checks page](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/93/checks) and final verification comment bind the current head to its successful CI. The committed ledger preserves the executed implementation snapshot; a documentation-only amendment still requires a fresh latest-head check before handoff.
