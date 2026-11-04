# WinForms acceptance ledger — W1–W6

Official08:113 unique IDs, **48 P0 /59 P1 /6 P2**; source SHA256 `ae7962fe17cd92aacd1445b203b319c0e6d47859827a9fda6ddbadffca89ae29`. Official09 SHA256 `3d4fc3bb450801ad00de1c06828dac4759ee4c0c0eb45248e71f9fe9f4428fad`; file10 SHA256 `30823d9a3de99fd3f03639ed0eaca07160c8da9ea8121a2f55dff370f2847ccb`.

W1–W5 PR103/105/107/109/111 merged. [W6 report](WINFORMS_W6.md), [handover](WINFORMS_HANDOVER.md). The26 original NOT RUN gaps were reported individually before coding. Their current status/evidence is reconciled below, including the four cumulative BASE rows. Earlier executed evidence is retained; W6 whole-suite regression revalidates changed Admin paths. BASE17/GATE03 and GATE02 are closed with the required CI verification record; a later head commit requires verification again. No merge or release approval is inferred.

## Whole phase

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN | N/A |
|---|---:|---:|---:|---:|---:|---:|
|P0|48|48|0|0|0|0|
|P1|59|59|0|0|0|0|
|P2|6|6|0|0|0|0|

Remaining NOT RUN: none. All113 IDs PASS, including48 P0; no FAIL/BLOCKED/N/A. [Final head CI verification](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/113#issuecomment-6569365089) must confirm SUCCESS on the actual PR head; initial source CI a223fb6 is separately recorded, never substituted for a newer head. Accepted user qualifications (AI-operated Release walkthrough, native DPI simulation, later human follow-up) are explicit in the handover. Priority grouping is copied from official08, never redefined.

## P0

| ID | Stage | Status | Evidence / remaining scope |
|---|---|---|---|
|WIN.BASE.01|W1|PASS|Four projects built locally Release without warnings/errors; exact Windows/Ubuntu CI evidence required before PASS. CI SUCCESS snapshot 4b5f3d940fcf0291edeeb9c2da9267355ced47ff; exact final head must be reverified after docs amendment.|
|WIN.BASE.02|W1|PASS|Both shipping source/projects scanned; restored Client assets scanned in Ubuntu checks and both application assets in Windows checks. No Core/Web/Infrastructure/EF/SqlClient application reference; test host references scoped to new QA runner.|
|WIN.BASE.03|W1|PASS|Shipping code/settings contain no credential or SQL literal. Cookie jar stays in memory; generated QA credentials absent from captured logs. Password controls clear before await, after errors and on close; no remember option.|
|WIN.BASE.04|W1|PASS|Actual missingCSRF400 and deliberately stripped-token400 cause no session write/retry; successful login/logout fetch a fresh token for each POST and share in-memory cookies.|
|WIN.BASE.05|W1|PASS|HTTP nonloopback, credentials, schemes, query/fragment/path targets rejected; HTTPS and loopback accepted. Default certificate checking, no certificate bypass.|
|WIN.BASE.06|W1|PASS|Real business failures plus controlled transport injections cover403/404/409/500trace/503, malformedJSON, network/timeout. Backend/private body text never displayed; global UI failure handlers are generic. Malformed success reconciles before another write.|
|WIN.BASE.07|W1|PASS|Actual SQL session expiry yields401 and prevents subsequent POST. Actual production application context, driven with fakeAPI on STA, closes Main and returns Login on session event; password/session values are not carried across.|
|WIN.BASE.08|W1|PASS|Actual server commits logout then probe loses response: next write blocked before POST; failed reconciliation keeps guard. Lost/malformed login responses are checked via real session GET before explicit retry/recovery. W2–W5 domain writes add authoritative readback and guarded same-intention retries; whole-suite checks revalidated at W6.|
|WIN.BASE.09|W1|PASS|STA double-submit while login pending sends one call; Enter routes through actual Form dialog-key handling. Intent key helper keeps immutable in-memory GUID per intention; new intention receives new key. W4/W5 real key/retry/race proofs are delivered, with two-Admin W6 contention added.|
|WIN.BASE.10|W1|PASS|W1–W6 UI/client keep server authority for selling price, stock, revenue and transitions. Preview values are labelled estimates; report monthly grouping projects server daily rows. Display action policy is independently checked against the server matrix; services remain untouched.|
|WIN.A01.01|W1|PASS|Actual readiness environment/fingerprint shown in real Login capture. Wrong expected environment/fingerprint prevents POST.503 is mapped in controlled transport proof.|
|WIN.A01.02|W1|PASS|Actual invalid credentials, CUSTOMER and locked account get generic feedback and leave session row count unchanged; valid ADMIN opens real MainForm.|
|WIN.A01.03|W1|PASS|Actual controls clear submitted password before pending task; error/close clears again; shipping settings and code have no default credential or remember-password option.|
|WIN.A02.01|W1|PASS|Actual client and real WinForms/Kestrel logout revoke current SQL session and clear local identity/cookies; application context returns Login and closes owned screens.|
|WIN.A03.01|W1|PASS|Two real ADMIN sessions; valid own-password change updates hash and deletes every SQL session; old session401, old password rejected, new password accepted.|
|WIN.A05.01|W2|PASS|Real POST201 -> GET; two variants server price12,000,000; SQL initial H0/R0, no journal. Actual QA ProductEditor creates and reads back independently.|
|WIN.A05.02|W2|PASS|Real duplicate code/SKU/variant-combination and invalid cost/rates/images rejected400 with identical ordered SQL snapshots; actual form retains fields/variants and re-enables save.|
|WIN.A06.01|W2|PASS|GET -> dedicated complete PUT with old IDs/versions; direct incomplete PUT400 leaves SQL unchanged. Cost/price read-only, old cost retained by DTO binder; new variant zero stock.|
|WIN.A06.02|W2|PASS|Real stale PUT409 CONCURRENT_UPDATE after competing valid update; rejected SQL snapshot unchanged, form retains draft and tells user to reload.|
|WIN.A07.01|W2|PASS|GET full aggregate -> confirmed PUT active=false/true -> GET; cancellation sends no write. Product/history retained; variant state edits also ask confirmation.|
|WIN.A08.02|W2|PASS|Real duplicate create/update lookup400 leaves SQL unchanged; stopping does not remove product/orders/history. No delete operation.|
|WIN.A10.01|W3|PASS|Exactly four profile request fields; real duplicate phone/email and bad syntax400 are atomic. Actual editor has four TextBoxes/no state/role/password field and retains rejected draft.|
|WIN.A11.01|W3|PASS|Confirmed active+expectedActive command tested. Lock deletes2 actual customer sessions; both old clients401, old write401; unlock restores0 sessions; fresh login required. UI explains both consequences.|
|WIN.A14.01|W4|PASS|Groups1/2/11: active supplier/unique positive lines, POST201/requestKey/GET; same-key201 sameID, unknown response identical-body recovery only; actual controls. [W4 evidence](WINFORMS_W4.md).|
|WIN.A14.02|W4|PASS|Groups2/3/4: whole-line PUT/version; stale and finalized rejected with identical SQL snapshots; final-state controls read-only. [W4 evidence](WINFORMS_W4.md).|
|WIN.A15.01|W4|PASS|Group3 gated two-connection confirm race204/409; exact H+10/+5, server cost8M/9M and price9.6M/11.25M; real UI confirmation and SQL H0->4. [W4 evidence](WINFORMS_W4.md).|
|WIN.A15.02|W4|PASS|Groups4/12 empty and nonempty cancel with version+operationKey; no stock/cost/price changes; uncertain reply GET recovery without resend. [W4 evidence](WINFORMS_W4.md).|
|WIN.A15.03|W4|PASS|Group3 two actual concurrent requests, one effect; repeat/stale/finalized rejected atomically. Held form command double-click sends once. [W4 evidence](WINFORMS_W4.md).|
|WIN.A16.02|W4|PASS|Group7 actual SQL THROW51001 on stock read ->500 SERVER_ERROR/trace, never stock0; no-match success separate; actual empty/error captures. [W4 evidence](WINFORMS_W4.md).|
|WIN.A17.01|W4|PASS|Group8 distinct variantId/productId and stock/catalog versions, absolute12/reason/key; native confirmation and real H4->5 SQL readback. [W4 evidence](WINFORMS_W4.md).|
|WIN.A17.02|W4|PASS|Group8 negative/belowR/sameH rejected with unchanged SQL; actual reason/belowR validation and read-only stock grid. [W4 evidence](WINFORMS_W4.md).|
|WIN.A17.03|W4|PASS|Groups8/9 stable key, old409/same-count400/reused-key409; committed lost reply resolves via journal operationKey, no second effect; new intention new key. [W4 evidence](WINFORMS_W4.md).|
|WIN.A19.01|W5|PASS|Independent section4 literal oracle vs display policy and all6 form states; customer-cancel absent from Admin, return only failed/unreceived, invoice only permitted states. [W5 evidence](WINFORMS_W5.md).|
|WIN.A19.02|W5|PASS|Matrix waiting/confirm204 -> confirmed GET/version/history, H/R unchanged and COD unpaid; actual native QA form confirm checkpoint. [W5 evidence](WINFORMS_W5.md).|
|WIN.A19.03|W5|PASS|Matrix both cancel states and actual native cancel, H unchanged/R reduced, COD unpaid. Required reason beside input; dismissed protected confirmation sends0 writes. [W5 evidence](WINFORMS_W5.md).|
|WIN.A19.04|W5|PASS|SQL group3 real two-connection barrier customer-cancel204 wins/admin-confirm409; client GET resolves cancelled. Actual form409 reload/retention and failed-readback locks tested. [W5 evidence](WINFORMS_W5.md).|
|WIN.A20.01|W5|PASS|48 actual API matrix cells validate two-variant H/R/COD/history/journal effects; dedicated version/key/reason DTOs, optional/trimmed/overlong return reason; real desktop transitions. [W5 evidence](WINFORMS_W5.md).|
|WIN.A20.02|W5|PASS|Matrix completed payment amount=total and collectedAt=completedAt; other states unpaid0; all6 rendered states, real completion COD24M checkpoint. Display derives from payment/total/completedAt. [W5 evidence](WINFORMS_W5.md).|
|WIN.A20.03|W5|PASS|SQL group2 null/blank/trim return succeeds once; stale409 and fresh-version repeat409 leave full snapshot unchanged. Returned flag hides button; real physical-receipt dialog and H+2 once. [W5 evidence](WINFORMS_W5.md).|
|WIN.A20.04|W5|PASS|48 actual API cells:11 allowed/37 denied, full denied snapshots identical; independent literal matrix policy and forced invalid form commands report error/refreshed data. [W5 evidence](WINFORMS_W5.md).|
|WIN.A22.01|W6|PASS|W6: Real report SQL, UTC+7 start/end boundary fixture: previous tick/next-day boundary excluded, first/last tick included; bad/missing dates blocked before HTTP; field error retained. See [W6 evidence](WINFORMS_W6.md).|
|WIN.FLOW.01|W6|PASS|W6: Actual Release UI creates phone/cost/profit, purchase10 at8M → server9.6M, checkout2 reserves2, confirm/dispatch/complete, report19.2M.19-stage observer and step pictures below. See [W6 evidence](WINFORMS_W6.md).|
|WIN.FLOW.02|W6|PASS|W6: Release UI failed-delivery/receive/cancel flow; H6→8 once; confirmed cancellation R2→0, H8 unchanged, COD uncollected. Client repeat rejection leaves SQL unchanged. See [W6 evidence](WINFORMS_W6.md).|
|WIN.FLOW.03|W6|PASS|W6: Real gated requests from two distinct Admin accounts and SQL connections; one succeeds, loser409 CONCURRENT_UPDATE; authoritative refresh, no overwrite. W5 form conflict feedback remains covered. See [W6 evidence](WINFORMS_W6.md).|
|WIN.GATE.01|W6|PASS|W6: A01–A22 screens and UI/UC links now delivered across W1–W6, with retained previous proofs plus W6 actual UI/report evidence. See [W6 evidence](WINFORMS_W6.md).|
|WIN.GATE.02|W1–W6|PASS|All48 official P0 rows PASS after their executable/SQL/UI proofs and required CI verification; no P0 FAIL/BLOCKED/NOT RUN. [Verification](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/113#issuecomment-6569365089).|
|WIN.GATE.03|W1–W6|PASS|Ubuntu and Windows required jobs SUCCESS; exact final head/run bound in the PR verification record. A later commit requires revalidation. [Verification](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/113#issuecomment-6569365089).|
|WIN.GATE.04|W6|PASS|W6: Diff against base is empty for Core/Infrastructure/Web/old Checks; no schema/migration/API/customer MVC change. See [W6 evidence](WINFORMS_W6.md).|

## P1

| ID | Stage | Status | Evidence / remaining scope |
|---|---|---|---|
|WIN.BASE.11|W1|PASS|Bounded page/query builder rejects>100; real supplier no-match reads items/total/page/pageSize. W2–W6 screens retain the supported-filter boundary; no invented supplier/date receipt filter or returnReceived server filter.|
|WIN.BASE.12|W1|PASS|Exact23.980.000 đ and UTC+7 next-day boundary asserted; DateOnly request formatter is invariant yyyy-MM-dd. W2–W6 money/table values and W5/W6 inclusive UTC+7 boundaries are verified in their form/SQL groups.|
|WIN.BASE.13|W6|PASS|W6: Every module connected; navigation/status/minimum-window proofs plus actual MainForm/dashboard and56 form/tab DPI captures. See [W6 evidence](WINFORMS_W6.md).|
|WIN.BASE.14|W6|PASS|W6: Cumulative W1–W5 async/busy/empty/error cases retained; new report/overview errors distinguish empty0 from actual errors, metrics load separately. Source search finds no blocking .Result/.Wait calls. See [W6 evidence](WINFORMS_W6.md).|
|WIN.BASE.15|W6|PASS|W6: Cumulative dialogs plus new full dismissal suite: product, brand, category, customer lock/unlock, purchase confirm, count, cancel/failure/return; zero writes when dismissed. See [W6 evidence](WINFORMS_W6.md).|
|WIN.BASE.16|W6|PASS|W6: Retained writing-form error/retention tests plus W6 report dates and errors; password fields deliberately clear; no field guessing from combined error strings. See [W6 evidence](WINFORMS_W6.md).|
|WIN.BASE.17|W1–W6|PASS|Both required jobs SUCCESS: real Admin.Client SQL checks on Ubuntu and STA form/DPI checks on Windows. Exact final head/run bound in the PR verification record. [Verification](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/113#issuecomment-6569365089).|
|WIN.BASE.18|W1|PASS|Actual settings form rejects remoteHTTP without losing input; allowlisted URL/environment/fingerprint roundtrip through QA settings file, with no password/cookie/token properties.|
|WIN.BASE.20|W1|PASS|Separate LoginInput/PasswordInput exact camelCase fields asserted; real paged DTO response read correctly. W2–W6 use dedicated request records for products, contacts, purchase/count and order commands; the report response is read-only.|
|WIN.A01.04|W1|PASS|Actual postcommit lost login response blocks blind resend; real session GET restores explicit retry path. Network error form re-enables actions; malformed success can recover received session without second login.|
|WIN.A01.05|W1|PASS|Actual Enter key handling and repeated PerformClick during held login produce one call and readable external progress label.|
|WIN.A02.02|W1|PASS|Actual already-revoked session logout safely clears local state; repeated logout is harmless.|
|WIN.A03.02|W1|PASS|Actual wrong current/short/overlong new password rejected with hash unchanged; form error keeps fields empty and shows controlled summary.|
|WIN.A03.03|W1|PASS|No accountId/role/foreign target in PasswordInput or UI; test logs exclude generated passwords; form only invokes self-password API.|
|WIN.A04.01|W2|PASS|Actual query/brand/state paging and distinct page2; UI controls list inactive rows/names from ADMIN.20 default, bounded100 lookup pages; no invented category filter.|
|WIN.A04.02|W2|PASS|Actual ADMIN detail DTO and server price/cost/profit/H/R/version; actual STA details/editor show description/image URLs and available presentation. Real QA captures retained.|
|WIN.A04.03|W2|PASS|Actual404 and no-match wrapper; form fake503 displays genuine failure feedback rather than an empty/zero success.|
|WIN.A05.03|W2|PASS|Client required code/name/SKU/color, positive storage/cost/integer suggested quantity and percent bounds asserted; actual controls block invalid draft, server independently rejects bad values.|
|WIN.A05.04|W4|PASS|API/SQL group13; actual default modal handoff and real five-stage desktop SQL prove suggestions local until explicit draft save, no stock effect. [W4 evidence](WINFORMS_W4.md).|
|WIN.A05.05|W2|PASS|Main image optional; HTTPS and /images accepted; invalid schemes/credentials/traversal/blank Images[]/overlong rejected; form retains invalid URL without sending.|
|WIN.A06.03|W2|PASS|Actual checkout snapshot before rate PUT retains line price12,000,000/payment/history/H5R1/journal; live server price12,500,000 after25%. Form renders returned price.|
|WIN.A06.04|W2|PASS|No variant delete action or DELETE request; grid disallows row deletion, retains every old variant and offers active state only.|
|WIN.A07.02|W2|PASS|Actual inactive filter and rendered text Đã ngừng; status does not rely on color.|
|WIN.A08.01|W2|PASS|Actual lookup POST201/list and PUT204/list, dedicated name+active, ADMIN inactive retention/paging; actual form new/edit selection paths tested.|
|WIN.A08.03|W2|PASS|Actual confirmation cancellation blocks write, warning explicitly mentions website visibility; actual public detail404 when either brand/category inactive, ADMIN retained.|
|WIN.A09.01|W3|PASS|Real query name/phone/email, state, page1/page2 and no-match; safe customer DTO has no credential/hash/role fields. Actual list controls and empty/error states rendered.|
|WIN.A09.02|W5|PASS|W3 customer/contact read evidence retained; W5 actual production customer->orders->full detail handoff test shows actor history/actions, no writes. Real order detail SQL reads separately proven. [W5 evidence](WINFORMS_W5.md).|
|WIN.A10.02|W3|PASS|PUT204 then GET authoritative profile, own unchanged contacts accepted.21 real checkout snapshots/payment/history/H50R21/journal remain identical; credential hash privately unchanged.|
|WIN.A11.02|W3|PASS|Competing authenticated admin session changes state; stale expectedActive409 changes no SQL data. Actual form retains state and displays reload message.|
|WIN.A11.03|W3|PASS|ADMIN excluded from actual customer list; ADMIN customer-read/state target404 with SQL unchanged. Invalid-target detail hides lock action, no role input exists.|
|WIN.A12.01|W5|PASS|W3 real customer order paging20+1/empty evidence retained; W5 production handoff now opens full OrderDetailForm with history/COD/version. Typed event is connected in CustomerDetailsForm; no writes on view. [W5 evidence](WINFORMS_W5.md).|
|WIN.A13.01|W3|PASS|Actual supplier POST201/list by code and PUT204/list, stop/reopen, query/state/paging. Actual new/edit controls and list readback; no DELETE/by-id GET.|
|WIN.A13.02|W3|PASS|Real duplicate supplier code create/update400 rolls back fully. Actual editor retains code/name/contact/status draft and allows correction.|
|WIN.A13.03|W3|PASS|Real active=true selector reads all103 active rows across bounded100 pages and excludes stopped NCC. Actual picker returns active record; W4 purchase integration and W6 actual Release receipt selection/confirmation are delivered.|
|WIN.A14.03|W4|PASS|Group1 metadata/creator/time/code/status/125M total from server; real desktop40M draft and rendered server summary. [W4 evidence](WINFORMS_W4.md).|
|WIN.A14.04|W4|PASS|Groups4/5 empty draft saves/cancels, empty confirm400/no mutation; actual empty-draft controls. [W4 evidence](WINFORMS_W4.md).|
|WIN.A14.05|W4|PASS|Group6 real >20 drafts, page2/query/status/no-match/read-only SQL; actual paged list, no supplier/date fake filters. [W4 evidence](WINFORMS_W4.md).|
|WIN.A15.04|W4|PASS|Group5 stopped supplier/empty/overflow400 and rollback of earlier line; draft retained, public400 business reason exposed safely; form retains input. [W4 evidence](WINFORMS_W4.md).|
|WIN.A16.01|W4|PASS|Group7 real query/brand/explicit available8 threshold8vs7 and distinct pages; actual H/R/available read-only controls and visible threshold preference. [W4 evidence](WINFORMS_W4.md).|
|WIN.A16.03|W4|PASS|Group10 >20 actual journal entries; variant/key filters, page2, actor/document/time; SQL unchanged and actual read-only journal. [W4 evidence](WINFORMS_W4.md).|
|WIN.A17.04|W4|PASS|Group8 stale stock/catalog version409 with unchanged SQL; actual form retains reason and requests reload. [W4 evidence](WINFORMS_W4.md).|
|WIN.A18.01|W5|PASS|SQL group4 query branches/status/paging and exact UTC+7 start/inclusive-end boundaries; actual list filters/pending pager/empty/error controls. [W5 evidence](WINFORMS_W5.md).|
|WIN.A18.02|W5|PASS|SQL matrix/read/invoice authoritative DTO and real detail all6 states; separate COD/derived amount/time, snapshot lines, actor history, version/return flag; no invented response fields. [W5 evidence](WINFORMS_W5.md).|
|WIN.A18.03|W5|PASS|SQL group4 unchanged before/after list/detail/invoice; real desktop9 checkpoint observer, invoice and preview snapshots byte-identical. [W5 evidence](WINFORMS_W5.md).|
|WIN.A18.04|W5|PASS|SQL group5 real CUSTOMER request403; Admin.Client403 mapping and actual list form forbidden feedback, no crash. [W5 evidence](WINFORMS_W5.md).|
|WIN.A20.05|W5|PASS|Actual cancel/fail missing-reason400 atomic; both form paths reject beside input and retain reason after error; optional return blank succeeds. [W5 evidence](WINFORMS_W5.md).|
|WIN.A21.01|W5|PASS|Real invoice GET only3 allowed states in matrix, exact title/notice retained in viewer/native installed-driver PrintPreview; forbidden state fails cleanly. [W5 evidence](WINFORMS_W5.md).|
|WIN.A21.02|W5|PASS|SQL matrix and group4 invoice reads unchanged; real desktop completed/invoice-read/preview-closed full SQL identical; server line/recipient/total snapshots. [W5 evidence](WINFORMS_W5.md).|
|WIN.A22.02|W6|PASS|W6: Actual variant bestsellers and stock; stock request has kind only and ignores invalid/irrelevant dates. See [W6 evidence](WINFORMS_W6.md).|
|WIN.A22.03|W6|PASS|W6: Same AdminReport source instance for table/chart; monthly sums daily; server total34.6M from two variants without double counting; variant IDs remain separate. See [W6 evidence](WINFORMS_W6.md).|
|WIN.A22.04|W6|PASS|W6: Actual empty report0, forced real SQL stock-read error500 with safe trace; error never becomes stock0. Fake form error clears previous totals/rows. See [W6 evidence](WINFORMS_W6.md).|
|WIN.A22.05|W6|PASS|W6: Server status totals, period/today and7 daily points;105 additional failed orders spanning pages, returned flags excluded; injected page2 failure yields error, no partial94; other tasks still succeed. See [W6 evidence](WINFORMS_W6.md).|
|WIN.A22.06|W6|PASS|W6: Production dashboard button opens waiting-order filter or explicit-threshold stock filter; changed filter invalidates old metric. See [W6 evidence](WINFORMS_W6.md).|
|WIN.FLOW.04|W6|PASS|W6: Actual Release draft quantity7, controlled QA session expiry, save401, clear unsaved-draft warning then login; no extra purchase/order/stock/history/payment/journal effect. See [W6 evidence](WINFORMS_W6.md).|
|WIN.FLOW.05|W6|PASS|W6: Real absolute count lowers variant to3, R0, Available3; explicit low-stock3 query/metric includes it. See [W6 evidence](WINFORMS_W6.md).|
|WIN.UI.01|W6|PASS|W6: 56 actual STA form/tab renders at local125% and native-event150%, UI01/02/08–12 and auxiliary forms, all1280×800;21 additional Release walkthrough captures. See [W6 evidence](WINFORMS_W6.md).|
|WIN.UI.02|W6|PASS|W6: Automatic control bounds and label/button/date-text+chrome checks, grids scroll; independent review found date clipping, fixed and recaptured successfully. See [W6 evidence](WINFORMS_W6.md).|
|WIN.GATE.05|W6|PASS|W6: User-accepted agent-assisted UI walkthrough: actual built Release app, isolated real backend/SQL,19 checkpoints and21 pictures. No form harness or fake API. User will manually run additionally. See [W6 evidence](WINFORMS_W6.md).|
|WIN.GATE.06|W6|PASS|W6: Official priority count, per-ID ledger, evidence, accepted differences/limits and WINFORMS_HANDOVER provided. See [W6 evidence](WINFORMS_W6.md).|

## P2

| ID | Stage | Status | Evidence / remaining scope |
|---|---|---|---|
|WIN.BASE.19|W1|PASS|Actual Enter-dialog-key route triggers guarded login; labels/accessibility names, AcceptButton/CancelButton and AutoScaleMode.Dpi present. Shared confirmation starts on dismissal. Full125/150%DPI matrix remains W6.|
|WIN.A02.03|W1|PASS|Actual Main Close attempts fakeAPI logout, respects two-second cancellation bound and disposes form; successful real API logout also verified. Network loss cannot promise remote revocation.|
|WIN.A21.03|W5|PASS|Deterministic no-installed-printer service test on STA keeps invoice content and shows guidance without calling preview; driver-exception branch recovers. Separate local real installed-driver preview captured; no physical print required. [W5 evidence](WINFORMS_W5.md).|
|WIN.A22.07|W6|PASS|W6: Report20-row pages from cached result, selected bar/daily value label, monthly grouping without refetch; table/chart use identical page rows. See [W6 evidence](WINFORMS_W6.md).|
|WIN.UI.03|W6|PASS|W6: Native Tab username→password, Enter/Cancel defaults, Escape closes modal without a write, CtrlS saves exactly once; geometry-based tab order, named inputs and chart arrow keys. See [W6 evidence](WINFORMS_W6.md).|
|WIN.UI.04|W6|PASS|W6: Vietnamese text states/empty/loading/errors remain alongside color; labels show order state and COD separately, selected chart values are textual. See [W6 evidence](WINFORMS_W6.md).|
