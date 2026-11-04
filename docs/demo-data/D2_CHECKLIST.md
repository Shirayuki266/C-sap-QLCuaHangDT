# D2 checklist evidence

All 43 original IDs/priorities are preserved. Historical D1 evidence remains separate. D2 closes SCALE, TIME, PURCH, ORDER and VERIFY.01, plus the previously partial customer/lifecycle/stock criteria. D3 has not been performed.

| Group | P0 PASS / total | P1 PASS / total | NOT RUN |
|---|---:|---:|---:|
| SAFE | 4 / 4 | 2 / 2 | 0 |
| SCALE | 1 / 1 | 1 / 1 | 0 |
| TIME | 3 / 3 | 1 / 1 | 0 |
| CATALOG | 4 / 4 | 3 / 3 | 0 |
| CUST | 1 / 1 | 2 / 2 | 0 |
| PURCH | 2 / 2 | 1 / 1 | 0 |
| ORDER | 5 / 5 | 3 / 3 | 0 |
| VERIFY | 1 / 3 | 0 / 3 | 5 |
| DOC | 0 / 0 | 0 / 2 | 2 |
| GATE | 0 / 1 | 0 / 1 | 2 |

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN |
|---|---:|---:|---:|---:|---:|
| P0 | 24 | 21 | 0 | 0 | 3 |
| P1 | 19 | 13 | 0 | 0 | 6 |
| P2 | 0 | 0 | 0 | 0 | 0 |
| Total | 43 | 34 | 0 | 0 | 9 |

Remaining NOT RUN: DATA.VERIFY.02–06, DATA.DOC.01–02, DATA.GATE.01–02. The library audit is exercised within D2 SQL verification, while the explicit existing Web --audit-db CLI proof and final application/UI/regression acceptance remain D3. Documentation is maintained now, but its final D3 acceptance is not claimed.

Per-ID reasons: [d2-checklist.json](../evidence/demo-data/d2-checklist.json). Counts, quarters, statuses, timings, hashes and corruption proof: [d2-summary.json](../evidence/demo-data/d2-summary.json). Read-only full report: [d2-verification.json](../evidence/demo-data/d2-verification.json).

Issue: [#115](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/issues/115). The linked PR verification binds the required CI run to its latest commit; no merge/tag is performed.
