# Official PRIORITY checklist adoption

The official checklist is CHECKLIST_BACKEND_TOAN_BO_USECASE_PRIORITY.md supplied on 2026-10-31: the same 224 IDs and criteria, with 114 P0, 89 P1 and 21 P2 labels. Report ordering and totals must use these labels verbatim. Earlier grouping by DEC.01/.05/.06 or test names is superseded.

Existing PASS results retain their original evidence/commit when unaffected. No extra local full-suite run is needed just to add labels. The full 213-check regression after the approved T9 source edit is preserved; six new P0 checks run separately with --priority-p0 on a generated isolated SQL database. PR-required CI remains configured to run the complete regression suite for the proposed source.

| Official P0 ID | New evidence |
| --- | --- |
| K07.04 | Literal special/SQL-like public search; a command interceptor verifies parameter values versus command text; HTTP results and business snapshots stay correct. |
| A07.01 | Disable/reopen affects public catalog and checkout; rejected checkout has no effect, reopened checkout reserves once. |
| A07.04 | Disable a previously viewed/cart variant; checkout revalidates and leaves order/stock/payment/history/cart unchanged on rejection. |
| A08.02 | Disable a referenced brand/category; products, variants, receipts, completed orders, snapshots and history survive unchanged. |
| A18.04 | CUSTOMER/guest cannot query all orders; ADMIN lists/details and service queries leave full business snapshots unchanged. |
| A21.02 | Change live catalog name/SKU/price/state and customer profile; historical invoice recipient/lines/prices/total remain byte-equivalent and reads have no effects. |

T9 is the sole production behavior change: optional trimmed reason, maximum1000 characters after trimming. ReturnReasonChecks and StateMatrixChecks verify optional input, exact stock/COD/history/version effects and rejection of oversized input with rollback. Other reasons remain mandatory. Accepted exact count-retry INVALID_INPUT and empty-draft persistence are not rewritten.

K10.03 stays official P0/NOT RUN by explicit user deferral to MVC; it is not relabeled P2, N/A or PASS. A03.02 and FE.04 are PASS with an explicit accepted-difference annotation backed by the existing shared-password/error mapping evidence and the user's decision. This is not a claim of a new runtime test.

Unrun P1/P2 rows keep NOT RUN with short reasons. A15.04 is clarified by the latest matrix but is not newly certified in this P0-first batch. Backend-wide gates remain explicit; no complete handoff is claimed while a required P0 remains unrun or a relevant gate awaits review.

The user explicitly revoked automatic merge permission. Submit the PR, verify current-head CI and wait for approval. Do not merge into develop or close the issue as delivered before that approval.
