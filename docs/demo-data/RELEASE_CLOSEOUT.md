# Real demo data closeout — work in progress

This record reconciles merged PRs before the next acceptance candidate. It is not release approval. No M1/M2 or release tag is declared until all current gates have executed.

## Merged PRs

| PR | Approved implementation head | Squash commit in develop | Exact-head CI |
|---|---|---|---|
| [#114](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/114) | `6c01ef8e59db42a2e5fd867006a1fbad98e64aac` | `e4ad5d678b4c189be0c6696e2fd34ab6079735d2` | [success](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41614516158) |
| [#116](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/116) | `418808cecf2158e87c73b3bea813814a1e17fecb` | `70a80c4630c62031ebed17fdb0d758ea2b4b63dc` | [success](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41624942721) |
| [#118](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/118) | `5440a1fc9bcfda401e577570cecd87c29cfb4346` | `f3130c96079f0c2a49dc353ffdba23399f3b74bf` | [success](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41636843891) |
| [#120](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/120) | `ec9350c5b97a7d2553de61bf36bc37efb3a806a5` | `a4cc50eed57f076d64accb1798620ccc4c95f947` | [success](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41664327430) |

Current develop baseline: `a4cc50eed57f076d64accb1798620ccc4c95f947`. Its tree equals the original PR120 head tree; PR120 must not be merged again. Main remains outside this work until the user says **DUYỆT PHÁT HÀNH**.

## User decisions, 2026-11-04 (UTC+7)

- Preserve Infrastructure/Web/WinForms runtime and the existing tags/history. The user explicitly approved retaining the legacy Web DemoSeeder as an exception: “Giữ nguyên runtime; ghi ngoại lệ rõ ràng cho seeder cũ, chỉ công cụ demo mới dùng catalog thật”. This exception is limited to the existing legacy seeder and its existing demo-phone.svg asset. It does not authorize fictional products from PhoneStore.DemoData or their use in the real demo database.
- Real photos may be replaced by suitably licensed real photos and models may change when necessary; keep 50 products and125 variants, with the original10-brand/6-category acceptance scale unless a separate change is approved. Source attribution alone is not recorded as redistribution permission.

## Acceptance and safety

- The separate demo tool is real-only. Archived fictional scripts must be reproduced at their historical tags/commits, not by the new tool.
- File11/12 retain historical planning context; the current DATA.CATALOG.01–03 requirements use real manufacturers/models, factual original Vietnamese descriptions, real images and recorded author/license/source. Synthetic customers and transactions remain synthetic. The old prohibition on real brand imagery does not govern this approved real catalog.
- Historical D3 screenshots remain historical and are explicitly labelled; fresh acceptance evidence must refer to the new candidate and dataset.
- Never remove the manually added brand from an existing user database to make the verifier pass. Build a new tool-owned full demo instead; preserve the existing preview database.
- Clean-checkout tests use fresh demo/QA targets and environment-only random passwords. No .bak, credentials, cookies or connection strings are committed.
- Current authorization is preparation and a correction PR only. Do not merge (including reconciliation merges), create any tag, push main, delete branches, rewrite history or remove recovery refs. Bind clean-checkout evidence to the exact correction PR head; after user review a future develop candidate must be verified separately. Retained backups stay outside Git.
- `node scripts/verify-data-closeout.mjs --report <artifact.json> --ci-run <run-id>` reads the actual CI run and rejects missing/non-PASS records, wrong candidate SHA/tree, missing/tampered evidence and baseline runtime changes. It does not create tags or mutate refs. Self-tests cover rejection of false completion and pending image rights.
- The ledger has76 records:75 require executed PASS; CLEAN.FILES.04 must be ACCEPTED_EXCEPTION with the approval reference below and a reason, counted separately. No other exception is accepted. M1 is not declared in this PR. Even after checks pass, do not create data-v1.0 until the user sends the literal “DUYỆT PHÁT HÀNH”.
- Stop after opening the correction PR and report its exact SHA/CI/evidence. Await user approval; no main PR, merge, data tag, v1.0, GitHub Release or branch cleanup in this phase.

## Remaining work

Image replacement/license checks, market-reference price assumptions, source-backed full data, clean-checkout real application tests, all-ref secret scan, backup inventory, final per-ID report, correction PR CI and develop candidate verification remain open. A successful unit build is not a PASS for these items.

## Approved price criterion clarification, 2026-11-04

The user selected simulated costs/prices with recorded reference bands, time basis and assumptions rather than a claim of current quotes or real purchase costs. [PRICE_ASSUMPTIONS.md](PRICE_ASSUMPTIONS.md) is the active price-acceptance policy for DATA.CATALOG.05/CLEAN.INTEG.02/CLEAN.REAL.03. Mathematical SQL invariants still require executed PASS evidence; this decision does not waive them.

## approval-legacy-seeder-2026-11-04

User approval in this conversation on2026-11-04: “Giữ nguyên runtime; ghi ngoại lệ rõ ràng cho seeder cũ, chỉ công cụ demo mới dùng catalog thật”. The user subsequently required: “thêm trạng thái ACCEPTED_EXCEPTION (kèm tham chiếu lần duyệt của mình và lý do) cho CLEAN.FILES.04; đếm riêng, không gộp vào PASS. Mọi ô khác phải PASS thật.”

Reason: replacing the legacy Infrastructure DemoSeeder would violate the approved runtime freeze. Only that existing seeder and its existing demo-phone.svg are retained. The separate current tool rejects fictional generation and the dedicated real demo must contain no fictional product records. This is a recorded exception, not a PASS or a waiver for other IDs.
