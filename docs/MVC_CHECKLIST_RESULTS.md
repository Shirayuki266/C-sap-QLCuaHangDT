# MVC checklist — COD checkout step4b

Official latest05: **99 unique IDs /48P0 /43P1 /8P2**; labels unchanged. SHA256: 28d3546ffab80960d3e64dfa6bde1c112c5815d51342c957f88e52dcb0149305. Base: merged PR95 atd4138d8. K13, K14.03-04 and K15 remain step5. No complete phase/FLOW claim.

310 local SQL regression groups and11 focused groups pass. Real Chromium proves price confirmation, double-submit/F5, qualified Back/HTTP replay and session recovery; SQL barrier proves last-unit contention. Exact latest PR-head CI is bound in final PR verification; any later commit requires a fresh green run.

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN | N/A |
|---|---:|---:|---:|---:|---:|---:|
|P0|48|35|0|0|13|0|
|P1|43|28|0|0|15|0|
|P2|8|6|0|0|2|0|
|Total|99|69|0|0|30|0|

[Checkout evidence and Back qualification](MVC_CHECKOUT.md), [machine-readable99 rows](evidence/mvc-checkout/checklist-results.json). Earlier evidence is retained at its proven scope; no unexecuted criteria become PASS.

## Complete P0 PASS inventory — 35 unique IDs

Derived from official per-ID rows, no duplicates. Historical reconciliation: accepted accounts14 P0 PASS included WEB.BASE.05 (omitted only in an earlier chat summary); catalog17, cart25. This step's total is derived afresh, not copied from that summary.

| No. | P0 PASS ID |
|---:|---|
|1|WEB.BASE.02|
|2|WEB.BASE.03|
|3|WEB.BASE.05|
|4|WEB.BASE.06|
|5|WEB.K01.01|
|6|WEB.K01.02|
|7|WEB.K01.03|
|8|WEB.K02.01|
|9|WEB.K02.02|
|10|WEB.K02.03|
|11|WEB.K03.01|
|12|WEB.K04.01|
|13|WEB.K05.01|
|14|WEB.K05.02|
|15|WEB.K06.01|
|16|WEB.K07.01|
|17|WEB.K09.01|
|18|WEB.K09.02|
|19|WEB.K10.01|
|20|WEB.K10.02|
|21|WEB.K10.03|
|22|WEB.K10.04|
|23|WEB.K11.01|
|24|WEB.K11.02|
|25|WEB.K12.01|
|26|WEB.K12.02|
|27|WEB.K12.03|
|28|WEB.K12.04|
|29|WEB.K12.05|
|30|WEB.K12.06|
|31|WEB.K12.07|
|32|WEB.K14.01|
|33|WEB.K14.02|
|34|WEB.GATE.03|
|35|WEB.GATE.04|

## P0

