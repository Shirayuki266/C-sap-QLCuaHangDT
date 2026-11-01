# WinForms acceptance ledger — W1–W4

Official08:113 unique IDs, **48 P0 /59 P1 /6 P2**; source SHA256 ae7962fe17cd92aacd1445b203b319c0e6d47859827a9fda6ddbadffca89ae29. W1/W2/W3 PR103/105/107 merged. [W4 report](WINFORMS_W4.md):17 executed IDs (9 P0/8 P1). Exact final-head CI is bound in the PR verification comment. No W4 merge or whole-phase gate approval.

File09 SHA256 3d4fc3bb450801ad00de1c06828dac4759ee4c0c0eb45248e71f9fe9f4428fad requires cumulative partial evidence for multi-stage rows. File10 was supplied in Downloads on2026-11-02, SHA256 30823d9a3de99fd3f03639ed0eaca07160c8da9ea8121a2f55dff370f2847ccb. All24 IDs represented by its nine individual rows and three ranges have been reconciled: BASE13/14/15/16, A09.02 and A12.01 remain partial NOT RUN; A22.06/FLOW01–05/UI01–04/GATE01–06 remain NOT RUN. A05.04 completes at W4 per its explicit requirement; BASE17 is checked each step and requires exact-final-head Ubuntu/Windows CI SUCCESS. Historical executed evidence is retained; no whole-phase gate is accepted.

## Whole phase

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN | N/A |
|---|---:|---:|---:|---:|---:|---:|
|P0|48|32|0|0|16|0|
|P1|59|35|0|0|24|0|
|P2|6|2|0|0|4|0|

## W4 subset

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN |
|---|---:|---:|---:|---:|---:|
|P0|9|9|0|0|0|
|P1|8|8|0|0|0|
|P2|0|0|0|0|0|

All44 remaining NOT RUN IDs and reasons appear individually below. Six named multi-stage rows are explicitly partial. File10 reconciliation is complete for this stage; outstanding W5/W6 scope remains unaccepted.

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
|WIN.BASE.08|W1|PASS|Actual server commits logout then probe loses response: next write blocked before POST; failed reconciliation keeps guard. Lost/malformed login responses are checked via real session GET before explicit retry/recovery. Later domain-command workflows remain their stages.|
|WIN.BASE.09|W1|PASS|STA double-submit while login pending sends one call; Enter routes through actual Form dialog-key handling. Intent key helper keeps immutable in-memory GUID per intention; new intention receives new key. Future domain-key/race proofs remain W4/W5.|
|WIN.BASE.10|W1|PASS|W1 UI/client have no selling-price, inventory, revenue or order-transition authority; form placeholders make no domain claims. Existing services untouched. Future domain presentation will be rechecked as added.|
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
|WIN.A19.01|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A19.02|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A19.03|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A19.04|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A20.01|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A20.02|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A20.03|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A20.04|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A22.01|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.FLOW.01|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.FLOW.02|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.FLOW.03|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.GATE.01|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.GATE.02|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.GATE.03|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.GATE.04|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|

## P1

