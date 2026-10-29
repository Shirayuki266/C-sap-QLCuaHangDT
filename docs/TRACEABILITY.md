# Requirements trace for the clean rebuild

Source: the revised phone-store report, section 2.3.12 (37 detailed tables), together with sections 1.5.2.1, 2.4 and appendix A. The private report and option A prototype remain local. The user explicitly requested removal and replacement of the legacy code on 2026-10-30. Legacy test reports and source references in the Word document are not evidence for this rebuild.

## Trace identifiers and evidence

K01-K15 and A01-A22 are original source identifiers. `B1` through `B5` below are generated trace labels for the five numbered basic steps in each table: open, display, supply input/select action, server processing, display result. `PRE`, `POST` and `BR1` label the corresponding source fields. `E1` is a generated label for the unnumbered exception field; a row can contain multiple exception cases. `A1`/`A2` retain the original alternative identifiers when present. `G01`-`G08` are generated labels for shared requirements, not additional source use cases.

Every row traces B1-B5, PRE, POST, BR1 and E1. Its server column details B4; the UI column owns B1-B3/B5 and remains deferred. All scenarios in the last column are **planned verification**, not existing or passing tests. A source field saying there is no separate alternative creates no extra branch to implement. Trigger = the actor selects the function; availability of the application/data connection is a shared assumption. Connection failures remain errors, never empty successful lists.

## Customer requirements

All customer mutations run on the server except the guest browser cart. Record IDs and actors come from authenticated server context. The proposed service names describe future boundaries, not code that already exists.

| ID / future UI | PRE and actor | Server/data and POST | Alternative / exception | Planned service verification |
| --- | --- | --- | --- | --- |
| K01 / website registration | Guest, not signed in | Account service validates username/password/name/phone/optional email; creates CUSTOMER account + profile atomically; no automatic login | A1 correct duplicate input and retry; E1 invalid or duplicate username/phone/email | Hash only; CUSTOMER cannot be overposted; concurrent duplicate registrations leave one account/profile |
| K02 / website login | Guest with active account | Account service verifies hash/state; creates stored session and cookie | E1 invalid credentials or locked account | Invalid requests create no session; session expires at configured source-backed 8 hours |
| K03 / website logout | Signed-in customer | Account service revokes only the current session and ends login | E1 expired session | Revoked cookie cannot access authenticated endpoints; another valid session is unaffected |
| K04 / website profile | Signed-in customer | Account service validates and updates only the caller's name/phone/email/address | E1 invalid/duplicate phone or email | Actor cannot be supplied by client; uniqueness failures preserve profile |
| K05 / website password | Signed-in customer | Verify current password; hash replacement and revoke all related sessions; require login again | E1 wrong current or invalid new password | Existing cookies stop working; failed change preserves password and sessions |
| K06 / website catalog | Guest or customer, website available | Catalog service reads only public active products/variants; no write | E1 no products is a valid empty result | Disabled product/brand/category/variant excluded; stable pagination |
| K07 / website search | Guest or customer | Catalog service applies keyword matching; no write | E1 no result with UI guidance to revise keyword | Name/brand/keyword cases, empty results, bounded search input |
| K08 / website filters | Guest or customer | Catalog service combines brand/price/storage/color/sort conditions; no write | A1 clear filters; E1 invalid range or no matches | Conditions apply to the same matching variant; stable sorted pages; clear filter restores unfiltered query |
| K09 / website detail | Guest or customer, public product | Catalog service reads images/specifications and variant price/availability; no write | E1 missing/discontinued product | Active variants only; availability = physical minus reserved |
| K10 / website add to cart | Guest or customer, sellable variant | Cart service validates quantity and availability; updates browser guest cart or persisted customer cart; no stock reservation | A1 guest cart can merge on login; E1 invalid quantity or insufficient availability | Guest preview untrusted; merging adds matching quantities atomically, rejects overflow/shortage without losing input |
| K11 / website cart | Guest or customer, cart can be opened | Read cart, set quantity or delete a line; server recalculates displayed totals; no reservation | A1 delete line; E1 insufficient stock or malformed input | Quantity/type/bounds checks; deletion does not affect inventory |
| K12 / website COD checkout | CUSTOMER, nonempty persisted cart | Order service rechecks recipient/phone/address, confirmed quantities/prices and stock; snapshots order/lines/recipient, unpaid COD, reservation/history/journal and cart deletion in one transaction | A1 show changed prices and obtain confirmation; E1 changed price, shortage, invalid recipient/payment or transaction failure | Rollback after one line; last-unit race; double submit and retry after commit return one owned order |
| K13 / website order history | CUSTOMER | Order queries filter only the caller's orders by date/state; no write | A1 change filters or open detail; E1 no orders is empty | Another customer's orders never enter results; inclusive local date/exclusive next day |
| K14 / website order detail | CUSTOMER owning order | Order queries return immutable snapshots, payment state and timeline; no write | E1 missing/not owned order | Nonowner gets 404 without disclosing recipient or lines |
| K15 / website cancel | CUSTOMER owning pending order | Check owner/state/version and reason; cancel, release reserved quantity and append history/journal atomically | E1 missing reason, disallowed state or stale version | Confirmation/cancellation race; repeated cancel; physical stock unchanged |

