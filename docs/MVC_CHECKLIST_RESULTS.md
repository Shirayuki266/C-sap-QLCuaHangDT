# MVC checklist — final acceptance

Official latest05: **99 unique IDs /48 P0 /43 P1 /8 P2**. SHA256: 28d3546ffab80960d3e64dfa6bde1c112c5815d51342c957f88e52dcb0149305. Base: merged PR99 at3bc247d. Previous81 PASS rows retain their actual prior evidence; the remaining18 criteria are assessed below without changing priorities.

322 full local regression groups and4 focused HTTP groups pass on disposable guarded SQL QA databases. Actual Chromium/admin API flows have per-step SQL H/R/Available, payment/history/journal proof. Final exact-head CI is bound in PR verification; any new commit requires a fresh SUCCESS.

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN | N/A |
|---|---:|---:|---:|---:|---:|---:|
|P0|48|48|0|0|0|0|
|P1|43|43|0|0|0|0|
|P2|8|8|0|0|0|0|
|Total|99|99|0|0|0|0|

[Handover](MVC_HANDOVER.md), [source boundary audit](MVC_ARCHITECTURE_AUDIT.md), [all99 machine-readable rows](evidence/mvc-acceptance/checklist-results.json).

## Complete P0 PASS inventory — 48 unique IDs

1. WEB.BASE.01
2. WEB.BASE.02
3. WEB.BASE.03
4. WEB.BASE.04
5. WEB.BASE.05
6. WEB.BASE.06
7. WEB.BASE.07
8. WEB.K01.01
9. WEB.K01.02
10. WEB.K01.03
11. WEB.K02.01
12. WEB.K02.02
13. WEB.K02.03
14. WEB.K03.01
15. WEB.K04.01
16. WEB.K05.01
17. WEB.K05.02
18. WEB.K06.01
19. WEB.K07.01
20. WEB.K09.01
21. WEB.K09.02
22. WEB.K10.01
23. WEB.K10.02
24. WEB.K10.03
25. WEB.K10.04
26. WEB.K11.01
27. WEB.K11.02
28. WEB.K12.01
29. WEB.K12.02
30. WEB.K12.03
31. WEB.K12.04
32. WEB.K12.05
33. WEB.K12.06
34. WEB.K12.07
35. WEB.K13.01
36. WEB.K14.01
37. WEB.K14.02
38. WEB.K15.01
39. WEB.K15.02
40. WEB.K15.03
41. WEB.K15.04
42. WEB.FLOW.01
43. WEB.FLOW.02
44. WEB.FLOW.03
45. WEB.GATE.01
46. WEB.GATE.02
47. WEB.GATE.03
48. WEB.GATE.04

## P0

