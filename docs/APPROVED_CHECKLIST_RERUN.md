# Approved checklist restart verification

The superseding user request requires a fresh repository audit, the four current inputs, the final optional T9 reason policy, all eight named checklist rows and a new full regression. The official PRIORITY file is the sole source of labels: 114 P0, 89 P1 and 21 P2 across unchanged 224 IDs.

## Starting state

Read-only step0 found local feature/79-optional-return at 35f5dbc, no tracked unstaged/staged changes, and two unrelated untracked directories (.vscode and the existing option-A design folder). GitHub had no open PRs; PR80 was already merged as 1f0af932ab4fa66c314c5a8d232c42b0fc92044d, while local develop still pointed at 559e8be. Post-merge CI for 1f0af9 was independently verified successful before creating the new task branch. Nothing was deleted or overwritten during step0; the unrelated directories are preserved.

## Production behavior

T9 is already correct in the merged base: optional reason, trimming and a 1000-character limit after trimming; blank input succeeds. Supplied normalized text is stored in return/order history. Existing cancellation/failure reasons remain required. The nine ReturnReasonChecks and full order matrix are rerun under this policy. No further production source or migration edit is necessary or authorized.

Exact count retries returning INVALID_INPUT and empty persisted purchase drafts remain unchanged. A03.02 and FE.04 are accepted compatibility differences. Proposed route/DTO/error names are mapped to the existing backend instead of triggering extra runtime changes.

## New A15.04 coverage

PurchaseCancellationChecklistChecks uses actual SQL/HTTP requests and independent stored drafts. It checks populated and empty cancellation; rejection of empty/inactive-supplier confirmation while cancellation is still allowed; stale edit/cancel/confirm; and all three actions against confirmed/cancelled receipts. Successful cancellation may change only receipt Status and Version. Full business snapshots prove no stock, reservation, price, receipt-line, payment or history side effects. Rejected operations leave the entire captured business snapshot unchanged.

Current error mappings are explicit: PURCHASE_EMPTY and SUPPLIER_INACTIVE proposal names map to INVALID_INPUT/400; STALE_VERSION maps to CONCURRENT_UPDATE/409; finalized receipt actions return INVALID_STATE/409. These transport differences are reported without production edits.

## Fresh run and review boundary

The full SQL integration command reruns A20.05, A15.04, K07.04, A07.01, A07.04, A08.02, A18.04 and A21.02 together with all existing regression checks. PHONESTORE_MATRIX_STRICT=1 makes any unresolved matrix observation fail the run. Only generated QA databases may be used; the sample/legacy databases are preserved.

Fresh local validation completed with zero build warnings/errors and 223 passing SQL/HTTP regression checks, including four directly named A15.04 groups. The matrix artifact has no assertion failures or unresolved conformance observations. Check counts are not checklist-row counts; each of the eight requested IDs has its own evidence.

K10.03 remains official P0/NOT RUN by explicit MVC-phase deferral. Other unrun P1/P2 cells are not silently certified from a shared test count. Existing unaffected evidence is kept with its original source; new proof must identify the actual fresh run and PR head.

The new task is submitted as a PR for user review. Do not merge automatically. A historical successful merge of PR80 does not grant permission to merge the new PR.