## Administrator requirements

Every row requires a live active ADMIN session. WinForms calls the same backend through HTTP; it never accesses SQL Server directly.

| ID / future UI | Additional PRE | Server/data and POST | Alternative / exception | Planned service verification |
| --- | --- | --- | --- | --- |
| A01 / WinForms login | Ready backend, active ADMIN account | Account service verifies password and ADMIN role, creates session | E1 credentials/role/locked/network failure | CUSTOMER and locked accounts denied; no session on failure |
| A02 / WinForms logout | Signed in | Revoke current session and return to login | E1 expired session/network failure | Old cookie denied after successful logout; network failure not presented as server success |
| A03 / WinForms password | Signed in | Verify current password, save new hash and revoke all sessions | E1 wrong current password/invalid input | All former ADMIN sessions fail after change |
| A04 / WinForms product list | Signed in | Internal catalog query includes discontinued products; no write | E1 empty result | Guest/customer cannot request internal visibility |
| A05 / WinForms product editor | Valid brand/category | Catalog service validates code/SKU/cost/percentage, saves product + variants with zero inventory | A1 optionally continue to a separate purchase draft; E1 invalid/duplicate code/SKU | Unique SKU and product/color/storage; correct decimal formula; zero stock even if client submits stock |
| A06 / WinForms product editor | Existing product | Check data and versions; preserve old variants; calculate price from current cost and percentage; preserve order snapshots | E1 invalid/duplicate input or stale version | Ignore forged sale price/current cost on existing variants; changing percentage recalculates; stale common edits rejected |
| A07 / WinForms product state | Existing product | Update trading state without deleting history; public catalog reflects state | A1 reopen discontinued product; E1 missing product/stale version | Concurrent edits rejected; old order lines unchanged; new purchase attempts denied while disabled |
| A08 / WinForms brands/categories | Signed in | Add/edit/disable lookups while retaining references | A1 choose add/edit/disable for each type; E1 duplicate name/invalid data | No hard delete of referenced rows; disabled lookup excluded from new public sales |
| A09 / WinForms customer search | Signed in | Search name/phone/email; read customer profiles | E1 no results is empty | ADMIN only; no password/hash/session tokens returned |
| A10 / WinForms customer editor | Existing customer | Validate profile and unique phone/email; persist changes | E1 invalid/duplicate data | Failed uniqueness preserves old values and orders |
| A11 / WinForms customer state | Existing customer | Update lock state; locking revokes all customer sessions and preserves orders | A1 unlock; customer must log in again; E1 missing customer/invalid action | Unlock does not resurrect revoked sessions; account and revocation commit together |
| A12 / WinForms customer orders | Existing selected customer | Query that customer's orders; no write | E1 no orders is empty | Exact customer filter and ADMIN authorization |
| A13 / WinForms suppliers | Signed in | Search/add/edit/disable supplier; preserve historical receipts | A1 choose each supported action; E1 duplicate code/invalid data | Referenced supplier cannot be hard deleted; inactive supplier cannot confirm a receipt |
| A14 / WinForms purchase editor | Valid supplier/variants | Purchasing service validates lines/quantities/costs, computes total and persists draft; stock/prices unchanged | A1 edit or cancel draft; E1 invalid quantity/cost | Empty draft allowed, confirmation denied; duplicate variant lines rejected; save/reload survives restart |
| A15 / WinForms purchase actions | Draft receipt | Confirm: check version/supplier/lines, update physical stock/current costs/computed prices/journal and receipt atomically; cancel: draft state only | A1 cancel draft without stock/price effect; E1 empty/invalid/inactive supplier/stale/finalized receipt | Duplicate confirmation; concurrent confirms; repricing overflow rolls back all lines; finalized receipt immutable |
| A16 / WinForms inventory | Signed in | Search/filter variants; read physical/reserved/available balances | E1 empty result | Available reconciles with physical/reserved; no date filter on current balances |
| A17 / WinForms stock count | Existing variant | Validate reason, version and counted >= reserved; calculate delta and save stock/journal atomically | E1 missing reason/count below reservations | Stale rowversion and duplicate operation rejected; no client supplied delta |
| A18 / WinForms order list | Signed in | Query by order code/customer/date/state; no write | E1 empty result | Valid date windows, pagination and authorization |
| A19 / WinForms confirm/cancel | Order not dispatched | Check state/version; confirm without collecting COD, or cancel with reason and release reservation/history | A1 cancel before dispatch; E1 wrong state/stale/missing reason | Cancellation does not add physical stock; concurrent customer/admin actions |
| A20 / WinForms delivery/COD | State permits selected action | Dispatch decrements physical + reserved; completion collects COD; failure records reason without restock; actual receipt of returned goods restores stock once | A1 failure without stock return; A2 receive actual returned goods once; E1 wrong state/duplicate/missing failure reason | Dispatch/payment/return double submission; same transaction rollback; no COD on confirmation or failed delivery |
| A21 / WinForms invoice preview/print | Confirmed, shipping or completed order | Read order snapshots for an internal sales document; printing does not write order, payment or stock | E1 ineligible order or unavailable printer | Server enforces eligible states; reprint does not mutate; physical printer verification belongs to UI phase |
| A22 / WinForms reports | Valid sales period when applicable | Revenue/bestsellers aggregate completed and COD-paid orders by completion UTC+7 date; stock uses current balances; one data result drives table/chart | A1 stock has no date period; E1 invalid range; no data is a valid zero/empty result | UTC+7 boundaries; no join double-count; bestsellers ordered by quantity; physical/reserved/available reconcile |

