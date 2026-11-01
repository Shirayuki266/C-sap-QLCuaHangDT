# WinForms acceptance ledger — W1–W2

Official latest08:113 unique IDs, **48 P0 /59 P1 /6 P2**. Source SHA256 ae7962fe17cd92aacd1445b203b319c0e6d47859827a9fda6ddbadffca89ae29. W1 accepted PR103; W2 has16 executed IDs (6 P0/10 P1). [W2 report](WINFORMS_W2.md); [W1 historical report](WINFORMS_W1.md). Exact final-head CI is bound in PR verification.

## Whole phase

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN | N/A |
|---|---:|---:|---:|---:|---:|---:|
|P0|48|21|0|0|27|0|
|P1|59|23|0|0|36|0|
|P2|6|2|0|0|4|0|

## W2 subset

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN | N/A |
|---|---:|---:|---:|---:|---:|---:|
|P0|6|6|0|0|0|0|
|P1|10|10|0|0|0|0|
|P2|0|0|0|0|0|0|

All67 remaining NOT RUN IDs are listed below with scope/reasons. A05.04 remains W4; BASE15 remains partial until later confirmations. No whole-phase gate is passed.

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
|WIN.A10.01|W3|NOT RUN|W3: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A11.01|W3|NOT RUN|W3: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A14.01|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A14.02|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A15.01|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A15.02|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A15.03|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A16.02|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A17.01|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A17.02|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A17.03|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A19.01|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A19.02|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A19.03|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A19.04|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A20.01|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A20.02|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A20.03|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A20.04|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A22.01|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.FLOW.01|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.FLOW.02|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.FLOW.03|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.GATE.01|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.GATE.02|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.GATE.03|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.GATE.04|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|

## P1

| ID | Stage | Status | Evidence / remaining scope |
|---|---|---|---|
|WIN.BASE.11|W1|PASS|Bounded page/query builder rejects>100; real supplier no-match reads items/total/page/pageSize. No unsupported domain filters exposed by W1; later screens must retain this boundary.|
|WIN.BASE.12|W1|PASS|Exact23.980.000 đ and UTC+7 next-day boundary asserted; DateOnly request formatter is invariant yyyy-MM-dd. Future money/table displays remain their function tests.|
|WIN.BASE.13|W1|PASS|Actual MainForm navigation has11 named entries,1280x800 minimum, server/user status and honest unimplemented-module text; native sidebar scroll enabled. Real API Main capture retained.|
|WIN.BASE.14|W1|PASS|Readiness/login/password/logout use async HTTP; held fake login proves live busy state, disabled submit and error recovery on actual controls. Only W1 screens are assessed; later data screens remain NOT RUN.|
|WIN.BASE.15|W1|NOT RUN|Partial W1 shared dialog plus W2 product/variant/lookup confirmations proven; customer/order/purchase/stock integrations remain W3–W5, so whole criterion stays NOT RUN.|
|WIN.BASE.16|W1|PASS|Invalid login/settings retain username/configuration; password fields deliberately blank. Safe form errors shown without guessing fields from backend message text. Later domain forms not yet implemented.|
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
|WIN.A05.04|W4|NOT RUN|W4 NOT RUN: typed purchase handoff/event exists, button disabled; actual draft opening/saving/stock workflow not implemented or accepted.|
|WIN.A05.05|W2|PASS|Main image optional; HTTPS and /images accepted; invalid schemes/credentials/traversal/blank Images[]/overlong rejected; form retains invalid URL without sending.|
|WIN.A06.03|W2|PASS|Actual checkout snapshot before rate PUT retains line price12,000,000/payment/history/H5R1/journal; live server price12,500,000 after25%. Form renders returned price.|
|WIN.A06.04|W2|PASS|No variant delete action or DELETE request; grid disallows row deletion, retains every old variant and offers active state only.|
|WIN.A07.02|W2|PASS|Actual inactive filter and rendered text Đã ngừng; status does not rely on color.|
|WIN.A08.01|W2|PASS|Actual lookup POST201/list and PUT204/list, dedicated name+active, ADMIN inactive retention/paging; actual form new/edit selection paths tested.|
|WIN.A08.03|W2|PASS|Actual confirmation cancellation blocks write, warning explicitly mentions website visibility; actual public detail404 when either brand/category inactive, ADMIN retained.|
|WIN.A09.01|W3|NOT RUN|W3: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A09.02|W3|NOT RUN|W3: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A10.02|W3|NOT RUN|W3: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A11.02|W3|NOT RUN|W3: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A11.03|W3|NOT RUN|W3: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A12.01|W3|NOT RUN|W3: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A13.01|W3|NOT RUN|W3: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A13.02|W3|NOT RUN|W3: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A13.03|W3|NOT RUN|W3: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A14.03|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A14.04|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A14.05|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A15.04|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A16.01|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A16.03|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A17.04|W4|NOT RUN|W4: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A18.01|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A18.02|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A18.03|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A18.04|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A20.05|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A21.01|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A21.02|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A22.02|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A22.03|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A22.04|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A22.05|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A22.06|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.FLOW.04|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.FLOW.05|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.UI.01|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.UI.02|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.GATE.05|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.GATE.06|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|

## P2

| ID | Stage | Status | Evidence / remaining scope |
|---|---|---|---|
|WIN.BASE.19|W1|PASS|Actual Enter-dialog-key route triggers guarded login; labels/accessibility names, AcceptButton/CancelButton and AutoScaleMode.Dpi present. Shared confirmation starts on dismissal. Full125/150%DPI matrix remains W6.|
|WIN.A02.03|W1|PASS|Actual Main Close attempts fakeAPI logout, respects two-second cancellation bound and disposes form; successful real API logout also verified. Network loss cannot promise remote revocation.|
|WIN.A21.03|W5|NOT RUN|W5: outside W1; no implementation or real acceptance evidence yet.|
|WIN.A22.07|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.UI.03|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
|WIN.UI.04|W6|NOT RUN|W6: outside W1; no implementation or real acceptance evidence yet.|
