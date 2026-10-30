# Rebuild decisions

## Confirmed

- User approval on 2026-10-30: DEC.02 uses max(server, guest), caps at current Available, removes missing/discontinued/zero-available lines and reports adjustments. Duplicate guest IDs use max. Unchanged inputs and availability produce an idempotent merge; changed live availability is re-evaluated on retry. DEC.03 explicitly retains current behavior, including public exclusion of products under inactive brands/categories.

- On 2026-10-30 the user explicitly requested deleting the old application code and implementing a fresh version from the revised report and backend-first prompt. This overrides preserving the former UI/source. Git history, private Word/prompt/design inputs, local tool settings and existing SQL databases remain intact.
- Keep .NET 10, C#, EF Core SQL Server, the Core/Infrastructure/Web project boundaries, one shared business layer, and an eventual WinForms ADMIN client. No second runtime backend or alternative database is introduced.
- `develop` was bootstrapped from original `main` commit `77d0bd6` because the remote had only `main`. Task PRs target `develop` and squash only after checks and review requirements are satisfied. `main` is not promoted or rewritten. No branch protection is changed.
- This is a new database lineage. `PhoneStore_Rebuild` is a documented local example, never an automatic fallback. The legacy report's migration 003 backfill is an upgrade instruction for legacy databases, not an instruction to fabricate legacy receipts in a clean database. New schemas store Cost/ProfitPercent from their first migration. Migration commands must reject recognizable legacy schema rather than overwrite or reprice it.
- Source-backed session lifetime is 8 hours, fixed without sliding renewal. Monetary calculations use decimal and AwayFromZero rounding to two places; source profit range is 0-10000 percent. Validation boundaries for other fields must be named in contracts and checked against SQL capacity, rather than inferred from prototype values.
- Purchase drafts can be empty and persisted. Confirmation requires a nonempty valid draft with an active supplier. Duplicate variant lines in a draft request are rejected, not merged. This choice must be part of the purchasing contract and tests.
- There is no public ADMIN registration or seeded default password. Admin provisioning must be an explicit administrative command with an external password input, once the account domain is implemented.
- No production deployment, promotion to staging/main, fake contact publication, or change to repository access/protection is authorized by task merge permission.

## Pending decisions

- Homepage bestseller period: the legacy code used 30 days, but it was removed. The revised prompt requires a separate decision if the report does not settle this. Proposed: current calendar month in UTC+7. Do not fix this homepage policy until the user chooses it; general reports accept explicit periods and can proceed.
- Verified shop name/contact/address/hours and licensed product imagery: obtain before publishing content. Prototype values remain examples.
- Load/latency targets and deployment/backup schedule: not quantified by the report; no performance claim is made by correctness tests.

These questions do not block the schema, account, catalog, stock or order backend tasks.
