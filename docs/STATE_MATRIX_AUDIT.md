# State matrix verification — current approved decisions

Inputs: the 31 October revisions of 01_API_CONTRACT, 02_MA_TRAN_TRANG_THAI and 03_DEC_DA_CHOT; matrix SHA256 2faa67fee0eb2573e29ad7d4126423d50994578412372a5b2e4ca29cd0ceb19c. Raw authoring files remain private. Checklist priority comes only from CHECKLIST_BACKEND_TOAN_BO_USECASE_PRIORITY, never inferred from decision groups or test class names.

The final user decision makes T9 return reasons optional. The only production edit is passing optional:true to the existing trimmed, 1000-character reason validator in ReceiveReturn. Empty/missing/null/whitespace values succeed; supplied text is trimmed and persisted in stock and order history. Cancellation/delivery-failure reasons stay mandatory. Order state, rowversion, ownership, COD and once-only stock guards are retained.

ReturnReasonChecks covers nine HTTP cases: missing, null, empty, whitespace, long whitespace, trimmed text, exact maximum, trim-before-length and oversized rejection. Successful cases verify H/R, one same-state history row, stored normalized reasons and duplicate-return refusal. Oversized input must leave the whole SQL snapshot unchanged.

StateMatrixChecks retains all 48 order cells, 15 receipt cells, seven race/retry groups and a six-state conditional COD Amount check. Forbidden cells compare full business snapshots; allowed writes reconcile per-SKU deltas, snapshots, COD and actor/history fields. Invoice reads preserve versions. T9 must write exactly one same-state history row under the newly accepted matrix.

## Previously reported differences

| Observation | Current decision and expectation |
| --- | --- |
| T9 required reason | Fixed to optional by the final user instruction; the earlier sentence accepting a required reason is superseded. |
| Exact count replay with fresh version | Accepted unchanged INVALID_INPUT/400, no second effect. Different count/same key still INVALID_STATE/409; stale version still CONCURRENT_UPDATE/409. |
| Empty draft | Accepted persisted NHAP with zero lines/total; empty confirmation rejected as INVALID_INPUT/400 (proposal PURCHASE_EMPTY mapping). Empty cancellation is permitted. |
| COD Amount | Amount0 for CHUA_THU; Amount=Total only for DA_THU, with completion/collection metadata reconciled across six states. |

These observations now require Conforms=true. Unknown disagreements remain reportable; the test audit never approves them automatically. Strict mode throws if any observation or cell fails. There is no need to rename current routes, DTOs, journal kinds or error codes to match proposal names.

## Priority and review scope

PriorityP0Checks adds the six outstanding official P0 IDs K07.04, A07.01, A07.04, A08.02, A18.04 and A21.02. The --priority-p0 runner creates/migrates its own generated SQL database and executes only those checks. Existing PASS evidence is carried forward when unaffected; the prior full regression after the T9 source edit is preserved separately. Repository-required PR CI still runs its configured full suite.

K10.03 is official P0 but remains NOT RUN by the user's explicit MVC-phase deferral. Other unrun P1/P2 rows remain NOT RUN; A15.04 is clarified by the new matrix but has not been newly certified here. A03.02 and FE.04 are accepted compatibility differences, not unresolved business decisions. Physical printing/browser/WinForms behavior is not claimed by server tests.

The current work must be submitted as a PR and await the user's review. No automatic merge to develop is authorized.
