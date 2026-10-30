# State matrix verification

Scope: the external `02_MA_TRAN_TRANG_THAI.md`, SHA256 `c8a3697ab2da821350428307e267a3e84fd806b166aa4a74b1c52d3f5d7b1827`, sections 3, 4, 6 and 7. Raw Word/matrix inputs remain private. This task changes tests and evidence only; production services, routes, DTOs and migrations remain unchanged.

## Executable coverage

`StateMatrixChecks` uses a parameter table with all **48 order cells**: six statuses by eight actions. Customer and ADMIN cancellation are independent rows. The eighth action is invoice reading, not a state-changing command. Every cell has a directly named check and uses its own order created through the actual workflow. Two variants have different quantities, 2 and 3.

Each forbidden cell requires `INVALID_STATE` and a byte-for-byte serialized SQL snapshot match for orders/versions, lines, inventory/versions, variant prices, COD, history, journals, receipts and carts. Each allowed write verifies the section-3 target, exact per-variant delta, payment fields/collector/time, history actor and journal kind/deltas. Invoice reads preserve the entire snapshot, including versions. The T9 history row is optional in the matrix; the current backend writes one.

Section 6 has **15 receipt cells**: three states by edit, add line, remove line, cancel and confirm. This expands the matrix's combined edit/add/remove row rather than omitting either line operation. Valid drafts can change supplier and replace lines without stock/price effects; cancellation preserves lines; confirmation updates each cost, rounded price, stock and journal once. Fixtures include a midpoint rounding case: 100.04 at 12.5% yields 112.55 with AwayFromZero. P1 creation is checked for unchanged inventory/prices on every fixture. Existing purchase tests also cover empty confirmation, inactive suppliers and late repricing rollback.

Section 7 uses gated simultaneous **HTTP requests** with tokens obtained before releasing the gate, independent database scopes and exact response-code assertions. RACE.02-.06, inventory operation-key races and same-owner checkout replay are covered. Rejected old-version and refreshed-version retries are followed by complete unchanged snapshots. No failed cell is swallowed: accumulated cell failures make the runner exit nonzero.

## Existing mappings, not business defects

| Matrix term | Actual backend | Interpretation |
| --- | --- | --- |
| STALE_VERSION / 409 | CONCURRENT_UPDATE / 409 | Old rowversion; reload required. |
| INVALID_STATE / 409 | INVALID_STATE / 409 | Fresh rowversion, disallowed state. |
| ALREADY_APPLIED / 409 | INVALID_STATE or CONCURRENT_UPDATE / 409 | Journal/state/version guards prevent a second effect. A two-return race can stop at version validation before the unique ledger constraint. |
| DAT_HANG / NHAP_HANG / KIEM_KE | GIU / NHAP / DIEU_CHINH | Same reservation/import/count deltas; names were proposals. |
| REQUEST_KEY_CONFLICT for another owner | FORBIDDEN / 403 | Existing owner boundary; no order/snapshot disclosure and no write. |

Observed order/receipt race losers use CONCURRENT_UPDATE/409. Refreshed-version repeats use INVALID_STATE/409. A refreshed inventory retry with a *different* count reaches the ledger guard and returns INVALID_STATE/409. The identical-count retry below is a separate case and is not concealed by that result.

## Deviations requiring the user's decision

| ID | Matrix versus current behavior | Assessment and recommendation |
| --- | --- | --- |
| DIFF-T9-REASON | Section 3 T9 says reason not required. Receive-return with current version/key but no reason returns INVALID_INPUT/400; no DB change. With a reason it passes and restores H only, once. | **Backend is stricter than the requested matrix.** Word UC09 explicitly requires reasons for cancellation/failure, but does not require one for receiving goods back. Recommend making the return reason optional if the new matrix is the target; alternatively explicitly amend T9 to retain the current contract. No change made. |
| DIFF-COUNT-EXACT-RETRY | After a successful count, repeat the same OperationKey/count with a refreshed version: INVALID_INPUT/400, not ALREADY_APPLIED's existing INVALID_STATE/409 mapping. Stock/history remain unchanged. | **Backend error classification differs from section 7**, beyond the approved naming map. The unchanged-count validation runs before the ledger check. Recommend detecting the committed OperationKey before no-change validation, while retaining authorization/version rules, if section 7 is approved. No change made. |
| DIFF-COD-AMOUNT | Section 5 writes Amount=Total unconditionally. A pending COD order has Amount=0; only collected COD has Amount=Total. | **Matrix wording is wrong under the existing approved policy** and conflicts with its own unpaid table. Qualify Amount=Total by DA_THU; retain Amount=0 for CHUA_THU. Do not collect pending COD. |
| DIFF-EMPTY-DRAFT | Section 6 P1 lists lines as creation conditions, while the backend persists an empty NHAP with total0, no inventory/price effects; confirming it is rejected. | **Matrix is incomplete/too restrictive under the confirmed drafting policy** in DECISIONS.md. Distinguish valid empty drafting from mandatory nonempty confirmation. No backend change recommended. |

**A20.05 is not finally accepted against the literal new matrix:** all 48 state/action cells can pass with current valid inputs, but the independent no-reason T9 case differs. The local checklist records this as FAIL against the new matrix, with the policy decision pending. This does not mean stock/COD invariants failed. The inventory retry mismatch is independently recorded; it does not turn all RACE checks into failures.

## Running and interpreting results

Use the standard isolated SQL runner from README. `PHONESTORE_MATRIX_RESULTS` optionally writes the sanitized per-cell observations to a local path. Credentials and business snapshots are never written to that artifact.

Default regression execution asserts all 48 order cells, 15 receipt cells and seven retry groups. Literal-policy probes print **MATRIX DEVIATION**, separately from passing regression checks; they are observations, not approved exceptions or PASS conformance results. The evidence artifact preserves `Conforms=false`. Set `PHONESTORE_MATRIX_STRICT=1` for literal acceptance: any unresolved deviation must make the runner fail. A green regression CI is not full matrix acceptance while these discrepancies remain. WinForms printing/hardware is outside this server invoice-read verification.
