# Backend closeout

This PR changes documentation, a read-only security inventory tool and QA tests only. Runtime source, services, API endpoints and migrations have zero diff from accepted backend2A/develop dbc888a. The separate MVC foundation PR87 is not merged or certified by this work. No release tag is created.

The subsequent review of PR89 adds documentation only. [BACKEND_HANDOVER](BACKEND_HANDOVER.md) records clean-clone README execution, exact source paths, migration-lock mechanisms, service scope and the distinction between remote Git history and local recovery refs. This does not change the PR's tests/tool or runtime.

## Accepted scope and final ledger

[Final224-ID ledger](BACKEND_CHECKLIST_FINAL.md) uses the original priority labels:114P0/89P1/21P2. K09.02 passes API behavior; selected product/variant validation transfers to WEB.K09.02 as an accepted difference. K10.03, BASE.12 and FE.08 are explicitly transferred to MVC. GATE.01/.03 pass for the accepted server scope. BASE.05 legacy-upgrade coverage is N/A: **chỉ hỗ trợ dựng DB mới, không có đường nâng cấp từ DB cũ**. Its clean-creation/replay portion has fresh evidence below. Unchanged accepted rows retain their prior proofs rather than being individually reclassified from a smoke result.

The effective final counts require the closeout PR's latest-head CI to be SUCCESS: P0=112PASS/1NOT RUN/1N/A; P1=88PASS/1NOT RUN; P2=20PASS/1NOT RUN. Before that CI completes, GATE.05 is NOT RUN; do not treat a pending check as PASS. The local authoritative ledger records the actual current-head check result.

## Empty database evidence

The actual CLI `--migrate` was run against a generated, initially empty QA DB. The installed lineage is `rebuild/001_initial_store`; its real ledger table is **dbo.__PhoneStoreMigrations**. The runtime contains only001_InitialStore.sql, which already builds the final schema. No002/003 migration is fabricated, applied or claimed.

The new `--backend-smoke` runner executes six groups:

1. Start the actual host on a blank QA DB: liveness200, readiness503, still zero application tables. Startup does not migrate, seed or provision.
2. Run the real migration CLI: one checksum ledger row, nineteen tables and no sample rows.
3. Replay the CLI without changing ledger timestamp/hash or rows; execute `--audit-db` successfully.
4. Query INFORMATION_SCHEMA/sys and verify the final schema.
5. Start a fresh host: readiness200, empty catalog/bestsellers/brands, protected ADMIN requests401, token endpoint200 and missing-CSRF mutation400.
6. Confirm all eighteen business/session tables remain empty and the ledger is unchanged.

Observed metadata: **19 tables;37 unique indexes including19 primary keys;25 foreign keys, all enabled/trusted and NO ACTION on delete/update;30 enabled/trusted CHECK constraints;4 eight-byte SQL rowversion columns**. BIEN_THE.Cost/Price are decimal(18,2), ProfitPercent is decimal(7,2), all NOT NULL. The four versioned tables are BIEN_THE, TON_KHO, DON_HANG and PHIEU_NHAP. Email and warehouse document uniqueness retain their filtered predicates. Existing SQL constraint tests remain the prior behavioral enforcement proof; this new pass independently inspects installed metadata.

[Sanitized SQL/HTTP evidence](evidence/backend-closeout/empty-db-smoke.json) includes actual definitions and response statuses, no credentials, tokens, cookies or account data. The test fixture disposes only its own generated QA DB. [README](../README.md) gives reproducible build, migration, startup and test commands.

## Security and optional data

[Security inventory](BACKEND_SECURITY_REVIEW.md) distinguishes disposable QA credentials from real application secrets. The user accepted the existing optional demo seeder's non-secret sample identifiers; it requires external passwords and explicit invocation, must be used only on QA/dev and never on a live DB. Runtime enforcement of the QA-name restriction is deferred; no service is altered here.

## Tagging boundary

No merge, branch cleanup or tag creation is authorized by this closeout task. After the user approves the PR, verify its exact reviewed SHA, successful required CI and the merge outcome before any separately approved release operation. A possible annotated tag is `backend-v1.0`; it must identify the specifically approved backend commit, not an unchecked moving branch tip or an unrelated MVC commit. Do not overwrite an existing tag. Record the tag SHA and its provenance when it is actually created; until then the version is pending.