## Shared rules

| Generated ID | Source | Planned enforcement and proof |
| --- | --- | --- |
| G01 | 1.5.2.1; appendix A.3 | Server-side active account, role and record ownership; session lifetime 8 hours, no sliding renewal; logout current session; password/lock revoke all related sessions; password hashing and CSRF on cookie mutations |
| G02 | 1.5.2.1; A05/A06/A15; appendix A.2 | Decimal `Round(Cost * (1 + ProfitPercent / 100), 2, AwayFromZero)`; source percentage range 0-10000; positive two-decimal money; most recently confirmed purchase determines cost; changing percentage reprices; snapshots never reprice |
| G03 | K10-K12; A14-A17; appendix A.2 | `0 <= Reserved <= OnHand`; cart does not reserve; zero stock at variant creation; journal every movement; duplicate purchase variants rejected rather than silently summed |
| G04 | K12/K15/A19/A20; appendix A.2 | SQL Server transactions + rowversion + conditional stock writes; checkout request idempotency; operation keys and journal uniqueness prevent repeated effects across processes |
| G05 | K12/K14/A21; appendix A.2 | Recipient/line/name/SKU/price snapshots; only COD; no counter sales, refund workflow, electronic tax invoice or external delivery integration |
| G06 | A22; appendix A.4 | Completed + COD paid; completion dates with inclusive UTC+7 start/exclusive following day; current inventory ignores period; no profit report without cost allocation |
| G07 | 2.4; appendix A.2/A.5 | Preserve history; FK NO ACTION, unique identities and valid balances; explicit versioned migrations; no startup reset/seed; isolated SQL tests only |
| G08 | 2.2/2.5; prototype A | Vietnamese messages; dedicated home and catalog; restore safe form drafts on errors; keyboard/responsive/loading/empty/error behavior; no default login, fake shop contacts or mock transactions |

## State and delivery trace

All 37 business rows above are **Not implemented** in the replacement baseline. Planned service names, API routes, data tables and tests become concrete links only in the task implementing them. A passing foundation check does not satisfy G01-G08 or a business row. Each later task records source branch IDs, actual test method/file, UI status, issue, branch, commit, PR, latest-head CI and merge SHA. Partial work remains partial.

| Task | Phase / trace | Issue | Branch | Commit / PR | CI / merge | Status |
| --- | --- | --- | --- | --- | --- | --- |
| Backend CI | Foundation / enables SQL verification | [#1](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/issues/1) | `chore/1-backend-ci` | `524fc85`; [#3](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/3) | [merge CI](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/40959935122); `24ae9c8` | Merged and checked |
| Clean backend foundation | Foundation / prerequisite only | [#2](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/issues/2) | `feature/2-backend-foundation` | `4c8dd80`; [#4](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/4) | [merge CI](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/40960309866); `b0b4ef8` | Merged and checked |
| Requirement trace | Foundation / all rows | [#5](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/issues/5) | `docs/5-rebuild-requirements` | Pending | Pending | In progress |

## Dependency order and gate

Trace and contribution rules -> data/service contracts -> schema/migrations -> sessions/roles -> catalog/variants/pricing -> suppliers/purchasing/stock -> cart -> checkout/cancel -> fulfillment/COD/physical returns -> invoice data -> customer queries -> reports. Each domain is split into independently verifiable issue branches, not one implementation commit.

Frontend starts only when server behavior for every row/branch above is implemented and merged into `develop`, authorization/errors/filtering/pagination/contracts are verified, isolated SQL Server integration and important concurrency cases pass, independent API checks pass, and CI is successful on the latest `develop`. Existing mockups cannot satisfy this gate. UI-only actions such as sending a print job are explicitly deferred.