| ID | Status | Evidence / remaining scope |
|---|---|---|
|WEB.BASE.01|NOT RUN|Account/catalog/cart/checkout/read-only receipt use the in-process adapter; remaining order list/control/cancel actions await step5. No complete15-UC claim.|
|WEB.BASE.02|PASS|Global MVC antiforgery plus actual Account and all five Cart POST routes reject missing/wrong tokens without SQL writes; Quote/Merge require header token. Recheck future checkout/orders when added.|
|WEB.BASE.03|PASS|Existing account/catalog/cart privacy evidence plus new customer-only order projection and actual receipt HTML: no Cost/OnHand/Reserved/hashes/ActorId/OperationKey; raw OrderView/history actors never enter MVC views.|
|WEB.BASE.04|NOT RUN|Search/profile/catalog plus checkout recipient/address and receipt HTML encoding proved; cancellation-reason UI and its XSS branches remain step5.|
|WEB.BASE.05|PASS|Real Profile challenge and MVC Login GET/POST tested with local/external/protocol-relative/backslash destinations; Url.IsLocalUrl is used.|
|WEB.BASE.06|PASS|Cookie thực qua WAF: HttpOnly/SameSite=Lax/Secure HTTPS, SQL hết hạn8h, không sliding Set-Cookie; phiên hết hạn/thu hồi redirect. Dùng chính bộ validator phiên cũ.|
|WEB.BASE.07|NOT RUN|Cart/profile/checkout/receipt no-store and live price/availability proved; remaining order-list/control pages await step5 coverage.|
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
|WEB.K09.02|PASS|Prior read/UI evidence plus actual shipping Cart/Add rejects missing/inactive products and mismatched pair404 with zero cart SQL commands; association stays in adapter.|
|WEB.K10.01|PASS|Authenticated Add accumulates2+2; stock/excess/inactive-selection rejection is explicit; full inventory H/R/versions, journal, orders and payments unchanged.|
|WEB.K10.02|PASS|Forged form price/total/customerId and browser localStorage price/identity ignored; Quote/unit prices/totals use server values.|
|WEB.K10.03|PASS|Real guest double-submit, reload, quoted edits and last removal use localStorage; public Quote creates no SQL cart row and exposes only availability.|
|WEB.K10.04|PASS|SQL+Chromium prove max, duplicate-ID max, current-stock caps, discontinued/missing/zero-stock removal, adjustment text, repeated/parallel merge and clearing storage only on success.|
|WEB.K11.01|PASS|HTTP own-cart update/last removal; Chromium guest quote/update/last removal and customer PRG update/remove; totals match server and empty cart is valid.|
|WEB.K11.02|PASS|Two SQL QA customers; forged identity fields cannot update/remove another customer cart; other row remains1.|
|WEB.K12.01|PASS|Guest challenge and empty-cart redirect; two actual GETs generate distinct nonempty hidden GUIDs; editable profile defaults and commerce SQL unchanged.|
|WEB.K12.02|PASS|Real POST PRG to receipt with code/total/text status/COD/unpaid; exact SQL order/line/payment/history/GIU +1, H unchanged, R +quantity, own cart cleared and unrelated rows unchanged.|
|WEB.K12.03|PASS|SQL price change produces fresh displayed/protected quote, preserves shipping and explicitly requires another submit. HTTP and browser SQL checkpoints prove no order/reservation before confirmation.|
|WEB.K12.04|PASS|Empty/inactive/shortage and injected late SQL payment failure show safe form/line errors and no success; ordered full H/R/cart/order/line/payment/history/journal snapshots unchanged.|
|WEB.K12.05|PASS|Concurrent original-key HTTP replay, browser double-submit guard and receipt F5 keep one order/reservation. Chromium Back to no-store POST gives ERR_CACHE_MISS; captured form/key is then replayed through browser HTTP, same receipt/SQL. Stale DOM form restoration is not claimed.|
|WEB.K12.06|PASS|Two real WAF POSTs overlap at a gated SQL write-lock boundary:2 arrivals,2 independent SQL connections, both incomplete before release. Exactly1 winner; H3→3/R2→3; losing cart retained and no partial losing document.|
|WEB.K12.07|PASS|Forged total/price/Lines price/paymentStatus/customerId/role ignored; account-bound protected server quote supplies confirmation. Only exact COD accepted; wrong/tampered/cross-account confirmation rejected with no SQL effects.|
|WEB.K13.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K14.01|PASS|Actual other-customer order ID and nonexistent ID both404 with byte-identical HTML; ordered commerce SQL unchanged.|
|WEB.K14.02|PASS|Catalog name/price/color and profile are changed in QA; full customer snapshot/timeline DTO remains identical, actual receipt still shows original line/recipient/price/address. Browser also verifies COD/payment status and UTC+7.|
|WEB.K15.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K15.02|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K15.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K15.04|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.FLOW.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.FLOW.02|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.FLOW.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.GATE.01|NOT RUN|K01-K12 and minimum read-only K14 implemented; K13/order-list, remaining K14 controls and K15/cancel await step5. No complete15-UC/FLOW claim.|
|WEB.GATE.02|NOT RUN|Remaining P0 NOT RUN require step5/order controls/end-to-end flows; partial phase acceptance only.|
|WEB.GATE.03|PASS|310 SQL QA regression groups and11 focused groups pass. CI SUCCESS at implementation snapshot f9eb42fc6c5285a03d8cdfc58708cfa00452215e; final exact-head CI is bound in PR verification and must be rerun after any commit.|
|WEB.GATE.04|PASS|Diff from develop d4138d8 is empty for existing Infrastructure/services/migrations/admin endpoints/Program/Security/WinForms. Approved MVC interface/adapter/projection/host registration and presentation only;310 regression groups pass.|

## P1

