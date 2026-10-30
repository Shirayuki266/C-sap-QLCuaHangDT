# Source-backed checklist verification

The proposed API document is an interoperability reference. Runtime naming, success DTO shape and error-code differences do not require a backend rewrite when behavior is equivalent. The original Word report and source-backed checklist requirements remain authoritative.

P0 groups in the decision document `03_DEC_DA_CHOT.md` are DEC.01, DEC.05 and DEC.06. Proposed password/phone policies and changed-payload replay are not silently adopted. Source-backed session/ownership/hash tests remain in the executable runner.

`ChecklistCatalogFilterChecks` proves A04.01-A04.04 with active/inactive internal search, correct totals/pages, role guards and unchanged public visibility/inventory/prices. The missing trading-state filter is a functional correction; q/query and DTO naming remain mappings.

`ChecklistP0Checks` adds directly named FLOW.01-FLOW.08 evidence with H=10/R=0, cost 1,000,000 and price 1,200,000. It also checks RACE.04/05/07/08 and RACE.10's late two-SKU dispatch failure followed by a safe retry. It uses real services and SQL Server, not in-memory mocks. Existing registration/session/password/checkout tests cover the remaining P0 authentication and retry paths.

Running a combined test does not automatically prove every clause of every checklist item. The local audit report records PASS/FAIL/BLOCKED/NOT RUN/N/A by exact original IDs and distinguishes unapproved proposals and deferred UI.

`ReadinessIdentityChecks` verifies Word A.1.2 target/environment metadata, identity stability across credential changes and isolation of configured targets, plus not-ready failure behavior. Remote WinForms TLS/fingerprint rejection itself remains a client-phase test.

See [proposed-to-actual API mappings](API_COMPATIBILITY.md) for transport-only differences and explicitly non-equivalent proposals. Original checklist IDs and all five result statuses are maintained in the local audit report.