| ID | Stage | Status | Evidence / remaining scope |
|---|---|---|---|
|WIN.BASE.11|W1|PASS|Bounded page/query builder rejects>100; real supplier no-match reads items/total/page/pageSize. No unsupported domain filters exposed by W1; later screens must retain this boundary.|
|WIN.BASE.12|W1|PASS|Exact23.980.000 đ and UTC+7 next-day boundary asserted; DateOnly request formatter is invariant yyyy-MM-dd. Future money/table displays remain their function tests.|
|WIN.BASE.13|W1–W6|NOT RUN|Partial evidence retained: Actual MainForm navigation has11 named entries,1280x800 minimum, server/user status and honest unimplemented-module text; native sidebar scroll enabled. Real API Main capture retained. File09/10: evidence is partial; full order detail completes W5 for A12.01, remaining modules complete W6 for BASE13/14, writing forms complete W5 for BASE16 followed by final W6 review. File10 now confirms this hold; reconciled2026-11-02.|
|WIN.BASE.14|W1–W6|NOT RUN|Partial evidence retained: Readiness/login/password/logout use async HTTP; held fake login proves live busy state, disabled submit and error recovery on actual controls. Only W1 screens are assessed; later data screens remain NOT RUN. File09/10: evidence is partial; full order detail completes W5 for A12.01, remaining modules complete W6 for BASE13/14, writing forms complete W5 for BASE16 followed by final W6 review. File10 now confirms this hold; reconciled2026-11-02.|
|WIN.BASE.15|W1–W6|NOT RUN|Partial evidence: W1 shared dialog; W2 product/lookup; W3 customer lock/unlock; W4 purchase confirmation and stock counting with production descriptions. Order cancel/delivery-failure/return-receipt confirmations and proof that dismissal sends no request remain W5, final cumulative review W6. NOT RUN per new09.|
|WIN.BASE.16|W1–W6|NOT RUN|Partial evidence retained: Invalid login/settings retain username/configuration; password fields deliberately blank. Safe form errors shown without guessing fields from backend message text. Later domain forms not yet implemented. File09/10: evidence is partial; full order detail completes W5 for A12.01, remaining modules complete W6 for BASE13/14, writing forms complete W5 for BASE16 followed by final W6 review. File10 now confirms this hold; reconciled2026-11-02.|
|WIN.BASE.17|W1|PASS|Independent Client SQL QA checks13 and Windows STA form checks8 pass locally; both CI jobs must be successful. CI SUCCESS snapshot 4b5f3d940fcf0291edeeb9c2da9267355ced47ff; exact final head must be reverified after docs amendment.|
|WIN.BASE.18|W1|PASS|Actual settings form rejects remoteHTTP without losing input; allowlisted URL/environment/fingerprint roundtrip through QA settings file, with no password/cookie/token properties.|
|WIN.BASE.20|W1|PASS|Separate LoginInput/PasswordInput exact camelCase fields asserted; real paged DTO response read correctly. No response object used as request; later DTOs remain future work.|
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
|WIN.A09.02|W3–W5|NOT RUN|Partial evidence: actual customer GET and rendered contact/status/date; working customer-order entry, no credential/hash/role exposure. Full order detail from customer drilldown remains W5 per file10; W6 cumulative review. Earlier W3 evidence retained.|
|WIN.A10.02|W3|PASS|PUT204 then GET authoritative profile, own unchanged contacts accepted.21 real checkout snapshots/payment/history/H50R21/journal remain identical; credential hash privately unchanged.|
|WIN.A11.02|W3|PASS|Competing authenticated admin session changes state; stale expectedActive409 changes no SQL data. Actual form retains state and displays reload message.|
|WIN.A11.03|W3|PASS|ADMIN excluded from actual customer list; ADMIN customer-read/state target404 with SQL unchanged. Invalid-target detail hides lock action, no role input exists.|
|WIN.A12.01|W3–W5|NOT RUN|Partial evidence retained: Real customer orders page20+1, peer empty and read-only order detail GET. Actual button opens fallback details; typed handoff supports future fullW5 without claiming those operations. File09/10: evidence is partial; full order detail completes W5 for A12.01, remaining modules complete W6 for BASE13/14, writing forms complete W5 for BASE16 followed by final W6 review. File10 now confirms this hold; reconciled2026-11-02.|
|WIN.A13.01|W3|PASS|Actual supplier POST201/list by code and PUT204/list, stop/reopen, query/state/paging. Actual new/edit controls and list readback; no DELETE/by-id GET.|
|WIN.A13.02|W3|PASS|Real duplicate supplier code create/update400 rolls back fully. Actual editor retains code/name/contact/status draft and allows correction.|
|WIN.A13.03|W3|PASS|Real active=true selector reads all103 active rows across bounded100 pages and excludes stopped NCC. Actual picker returns active record; W4 purchase integration remains deferred.|
|WIN.A14.03|W4|PASS|Group1 metadata/creator/time/code/status/125M total from server; real desktop40M draft and rendered server summary. [W4 evidence](WINFORMS_W4.md).|
|WIN.A14.04|W4|PASS|Groups4/5 empty draft saves/cancels, empty confirm400/no mutation; actual empty-draft controls. [W4 evidence](WINFORMS_W4.md).|
|WIN.A14.05|W4|PASS|Group6 real >20 drafts, page2/query/status/no-match/read-only SQL; actual paged list, no supplier/date fake filters. [W4 evidence](WINFORMS_W4.md).|
|WIN.A15.04|W4|PASS|Group5 stopped supplier/empty/overflow400 and rollback of earlier line; draft retained, public400 business reason exposed safely; form retains input. [W4 evidence](WINFORMS_W4.md).|
|WIN.A16.01|W4|PASS|Group7 real query/brand/explicit available8 threshold8vs7 and distinct pages; actual H/R/available read-only controls and visible threshold preference. [W4 evidence](WINFORMS_W4.md).|
|WIN.A16.03|W4|PASS|Group10 >20 actual journal entries; variant/key filters, page2, actor/document/time; SQL unchanged and actual read-only journal. [W4 evidence](WINFORMS_W4.md).|
|WIN.A17.04|W4|PASS|Group8 stale stock/catalog version409 with unchanged SQL; actual form retains reason and requests reload. [W4 evidence](WINFORMS_W4.md).|
|WIN.A18.01|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A18.02|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A18.03|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A18.04|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A20.05|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A21.01|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A21.02|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A22.02|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A22.03|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A22.04|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A22.05|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A22.06|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.FLOW.04|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.FLOW.05|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.UI.01|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.UI.02|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.GATE.05|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.GATE.06|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|

## P2

| ID | Stage | Status | Evidence / remaining scope |
|---|---|---|---|
|WIN.BASE.19|W1|PASS|Actual Enter-dialog-key route triggers guarded login; labels/accessibility names, AcceptButton/CancelButton and AutoScaleMode.Dpi present. Shared confirmation starts on dismissal. Full125/150%DPI matrix remains W6.|
|WIN.A02.03|W1|PASS|Actual Main Close attempts fakeAPI logout, respects two-second cancellation bound and disposes form; successful real API logout also verified. Network loss cannot promise remote revocation.|
|WIN.A21.03|W5|NOT RUN|W5: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.A22.07|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.UI.03|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
|WIN.UI.04|W6|NOT RUN|W6: outside delivered W1–W4 scope; implementation and real acceptance evidence deferred to that stage.|
