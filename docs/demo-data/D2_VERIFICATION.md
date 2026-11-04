# D2 independent SQL verification coverage

`--verify-demo-data` reads a previously generated snapshot and lifecycle facts. `DatasetVerifier` has no reference to Generation and performs no SQL writes. It uses a coherent Serializable read under the existing shared `PhoneStore:write` lock. Existing `DataAudit` checks are retained; additional SQL closes its historical/time/snapshot gaps. Filesystem checks validate the image paths and bytes. A failure returns nonzero and retains table/quarter/status summaries and invariant labels.

| File 11 section 8 invariant | Evidence in persisted SQL / files |
|---|---|
| Final OnHand/Reserved equal journal sums; reservation equals open order quantities | DataAudit inventory/journal and open-order checks; existing trusted balance constraint |
| No negative/over-reserved stock at ANY historical step | Window SUM of both deltas per variant ordered by `At,Id`; balanced negative-prefix QA fixture proves detection beyond DataAudit |
| Totals, required lines, one COD, collected iff complete, amounts and same completion/collection time | DataAudit order/payment checks plus completion history/collector equality; schema PK/unique/FK/CHECK verification |
| Legal complete history, exact state, increasing timestamps and valid actors | DataAudit transition chain/latest state/role rules; strict timestamp SQL; create actor=owner; later privileged actors ADMIN; reasons required/trimmed |
| Physical receive-return does not change status, happens once, covers every line | Journal unique constraints, DataAudit physical return checks, and two-way same-state history/journal time+actor matching |
| Current price rounding, latest cost and historical checkout snapshot price | Independent SQL `ROUND`; latest confirmed cost by both journal identity and event time; each snapshot joins latest confirmed receipt BEFORE placement, with immutable name/SKU/variant description |
| Decreasing cost for older models | Independent LAG comparison across confirmed receipts; 3–6% compounded decline bounds per elapsed local calendar quarter, rounding tolerance 0.03 |
| Receipt totals, confirmation actor/time, empty drafts, no draft/cancel stock effects, no duplicate variant | Existing DataAudit + trusted CHECK/unique constraints; creator/account chronology and receipt/journal time+actor checks; explicit scenario metrics |
| Lowercase unique username/phone/optional email, NULL instead of empty | Existing trusted unique indexes, account/profile mapping, independent binary lowercase comparison and optional-email SQL |
| No future time; account before first order; ADMIN before receipts; launch before receipt/sale | UTC watermark on every business timestamp; independent horizon calculation; metadata JSON joined to actual product IDs and receipts/orders; retirement prevents new activity |
| Valid ImageUrl, files, descriptions/specifications | Foundation SQL text/length/image ordering checks and actual local files; standalone verifier checks every persisted image hash against the seed manifest; real Kestrel tests retain HTTP 200/content-type/content proof |
| Unique operation/request keys and signed effects | Trusted SQL key constraints, existing services, DataAudit document effects and additional cross-document/type/time/actor operation-key grouping |

Generation does not provide expected stock totals, order totals or computed prices to Verification. SQL derives those from persisted rows. Lifecycle dates are required input facts because the unchanged product schema has no dates. The manifest checksum and current normalized business snapshot comparison detect edits beyond the specific invariant checks; they supplement rather than replace SQL invariant checks.

Independent scale/scenario acceptance also checks all 19 table ranges (full), nine local quarters and their source targets, paid revenue by completion date, >=20 recent placements, >=8 eligible products completed in the current month, locked/one-time/repeat/non-buying customers, low/out/normal stock, 6–10 reasoned counts, inactive suppliers, four retired products with history and current cart ownership/availability.

QA corruption coverage: final inventory mismatch; order total mismatch; modified line price with order/payment totals kept consistent; equal consecutive history times; balanced historical negative stock; business timestamp after the snapshot cutoff. Each case must name its independent SQL failure in the CLI report and return nonzero. The valid snapshot passes again after restoration. First-of-month and first-of-quarter real service fixtures prove recent sales and current-month completion windows with no future timestamps.

The 19th table is the existing migration ledger. Its application timestamp, salted credential hashes and rowversions are technical values excluded from reproducibility; no migration/schema change or extra persistent table is introduced. Acceptance is for a freshly generated snapshot, not arbitrary ongoing real application edits. Real GUI walkthrough, explicit existing audit CLI and final gate remain D3.
