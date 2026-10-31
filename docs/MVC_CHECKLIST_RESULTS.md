# MVC checklist — Catalog/home step (PR3 of6)

Official latest file05: **99 unique IDs /48P0 /43P1 /8P2**; criteria and labels unchanged. Input SHA256: 28d3546ffab80960d3e64dfa6bde1c112c5815d51342c957f88e52dcb0149305. Base is verified Account merge342741f.

This is a test-evidence snapshot, not final MVC acceptance. Exact final PR-head CI is recorded in its final verification comment. Any later commit requires a fresh CI check. **Cart/Add action is step4; this PR is not a purchase flow. WEB.K09.02–04 remain NOT RUN: “bằng chứng một phần, phần POST ở bước 4”.**

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN | N/A |
|---|---:|---:|---:|---:|---:|---:|
|P0|48|17|0|0|31|0|
|P1|43|19|0|0|24|0|
|P2|8|4|0|0|4|0|
|Total|99|40|0|0|59|0|

[Current catalog evidence](MVC_CATALOG.md), [browser results](evidence/mvc-catalog/browser-results.json), [machine-readable99 rows](evidence/mvc-catalog/checklist-results.json). Accepted account/foundation evidence is retained at its proven scope;291 passing check groups are not291 checklist rows or complete shopping-flow acceptance.

## P0

### Complete P0 PASS inventory —17 unique IDs

This numbered inventory is derived from the per-ID rows below and checked against the official05 priority labels. It is a cumulative list, not only the newly executed account/catalog cases.

| No. | P0 PASS ID | Existing executed evidence / scope |
|---:|---|---|
|1|`WEB.BASE.05`|Account PR91; MvcAccountChecks: real login, protected redirect and local/external returnUrl cases.|
|2|`WEB.BASE.06`|Foundation PR87 retained; MvcFoundationChecks and real Account cookie/session checks.|
|3|`WEB.K01.01`|Account PR91; real registration, PRG, one CUSTOMER/profile, no automatic session.|
|4|`WEB.K01.02`|Account PR91; duplicate username/phone/email, summary-only errors, unchanged SQL snapshots.|
|5|`WEB.K01.03`|Account PR91; overposted role/active/id ignored in actual HTTP form.|
|6|`WEB.K02.01`|Account PR91; successful login, fixed session/cookie, local returnUrl.|
|7|`WEB.K02.02`|Account PR91; generic unknown/wrong/locked failures and no session creation.|
|8|`WEB.K02.03`|Account PR91; ADMIN login rejected on customer form with generic feedback.|
|9|`WEB.K03.01`|Account PR91; POST/logout CSRF, current-session revocation, old-cookie replay rejected.|
|10|`WEB.K04.01`|Account PR91; own-profile updates only, foreign IDs/role/active ignored.|
|11|`WEB.K05.01`|Account PR91; current/new-password field errors preserve SQL and clear response secrets.|
|12|`WEB.K05.02`|Account PR91; successful password change revokes every device session.|
|13|`WEB.K06.01`|Catalog PR93; exact public eligibility, totals/pagination and unchanged business SQL.|
|14|`WEB.K07.01`|Catalog PR93; literal special/SQL-like/HTML searches, safe retained text and encoding.|
|15|`WEB.K09.01`|Catalog PR93; server variant price/availability, real browser and no-JS selection.|
|16|`WEB.GATE.03`|Exact-head required SQL CI; use PR93 Checks and its latest verification comment.|
|17|`WEB.GATE.04`|Protected backend diff is empty; approved MVC boundary only,291 regression checks.|

**Count reconciliation:** the Account snapshot has14 P0 PASS =10 account IDs (`WEB.K01.01–03`, `WEB.K02.01–03`, `WEB.K03.01`, `WEB.K04.01`, `WEB.K05.01–02`) + `WEB.BASE.05` + `WEB.BASE.06` + `WEB.GATE.03` + `WEB.GATE.04`. The earlier chat summary listed the account IDs but omitted the additional shared-rule ID **WEB.BASE.05**, making the IDs explicitly mentioned there add up to13; the stored14-row set already included its real redirect/returnUrl proof. No extra PASS is created by this reconciliation.