| ID | Status | Evidence / remaining scope |
|---|---|---|
|WEB.BASE.01|PASS|Read all five controllers, adapter and quote-protection boundary; forbidden SQL/HTTP/cost/stock/raw-HTML search empty. Approved read-only association/display affordances distinguished from authoritative service decisions.|
|WEB.BASE.02|PASS|Global MVC antiforgery plus actual Account and all five Cart POST routes reject missing/wrong tokens without SQL writes; Quote/Merge require header token. Recheck future checkout/orders when added.|
|WEB.BASE.03|PASS|Existing account/catalog/cart privacy evidence plus new customer-only order projection and actual receipt HTML: no Cost/OnHand/Reserved/hashes/ActorId/OperationKey; raw OrderView/history actors never enter MVC views.|
|WEB.BASE.04|PASS|Earlier actual search/recipient/address encoding plus new script-like cancellation/failed-delivery reasons remain literal in real Chromium timeline; no unexpected alert or raw script HTML. Cancel reason encoding branch is now covered.|
|WEB.BASE.05|PASS|Real Profile challenge and MVC Login GET/POST tested with local/external/protocol-relative/backslash destinations; Url.IsLocalUrl is used.|
|WEB.BASE.06|PASS|Cookie thực qua WAF: HttpOnly/SameSite=Lax/Secure HTTPS, SQL hết hạn8h, không sliding Set-Cookie; phiên hết hạn/thu hồi redirect. Dùng chính bộ validator phiên cũ.|
|WEB.BASE.07|PASS|Actual HTTP every personal page: no-store, no ETag/Last-Modified, conditional request never304. Real admin price/stock changes reflected on subsequent reads; per-step SQL confirms no client cache authority.|
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
|WEB.K13.01|PASS|Real guest challenge, own11-order scope, forged customerId ignored, exact totals/disjoint pages, combined status/code/date filters and UTC+7 day boundaries on SQL QA.|
|WEB.K14.01|PASS|Actual other-customer order ID and nonexistent ID both404 with byte-identical HTML; ordered commerce SQL unchanged.|
|WEB.K14.02|PASS|Catalog name/price/color and profile are changed in QA; full customer snapshot/timeline DTO remains identical, actual receipt still shows original line/recipient/price/address. Browser also verifies COD/payment status and UTC+7.|
|WEB.K15.01|PASS|Exact SQL cancel: DA_HUY, H7→7/R6→5, COD CHUA_THU amount0 unchanged; one history/HUY_GIU movement with trimmed reason; snapshots/unrelated data unchanged. Forged identity/status/operation fields ignored.|
|WEB.K15.02|PASS|Direct POST foreign/missing/zero/negative IDs return byte-identical404 even with invalid reason; ordered commerce SQL unchanged.|
|WEB.K15.03|PASS|Real admin API login/token/read/confirm and MVC Cancel overlap on2 SQL sessions. Admin holds actual application lock; DMV observes customer blocked by it before admin commits. Admin204/customer302, exact reload notice, final DA_XAC_NHAN; H/R/payment/journal unchanged, only admin confirmation history added.|
|WEB.K15.04|PASS|Old-version replay, fresh-version repeat on DA_HUY and all post-confirmation states reject without any SQL changes; R is not released twice. Browser F5 also read-only.|
|WEB.FLOW.01|PASS|Real Chromium home/catalog/detail→guest localStorage→login/merge→COD order→receipt→cancel. SQL checkpoints for every step: place R+1, cancel R−1, H unchanged; COD remains unpaid, exact history/journal.|
|WEB.FLOW.02|PASS|Real admin APIs confirm/dispatch/complete; customer refresh verifies each textual status/timeline and collected COD. SQL: confirm unchangedH/R; dispatch H−1/R−1; complete payment collected for exact total.|
|WEB.FLOW.03|PASS|Real admin product PUT changes profit to30%; old checkout renders13.000.000 đ and requires reconfirmation without order/reservation. Second confirmation creates exact new-price snapshot and R+1.|
|WEB.GATE.01|PASS|All official K01-K15 individual criteria now PASS with real evidence across foundation/accounts/catalog/cart/checkout/orders PRs. This confirms15 implemented functions, not complete WEB.FLOW/site acceptance.|
|WEB.GATE.02|PASS|All other47 official P0 IDs PASS with evidence; no priority relabeling or approved-BLOCKED shortcut.|
|WEB.GATE.03|PASS|322 full SQL QA groups pass; CI SUCCESS at implementation snapshot 1e7f8f5852bb0638276a2f665e87e221ce5e6798. Final exact-head SUCCESS must be rechecked in PR verification after any docs-only amendment.|
|WEB.GATE.04|PASS|Protected Core/Infrastructure/Program/admin Endpoints/Security diff empty from merged PR99 base3bc247d. Existing services/schema/admin API unchanged; MVC presentation/error translation and tests only.|

## P1