| ID | Status | Evidence / remaining scope |
|---|---|---|
|WEB.BASE.08|NOT RUN|ADMIN account/profile checks retained; remaining cancellation/order controls await step5.|
|WEB.BASE.09|PASS|WAF HTML403/404/500 và500 môi trường Production; thông báo chung +traceId, không stack/private exception. Playwright chụp404 desktop/mobile.|
|WEB.BASE.10|NOT RUN|Server money and actual receipt UTC+7 verified; comprehensive remaining order list/control formatting awaits step5.|
|WEB.BASE.11|NOT RUN|Checkout input/stock/reprice/used-key paths now exercised; remaining cancellation/state error mappings await step5.|
|WEB.BASE.12|NOT RUN|Account guards retained and browser checkout double-submit emits one POST; server replay is idempotent. Cancellation still awaits step5.|
|WEB.BASE.13|NOT RUN|Account/cart/checkout safe values retained on errors and price change; cancellation form awaits step5.|
|WEB.BASE.14|PASS|310 SQL QA regression groups and11 focused groups pass. CI SUCCESS at implementation snapshot f9eb42fc6c5285a03d8cdfc58708cfa00452215e; final exact-head CI is bound in PR verification and must be rerun after any commit.|
|WEB.BASE.15|PASS|Diff mới không thêm bí mật; Razor/HTML không có cấu hình nhạy cảm; logger không ghi body/cookie/token/password. Snapshot account/session/hash chỉ giữ trong RAM, không xuất.|
|WEB.BASE.16|NOT RUN|Current optionA frame/badge/cart/checkout/receipt consistent; remaining order-list/control screens await step5.|
|WEB.K01.04|PASS|Two registrations without email store NULL; malformed email produces field error and identical SQL snapshot.|
|WEB.K01.05|PASS|Policy visible before submission; 10/128-character valid boundaries accepted; short/129/no-letter/no-digit rejected without mutation or echoed password.|
|WEB.K02.04|PASS|Real Chromium login returns to Cart/Index, merges guest cart, refreshes header to8, and preserves data on injected network/500 failures before retry.|
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
|WEB.K09.03|PASS|Existing sold-out button/text browser proof plus real authenticated Cart/Add rejects sold-out variant and leaves SQL unchanged.|
|WEB.K09.04|PASS|Existing min/max/integer UI plus real POST zero/negative/fraction/text/int overflow/int-max/excess quantities reject with form errors and identical cart SQL.|
|WEB.HOME.01|PASS|Actual Home reads current-month PublicBestsellerService: QA product ranking2 then1, current server min prices, intro/contact/footer. Empty monthly data hides entire section; public reads preserve business SQL.|
|WEB.K10.05|PASS|Playwright aborts first merge and injects500 next; storage survives both; Retry succeeds and clears original snapshot.|
|WEB.K10.06|PASS|Guest badge2/0, merged customer8, F5 still8, update/remove5 verified in Chromium; customer badge is initially server-rendered.|
|WEB.K11.03|PASS|Failed update retains99 with affected variant association; actual SQL price change and discontinued state appear on exact server-rendered lines; live shortages shown by Quote.|
|WEB.K11.04|PASS|Actual guest sign-in entry uses local returnUrl Cart/Index and browser returns there after login/merge. Signed-in checkout remains disabled explicitly for step4b.|
|WEB.K12.08|PASS|Required recipient/phone/address produce field errors and retain safe entered values; defaults originate from profile and are editable. Shipping remains through price reconfirmation.|
|WEB.K12.09|PASS|SQL expires session mid-flow; old POST redirects login with local Checkout return. Real Chromium relogin retains cart/fresh GET key; expired cookie still denied afterward; SQL unchanged until authorized new-session submit.|
|WEB.K13.02|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K13.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K14.03|NOT RUN|Reserved for step5 as requested. Minimal receipt uses safe projected timeline; comprehensive timeline acceptance remains NOT RUN.|
|WEB.K14.04|NOT RUN|Cancellation controls/confirmation explicitly reserved for step5; no cancel action/button in this PR.|
|WEB.K15.05|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.FLOW.04|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.FLOW.05|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.UI.01|NOT RUN|UI03/UI04/UI13 prior captures, UI05 carts, UI06 checkout/receipt desktop/mobile; full UI07/order workflows await step5.|
|WEB.GATE.05|NOT RUN|Account/catalog/cart/checkout/receipt captures are not complete WEB.FLOW.01-03 evidence.|
|WEB.GATE.06|PASS|All99 official IDs retained with exactly48P0/43P1/8P2, per-ID evidence/limits and counts.|

## P2

| ID | Status | Evidence / remaining scope |
|---|---|---|
|WEB.K02.05|PASS|Authenticated GET Login/Register redirects home with unchanged session count.|
|WEB.K06.04|PASS|Real cards show supplied/fallback images, name, filtered public-variant price and stock text. Chromium verifies desktop3/tablet2/mobile1 columns and no overflow; QA image panels are test fixtures, not shipping assets.|
|WEB.K08.04|PASS|Chromium390px filter is closed initially, opens by summary/keyboard; error state opens with visible summary; desktop sidebar remains available.|
|WEB.HOME.02|PASS|StorefrontOptions cấu hình tập trung tên/giới thiệu/liên hệ; Razor render nội dung được cấu hình trong WAF và encode script-like input. Đây là ô cấu hình, không nghiệm thu toàn UI13.|
|WEB.K11.05|PASS|Actual UI05 guest/customer screenshots390px; one-column cards, no horizontal overflow and labeled controls; desktop1440px retained.|
|WEB.K12.10|PASS|Actual390px checkout/reprice/resumed captures measure summary below shipping, submit afterward, zero overflow/unnamed inputs.|
|WEB.UI.02|NOT RUN|Account/catalog/cart/checkout/receipt labels and browser interactions covered at scope; whole-site order/cancel keyboard checks await step5.|
|WEB.UI.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
