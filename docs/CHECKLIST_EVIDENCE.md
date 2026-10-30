# Source-backed checklist verification

The proposed API document is an interoperability reference. Runtime naming, success DTO shape and error-code differences do not require a backend rewrite when behavior is equivalent. The original Word report and source-backed checklist requirements remain authoritative.

P0 groups in the decision document `03_DEC_DA_CHOT.md` are DEC.01, DEC.05 and DEC.06. Proposed password/phone policies and changed-payload replay are not silently adopted. Source-backed session/ownership/hash tests remain in the executable runner.

`ChecklistCatalogFilterChecks` proves A04.01-A04.04 with active/inactive internal search, correct totals/pages, role guards and unchanged public visibility/inventory/prices. The missing trading-state filter is a functional correction; q/query and DTO naming remain mappings.

`ChecklistP0Checks` adds directly named FLOW.01-FLOW.08 evidence with H=10/R=0, cost 1,000,000 and price 1,200,000. It also checks RACE.04/05/07/08 and RACE.10's late two-SKU dispatch failure followed by a safe retry. It uses real services and SQL Server, not in-memory mocks. Existing registration/session/password/checkout tests cover the remaining P0 authentication and retry paths.

Running a combined test does not automatically prove every clause of every checklist item. The local audit report records PASS/FAIL/BLOCKED/NOT RUN/N/A by exact original IDs and distinguishes unapproved proposals and deferred UI.

`ReadinessIdentityChecks` verifies Word A.1.2 target/environment metadata, identity stability across credential changes and isolation of configured targets, plus not-ready failure behavior. Remote WinForms TLS/fingerprint rejection itself remains a client-phase test.

See [proposed-to-actual API mappings](API_COMPATIBILITY.md) for transport-only differences and explicitly non-equivalent proposals. Original checklist IDs and all five result statuses are maintained in the local audit report.

`RemainingP0Checks` runs the eight requested P0 cells explicitly: K03.04, A01.04, A02.03, A02.04, A11.04, A11.05, A20.05 and RACE.09. It compares business snapshots around logout/lock, runs four phone/email claim races, enumerates all 42 order state/action pairs, verifies missing reasons/stale/repeated effects, and injects undelivered requests, SQL login failure and lost committed logout replies. Captured-cookie/SQL reconciliation is independent of local cookie removal. ADMIN lifetime is checked against eight hours. WinForms/TLS/browser testing remains deferred; no other unrun P1/P2 cell is certified by these tests.

`PublicBestsellerChecks` proves approved DEC.07/FE.01 with real SQL sales: product grouping, snapshot revenue tie-breaking, top-eight/ID ties, current labels/prices, inactive product/brand/category exclusion, UTC+7 boundaries, empty periods, anonymous HTTP and unchanged business data. ADMIN report ordering remains independent.

`StateMatrixChecks` supersedes the seven-action coverage claim for the new matrix: 48 parameterized order cells including invoices and separate customer/admin cancellation, 15 receipt operation/state cells, and seven retry/race groups. Literal matrix disagreements are separate observations, not PASS claims; strict mode fails on them. See [state matrix audit and decisions still needed](STATE_MATRIX_AUDIT.md), especially the no-reason T9 and identical count retry. The prior A20.05 PASS was limited to the then-current contract and is not unconditional acceptance of the new matrix.