| ID | Status | Evidence / remaining scope |
|---|---|---|
|WEB.BASE.08|PASS|Actual ADMIN API session rejected on protected customer GET/POST routes; public cart remains guest mode. No admin navigation links. All denied writes preserve commerce SQL.|
|WEB.BASE.09|PASS|WAF HTML403/404/500 và500 môi trường Production; thông báo chung +traceId, không stack/private exception. Playwright chụp404 desktop/mobile.|
|WEB.BASE.10|PASS|Shared Money/Time helpers reviewed; prior actual SQL/HTML UTC+7 boundary proofs retained. Current Chromium asserts exact13.000.000 đ after real admin profit update and integer quantity controls.|
|WEB.BASE.11|PASS|Actual MVC HTTP, test-only one-shot IStoreService faults and existing real failures prove proposal/backend aliases map to forms/redirects/statuses. Unknown business code400; intentional internal500 generic trace only.|
|WEB.BASE.12|PASS|Chromium observes busy/disabled feedback for Register/Login/Checkout/Cancel using browser-only submit pause; separately sends duplicate real submits and asserts one POST each. Prior SQL idempotency/replay/concurrency proofs retained.|
|WEB.BASE.13|PASS|Actual HTTP invalid/duplicate Register/Login/Profile/Password/Cart/Checkout/Cancel retain safe form input; passwords absent. Invalid requests leave ordered commerce snapshot unchanged.|
|WEB.BASE.14|PASS|322 full SQL QA groups pass; CI SUCCESS at implementation snapshot 1e7f8f5852bb0638276a2f665e87e221ce5e6798. Final exact-head SUCCESS must be rechecked in PR verification after any docs-only amendment.|
|WEB.BASE.15|PASS|Diff mới không thêm bí mật; Razor/HTML không có cấu hình nhạy cảm; logger không ghi body/cookie/token/password. Snapshot account/session/hash chỉ giữ trong RAM, không xuất.|
|WEB.BASE.16|PASS|Same-build six-screen desktop/mobile matrix retains shared header, account/cart navigation, footer and blue primary actions; actual navigation and keyboard checks executed.|
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
|WEB.K13.02|PASS|No-match and beyond-last pages show friendly empty recovery; reversed/invalid dates, unknown status and malformed/out-of-range pagination render gentle errors, no500.|
|WEB.K13.03|PASS|Full ordered commerce snapshots before/after list/filter/error/timeline reads are identical, including order status/CompletedAt, payments, H/R, history and movements.|
|WEB.K14.03|PASS|All six order states read through safe customer projection: timeline only time/from/to/reason; no ActorId/admin username/private fields. Script-like reasons stay text; valid1000-char unbroken reason fits390px.|
|WEB.K14.04|PASS|Real state fixtures show cancel panel only CHO_XAC_NHAN; status is textual. Chromium native confirm dismiss sends no POST; accept then duplicate submit produces1POST.|
|WEB.K15.05|PASS|Empty/whitespace/overlong reason gives field error with form/version retained; malformed version preserves entered reason. Missing/wrong CSRF rejected without writes; browser exercises actual server validation.|
|WEB.FLOW.04|PASS|Real admin stops product already in cart; invalid line remains visible and checkout disabled. SQL checkpoints prove no order/payment/history/journal/stock mutation.|
|WEB.FLOW.05|PASS|Real SQL session expiry between checkout GET/POST challenges login with local checkout returnUrl; no order by expired session. Actual relogin returns checkout with persisted cart intact.|
|WEB.UI.01|PASS|Fresh same-build UI03–UI07/UI13 full-page captures at1440px and390px;768px catalog grid. All audited widths fit, labels/alt present, product grid3/2/1 columns.|
|WEB.GATE.05|PASS|Actual FLOW01–03 screenshots and SQL checkpoints retained, including confirm/dispatch/complete and price reconfirmation.|
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
|WEB.UI.02|PASS|Real Chromium Tab/focus checks on all six screens, keyboard filter expansion and variant arrows; labels/alt and textual states. Independent rendered finish review recorded separately.|
|WEB.UI.03|PASS|Fresh catalog/cart/order empty, inactive/out-of-stock, field/validation404/500 and all-six navigation-loading captures. Form-only screens use missing-input states; actual server500 template transported by browser interception, no shipping fault route.|