Catalog adds exactly3 P0 PASS (`WEB.K06.01`, `WEB.K07.01`, `WEB.K09.01`): **14 +3 =17**, leaving **48 −17 =31 P0 NOT RUN**. `WEB.K09.02` remains NOT RUN; partial GET/adapter evidence is not counted as PASS. The gate row is valid only with successful CI on the current PR head; the latest verification comment binds that SHA.

| ID | Status | Evidence / remaining scope |
|---|---|---|
|WEB.BASE.01|NOT RUN|Account controller now calls the in-process adapter; catalog/cart/order controllers remain later steps. No complete15-UC claim.|
|WEB.BASE.02|NOT RUN|Missing/invalid tokens on all five real Account POST routes reject400 with identical SQL; real Quote/Merge await cart PR.|
|WEB.BASE.03|NOT RUN|Public catalog/details/account HTML and ViewModels checked for forbidden fields; real cart/order HTML still absent.|
|WEB.BASE.04|NOT RUN|Search/product/profile text encoding tested on actual pages; shipping recipient/cancel reason screens remain deferred.|
|WEB.BASE.05|PASS|Real Profile challenge and MVC Login GET/POST tested with local/external/protocol-relative/backslash destinations; Url.IsLocalUrl is used.|
|WEB.BASE.06|PASS|Cookie thực qua WAF: HttpOnly/SameSite=Lax/Secure HTTPS, SQL hết hạn8h, không sliding Set-Cookie; phiên hết hạn/thu hồi redirect. Dùng chính bộ validator phiên cũ.|
|WEB.BASE.07|NOT RUN|Actual Account pages no-store; cart/checkout/order live-state acceptance awaits those pages.|
|WEB.K01.01|PASS|Real POST Register redirects to Login + TempData; SQL account/profile +1, sessions unchanged; repeat creates no second account.|
|WEB.K01.02|PASS|Three real duplicate POSTs (username/phone/email): summary only, safe input retained, passwords absent, exact accounts/profiles/sessions/business snapshot unchanged.|
|WEB.K01.03|PASS|HTTP form role=ADMIN, active=false, id=other ignored; SQL proves new active CUSTOMER with its own profile.|
|WEB.K02.01|PASS|Real MVC login creates fixed eight-hour SQL session and secure cookie; local Profile redirect; external/protocol-relative/backslash return URLs ignored.|
|WEB.K02.02|PASS|Nonexistent/wrong/locked (wrong and correct password) requests render identical generic summary and create no session or partial SQL changes.|
|WEB.K02.03|PASS|ADMIN credentials on actual customer form produce the same generic summary and no SQL session.|
|WEB.K03.01|PASS|GET Logout returns405 without revocation; tokened POST revokes exactly one session; replay of actual old cookie redirects to login.|
|WEB.K04.01|PASS|Real profile POST ignores foreign customerId/id/Username/role/active; only own profile changes, other customer and account role unchanged.|
|WEB.K05.01|PASS|Wrong current/weak new password yields correct field error; both password inputs cleared; exact SQL snapshot/hash/sessions unchanged.|
|WEB.K05.02|PASS|Valid change removes every SQL session (multiple real devices); both cookies challenged; login notice appears; old password fails, new succeeds.|
|WEB.K06.01|PASS|Real anonymous Catalog HTTP: four public products, exact total and disjoint2-item pages. Inactive product/brand/category/all-inactive variants excluded. Full business snapshot unchanged; no forbidden stock/cost/identity fields in HTML.|
|WEB.K07.01|PASS|HTTP special/SQL-like/HTML search retained safely and literal; product name/spec/description script-like metadata also encoded. No executing markup,500 or snapshot changes.|
|WEB.K09.01|PASS|Real server-rendered variant GET and Chromium storage/color changes show exact server prices and availability. SQL Blue256 has H8/R8 but UI available0; active selections have limits6/7. No-JS selection also works.|
|WEB.K09.02|NOT RUN|bằng chứng một phần, phần POST ở bước 4. GET missing/discontinued/hidden-parent product and foreign variant return404. Shared read-only adapter guard blocks mismatch before any cart SQL; no real Cart/Add POST action/assertion exists.|
|WEB.K10.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K10.02|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K10.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K10.04|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K11.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K11.02|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K12.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K12.02|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K12.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K12.04|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K12.05|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K12.06|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K12.07|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K13.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K14.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K14.02|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K15.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K15.02|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K15.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K15.04|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.FLOW.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.FLOW.02|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.FLOW.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.GATE.01|NOT RUN|Only foundation, accounts and public catalog/home/details are implemented; cart/checkout/orders remain later steps. No15-UC or purchase-flow completion claim.|
|WEB.GATE.02|NOT RUN|Remaining P0 NOT RUN await cart/checkout/order/flow steps; partial K09.02 does not become PASS from a probe or GET alone.|
|WEB.GATE.03|PASS|291 SQL QA checks; CI SUCCESS on verified implementation a5d521734f0b1177d1a4d6a2df0ebe791443b3e7. Final PR-head CI is bound in its verification comment; any later commit requires recheck.|
|WEB.GATE.04|PASS|Protected Infrastructure/services/migrations/API Program/Endpoints/Security have empty diff from develop342741f. Only MVC boundary/presentation/tests/docs change;291 local regression checks passed.|

