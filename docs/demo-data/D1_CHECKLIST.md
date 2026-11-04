# D1 checklist evidence

All 43 IDs and original priorities are retained. PASS applies only where the complete criterion has D1 evidence. Partial customer criteria remain NOT RUN. No D2/D3 acceptance is implied.

| Group | P0 PASS / total | P1 PASS / total | NOT RUN |
|---|---:|---:|---:|
| SAFE | 4 / 4 | 2 / 2 | 0 |
| SCALE | 0 / 1 | 0 / 1 | 2 |
| TIME | 0 / 3 | 0 / 1 | 4 |
| CATALOG | 4 / 4 | 1 / 3 | 2 |
| CUST | 1 / 1 | 0 / 2 | 2 |
| PURCH | 0 / 2 | 0 / 1 | 3 |
| ORDER | 0 / 5 | 0 / 3 | 8 |
| VERIFY | 0 / 3 | 0 / 3 | 6 |
| DOC | 0 / 0 | 0 / 2 | 2 |
| GATE | 0 / 1 | 0 / 1 | 2 |

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN |
|---|---:|---:|---:|---:|---:|
| P0 | 24 | 9 | 0 | 0 | 15 |
| P1 | 19 | 3 | 0 | 0 | 16 |
| P2 | 0 | 0 | 0 | 0 | 0 |
| Total | 43 | 12 | 0 | 0 | 31 |

Per-ID statuses and reasons, including every NOT RUN ID: [d1-checklist.json](../evidence/demo-data/d1-checklist.json). Runtime evidence: [d1-summary.json](../evidence/demo-data/d1-summary.json).

DATA.CUST.02 and DATA.CUST.03: D1 provides the account history, locked accounts and diverse fictional addresses. All 60 customers currently have zero orders. Exactly two customers with no orders and a skewed purchase distribution await D2.

The final PR verification binds CI to the latest commit. Earlier run hashes are not a substitute for latest-head CI. The final full-data gate remains NOT RUN.
