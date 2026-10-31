# MVC checklist — Account step (PR2 of6)

Official latest file05: **99 unique IDs /48P0 /43P1 /8P2**. Criteria and labels are unchanged. Input SHA256: `28d3546ffab80960d3e64dfa6bde1c112c5815d51342c957f88e52dcb0149305`. Foundation PR87 merged as `be460cb59c0bfc8f0a49f1787bead0b0487579d7`; [post-merge CI](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41178900613).

This is an evidence snapshot, not final MVC acceptance. Current PR CI/head is recorded in the PR final verification comment; any later commit requires a fresh check. No automatic merge.

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN | N/A |
|---|---:|---:|---:|---:|---:|---:|
|P0|48|14|0|0|34|0|
|P1|43|12|0|0|31|0|
|P2|8|2|0|0|6|0|
|Total|99|28|0|0|71|0|

[Account HTTP/SQL evidence](MVC_ACCOUNTS.md), [browser measurements](evidence/mvc-accounts/browser-results.json), [machine-readable99 rows](evidence/mvc-accounts/checklist-results.json). Foundation PASS rows are retained only for their established scope; partial account coverage does not complete criteria spanning future cart/order screens.

## P0

| ID | Status | Evidence / remaining scope |
|---|---|---|
|WEB.BASE.01|NOT RUN|Account controller now calls the in-process adapter; catalog/cart/order controllers remain later steps. No complete15-UC claim.|
|WEB.BASE.02|NOT RUN|Missing/invalid tokens on all five real Account POST routes reject400 with identical SQL; real Quote/Merge await cart PR.|
|WEB.BASE.03|NOT RUN|Account ViewModels/HTML have no forbidden fields; details/cart/order HTML not implemented yet.|
|WEB.BASE.04|NOT RUN|Actual Profile script-like name/address encoded; recipient/cancel/search screens remain later PRs.|
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
|WEB.K06.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K07.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K09.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K09.02|NOT RUN|Inherited association adapter tests pass; actual Catalog/Details + Cart/Add routes await catalog/cart PRs.|
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
|WEB.GATE.01|NOT RUN|Only foundation and Account K01-K05 implemented; catalog/cart/orders/flows not complete.|
|WEB.GATE.02|NOT RUN|Remaining P0 NOT RUN belong to later MVC steps; no overall handover acceptance.|
|WEB.GATE.03|PASS|Full277 SQL QA checks; CI SUCCESS on verified implementation 5cf710722d30b192f95845b65ba7f9260cbd2b27. Latest PR head must still match its successful required check.|
|WEB.GATE.04|PASS|Existing Infrastructure/services/migrations/API Program/Endpoints/Security have empty diff from merged foundation. Only MVC interface/adapter/presentation/tests/docs changed;277 local regression checks pass.|

## P1

| ID | Status | Evidence / remaining scope |
|---|---|---|
|WEB.BASE.08|NOT RUN|Real ADMIN cookie rejected403 on Profile/Password and API unchanged; checkout/cancel await later PRs.|
|WEB.BASE.09|PASS|WAF HTML403/404/500 và500 môi trường Production; thông báo chung +traceId, không stack/private exception. Playwright chụp404 desktop/mobile.|
|WEB.BASE.10|NOT RUN|Inherited formatting unit checks pass; money/order views remain unimplemented.|
|WEB.BASE.11|NOT RUN|Actual account errors mapped to summary/field/redirect/403; cart/checkout/order-specific mappings await later PRs.|
|WEB.BASE.12|NOT RUN|Chromium actual Register/Login double submits each1POST with disabled button, busy label and aria-busy; checkout/cancel deferred.|
|WEB.BASE.13|NOT RUN|Actual account forms preserve safe values and clear secrets on errors; remaining forms deferred.|
|WEB.BASE.14|PASS|Full277 SQL QA checks; CI SUCCESS on verified implementation 5cf710722d30b192f95845b65ba7f9260cbd2b27. Latest PR head must still match its successful required check.|
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
|WEB.K06.02|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K06.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K07.02|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K08.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K08.02|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K08.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K09.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K09.04|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.HOME.01|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
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
|WEB.UI.01|NOT RUN|Four account forms have desktop/390px captures; full UI03–UI07/UI13 screenshots await remaining screens.|
|WEB.GATE.05|NOT RUN|Account screenshots are not complete WEB.FLOW.01–03 purchasing-flow evidence.|
|WEB.GATE.06|PASS|All99 official IDs retained with exactly48P0/43P1/8P2, per-ID evidence/limits and counts.|

## P2

| ID | Status | Evidence / remaining scope |
|---|---|---|
|WEB.K02.05|PASS|Authenticated GET Login/Register redirects home with unchanged session count.|
|WEB.K06.04|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K08.04|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.HOME.02|PASS|StorefrontOptions cấu hình tập trung tên/giới thiệu/liên hệ; Razor render nội dung được cấu hình trong WAF và encode script-like input. Đây là ô cấu hình, không nghiệm thu toàn UI13.|
|WEB.K11.05|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.K12.10|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
|WEB.UI.02|NOT RUN|Account labels, keyboard/form focus and no-overflow verified; entire website keyboard coverage deferred.|
|WEB.UI.03|NOT RUN|Later MVC step; action/screen not yet implemented and not executed.|
