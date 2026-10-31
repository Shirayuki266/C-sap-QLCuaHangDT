# Prompt 2A — remaining checklist verification

This is a test-only task against the unchanged production base from merged PR80. Existing PRs82/83 are left untouched. No production source, migration, permissions or runtime error policy is changed. The official PRIORITY labels remain 114 P0, 89 P1 and 21 P2.

The starting report has20 NOT RUN rows. The supplied list has21 distinct IDs because A15.04 already has PASS evidence from PR83; it is rechecked here. Groups1/2 therefore add coverage for17 previously unrun rows plus one repeated PASS row. BASE.12, FE.08 and K10.03 remain NOT RUN for MVC.

## Required group

| ID | Direct SQL/HTTP evidence |
| --- | --- |
| A05.05 | Unsafe URL schemes, credentials, traversal/control characters, duplicate/oversized image collections, duplicate SKU and privileged overposting reject atomically; CUSTOMER cannot create. Valid HTTPS/local images are accepted. |
| A17.05 | A temporary QA CHECK constraint rejects the inventory journal after the stock UPDATE. The complete snapshot remains unchanged, the constraint is removed in finally, and retry with the same key/version succeeds once. SQL547 uses the existing INVALID_INPUT/400 mapping. |
| K09.02 | Fake/discontinued product IDs return404; detail contains only its own variants. The backend does not bind a selected-variant query: a foreign query value cannot inject a foreign variant into the returned product DTO. This does not certify MVC selection behavior. |
| K09.03 | Real checkout reserves all physical stock: H=R=5 per variant, public Available0, product still readable and internal balances/cost redacted. |
| A10.02 | ADMIN duplicate phone/email edits preserve the entire DB snapshot; retaining normalized own identifiers succeeds. |
| A10.04 | Missing target and invalid name/phone/email reject without partial profile edits or changed order-recipient snapshots. |
| A15.04 | Rechecks populated/empty cancellation (only receipt status/version change) and empty-confirmation rejection. Detailed finalized/stale/inactive-supplier coverage remains in PR83 evidence on the same production code. |
| A21.01 | All six order states, missing ID and CUSTOMER guard; eligible invoices preserve the complete snapshot. Physical printing is deferred. |
| A16.03 | No-match stock page has total0/items[]; temporarily renamed QA inventory table produces SERVER_ERROR/500 and is restored in finally, never a fake zero-stock success. |
| K08.01 | Combined brand/price/storage/color/sort returns exactly two qualifying products with correct page1/page2 totals and prices. |
| K08.02 | Near-miss products with color/storage/price spread across different variants are excluded. |

## Cheap group

| ID | Direct evidence |
| --- | --- |
| A08.03 | Inactive lookup cannot be newly selected on create/update; existing inactive references can be retained on edits, remain nonpublic and do not change stock/history. |
| A09.01 | Separate name/phone/email lookup and exact two-customer stable paging/totals. |
| A12.04 | Selected-customer lists retain line/history/total snapshots after live repricing and have no writes. |
| A09.04 | HTTP searches preserve profiles, account state, sessions and all captured business tables. |
| K07.02 | Blank/whitespace equals unfiltered catalog; no-match is a valid empty page with metadata. |
| K08.04 | Clearing color/price/all filters retains remaining predicates and recomputes exact totals/pages. |
| K13.04 | Own order lists keep ownership and leave status/completion/COD/stock/history unchanged. |

Prompt2AChecks captures all business tables plus products/images/lookups/profiles/accounts/sessions in memory; snapshots and secrets are never printed or persisted. Tests use the existing generated-QA database lifecycle. Temporary SQL fault injection is restricted to those QA databases, restored in finally, and is not a migration or a change to a shared database.

`--prompt2a` runs the18 original groups plus four supplemental checks in an isolated migrated database. Standard `--integration` includes these tests with the existing suite. Build/assertion failures exit nonzero; observed backend disagreements must be reported rather than fixed by this task.

Coverage counts and gate results must follow actual evidence, not the requested target percentages. A green test PR is not permission to merge it: submit the PR and wait for explicit user review. Browser localStorage, RAM/UI drafts and printer behavior remain deferred in BASE.12/FE.08/K10.03 under their original labels.

## Supplemental assertions and unresolved acceptance questions

The stricter prompt adds protocol-relative URLs, HTTP credentials, URL length, empty gallery entries, positive H-R availability and old recipient checks in the same duplicate-profile test. Those assertions use real SQL/HTTP and unchanged snapshots after rejection. Main/gallery URLs are tested separately: Images[].Url rejects null/empty/whitespace, valid HTTPS/local URLs succeed, a primary URL of exactly1000 characters succeeds and longer input is rejected. K09.03 now explicitly checks H=7/R=2/Available=5 as well as H=R=5/Available=0. A10.02 creates an order first and verifies its recipient after each duplicate edit and after retaining normalized own identifiers.

Supplemental acceptance is recorded separately from passing regression assertions. The user clarified that only Images[] URLs must reject blank values; the primary ImageUrl remains optional.

| ID | Request/response observed | Assessment |
| --- | --- | --- |
| A05.05-primary-empty | PUT product with valid retained versions and ImageUrl="" returns204; SQL changes the primary URL to empty, preserves the gallery and stock. Empty Images[].Url returns400 with no write. | PASS under the explicit user clarification: reject blanks only in Images[], keep the primary image optional. No production change. |
| K09.02-selected-variant | GET catalog/{product}?variantId={foreign} returns200 with only the product's own variants; no selected-variant field and no DB writes. | There is no native selected-variant binding. DTO isolation passes, but this is not evidence of rejecting a bad selected product/variant pair. Decide a server selection contract or explicitly assign selection validation to MVC. Do not invent an endpoint or change runtime behavior in a test-only task. |

Prompt2ASupplementChecks writes only sanitized request/response/fixture SQL summaries when PHONESTORE_PROMPT2A_SUPPLEMENT_RESULTS is configured. Full account/session snapshots stay in memory and are never exported. PHONESTORE_PROMPT2A_SUPPLEMENT_STRICT=1 fails after all checks while selected-variant acceptance remains unresolved. Default regression verifies the tested behavior and exposes SUPPLEMENT BLOCKED; green CI must not be reported as proof of a native selected-variant rejection guard. A05.05 matches the clarified image policy; K09.02 whole-row acceptance remains blocked for the selection clause. No production code has been changed.