## P1

| ID | Status | Evidence / remaining scope |
|---|---|---|
|WEB.BASE.08|NOT RUN|Real ADMIN cookie rejected403 on Profile/Password and API unchanged; checkout/cancel await later PRs.|
|WEB.BASE.09|PASS|WAF HTML403/404/500 và500 môi trường Production; thông báo chung +traceId, không stack/private exception. Playwright chụp404 desktop/mobile.|
|WEB.BASE.10|NOT RUN|Actual catalog/detail/home use server VND values; order-time UI remains deferred (formatter UTC+7 unit proof retained).|
|WEB.BASE.11|NOT RUN|Actual account errors mapped to summary/field/redirect/403; cart/checkout/order-specific mappings await later PRs.|
|WEB.BASE.12|NOT RUN|Chromium actual Register/Login double submits each1POST with disabled button, busy label and aria-busy; checkout/cancel deferred.|
|WEB.BASE.13|NOT RUN|Actual account forms preserve safe values and clear secrets on errors; remaining forms deferred.|
|WEB.BASE.14|PASS|291 SQL QA checks; CI SUCCESS on verified implementation a5d521734f0b1177d1a4d6a2df0ebe791443b3e7. Final PR-head CI is bound in its verification comment; any later commit requires recheck.|
|WEB.BASE.15|PASS|Diff mới không thêm bí mật; Razor/HTML không có cấu hình nhạy cảm; logger không ghi body/cookie/token/password. Snapshot account/session/hash chỉ giữ trong RAM, không xuất.|
|WEB.BASE.16|NOT RUN|Account links and POST Logout wired in shared frame; complete shopping navigation pending other controllers.|
|WEB.K01.04|PASS|Two registrations without email store NULL; malformed email produces field error and identical SQL snapshot.|
|WEB.K01.05|PASS|Policy visible before submission; 10/128-character valid boundaries accepted; short/129/no-letter/no-digit rejected without mutation or echoed password.|
|WEB.K02.04|NOT RUN|NOT RUN: guest localStorage cart and real /Cart/Merge belong to the cart/checkout PR; no synthetic merge endpoint shipped.|
|WEB.K03.02|PASS|Logout preserves exact profiles/cart/orders/lines/payments/history/stock/variants snapshot; other device works, anonymous header hides previous cart count.|
|WEB.K03.03|PASS|Expired SQL session POST Logout redirects to Login; exact SQL snapshot unchanged; no500.|
|WEB.K04.02|PASS|Duplicate phone/email render summary only with retained input and identical complete SQL snapshot; own unchanged phone/email accepted.|
|WEB.K04.03|PASS|PRG Profile displays saved fields + TempData; serialized old order recipient/address/phone and stock unchanged.|
|WEB.K04.04|PASS|Anonymous actual Profile/Password routes redirect to local Login and carry no-store.|
|WEB.K05.03|PASS|Submitted passwords absent from HTTP response bodies, browser password inputs and actual Kestrel host logs; no body/secret logging added.|
|WEB.K06.02|PASS|Actual no-match200 HTML contains recovery suggestions and zero cards. Controlled QA table-name fault returns real500/trace HTML, never empty success; table restored and snapshot unchanged.|
|WEB.K06.03|PASS|Zero/negative/noninteger/overflow page and invalid pageSize consistently return400 HTML; valid positive page beyond last returns200 empty with original total. No500 or SQL writes.|
|WEB.K07.02|PASS|Empty/whitespace query returns all four public products; no-match returns friendly zero-card/zero-total structure without writes.|
|WEB.K08.01|PASS|Combined brand+price+storage+color+sort excludes split-variant trap; main filtered card24m versus unfiltered12m. Page2 keeps every condition and shows22m. Real browser confirms equivalent interactions.|
|WEB.K08.02|PASS|Actual generated remove-color URL preserves price/storage/brand/query/sort and returns total3; reset filters preserves remaining search and coherent total/page1.|
|WEB.K08.03|PASS|Inverted price, unknown sort, malformed/negative/over-precision amounts and invalid IDs render soft200 filter errors without false results or SQL writes. Decimal annotation uses invariant limits; mobile errors auto-open panel.|
|WEB.K09.03|NOT RUN|bằng chứng một phần, phần POST ở bước 4. Real detail UI shows Hết hàng and disables Add/quantity for available0, including positive physical stock fully reserved. Direct POST rejection must be asserted after step4 action exists.|
|WEB.K09.04|NOT RUN|bằng chứng một phần, phần POST ở bước 4. Form sends productId/variantId/quantity +one antiforgery token; browser verifies integer/min/max UI and valid variant association. Server POST quantity revalidation not exercised here.|
|WEB.HOME.01|PASS|Actual Home reads current-month PublicBestsellerService: QA product ranking2 then1, current server min prices, intro/contact/footer. Empty monthly data hides entire section; public reads preserve business SQL.|
|WEB.K10.05|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K10.06|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K11.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K11.04|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K12.08|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K12.09|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K13.02|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K13.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K14.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K14.04|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K15.05|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.FLOW.04|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.FLOW.05|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.UI.01|NOT RUN|UI03/UI04/UI13 desktop1440 and390px plus768px catalog captures exist. UI05–UI07 cart/checkout/order screens remain unimplemented.|
|WEB.GATE.05|NOT RUN|Account screenshots are not complete WEB.FLOW.01–03 purchasing-flow evidence.|
|WEB.GATE.06|PASS|All99 official IDs retained with exactly48P0/43P1/8P2, per-ID evidence/limits and counts.|

## P2

| ID | Status | Evidence / remaining scope |
|---|---|---|
|WEB.K02.05|PASS|Authenticated GET Login/Register redirects home with unchanged session count.|
|WEB.K06.04|PASS|Real cards show supplied/fallback images, name, filtered public-variant price and stock text. Chromium verifies desktop3/tablet2/mobile1 columns and no overflow; QA image panels are test fixtures, not shipping assets.|
|WEB.K08.04|PASS|Chromium390px filter is closed initially, opens by summary/keyboard; error state opens with visible summary; desktop sidebar remains available.|
|WEB.HOME.02|PASS|StorefrontOptions cấu hình tập trung tên/giới thiệu/liên hệ; Razor render nội dung được cấu hình trong WAF và encode script-like input. Đây là ô cấu hình, không nghiệm thu toàn UI13.|
|WEB.K11.05|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K12.10|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.UI.02|NOT RUN|Catalog filter/detail selection keyboard and labels tested, with no-JS fallback; whole-site cart/order controls remain deferred.|
|WEB.UI.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
