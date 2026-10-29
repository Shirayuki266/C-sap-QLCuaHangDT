# Contributing

Implement the shared backend before adding customer pages or WinForms screens. Read [TRACEABILITY](docs/TRACEABILITY.md) and [DECISIONS](docs/DECISIONS.md); link the original K/A requirement and its basic, alternative, exception, precondition, postcondition and business-rule traces. The local Word report and option A archive are private inputs, not files to upload to an issue.

## Task workflow

1. Check the current worktree, remote, existing issues and PRs. Preserve unrelated changes. Pick one independently verifiable purpose; separate foundation, unrelated domains and UI tasks.
2. Create a concrete issue with Description and Acceptance Criteria. State role, record ownership, contracts, dependencies and exact behavior to prove. Use actual dependency issue numbers.
3. After dependencies merge and post-merge CI succeeds, fetch and create a branch from the newest `origin/develop`: `feature/<issue>-<purpose>`, `fix/<issue>-<purpose>`, `docs/<issue>-<purpose>` or `chore/<issue>-<purpose>`.
4. Implement and validate the task together with tests directly proving it. Update relevant contracts/trace. User-facing messages are Vietnamese; issues, branch names, commits, PRs and technical docs are English.
5. Review the diff and stage only task files. The branch must end with exactly one task commit. Open one PR to `develop`; link the issue and report actual validation and limitations.
6. Check the latest head SHA, correct base, one-commit count, conflicts, acceptance criteria, dependency merges, required checks and reviews. Pending/cancelled/skipped/neutral is not a passing required check. Do not bypass review, use admin merge, disable tests or change protection.
7. Squash-merge only when all conditions hold. Bind the merge to the expected head SHA. Check the merge commit and CI on `develop`; stop dependent work if post-merge CI fails. Verify the issue closes; when the default branch remains `main`, close it manually only after successful merge verification and link the evidence.

Fix review/CI feedback on the same task by amending its single commit and rerunning checks. `push --force-with-lease` is allowed only for your own task branch without collaborators/foreign commits. Never force-push shared or long-lived branches. A new independent defect after merge gets its own issue/branch/commit/PR. Do not default to stacked branches or combine multiple domains to reduce the number of PRs.

Task merge permission is limited to `develop`. Do not promote to `staging`/`main`, deploy production, expand token permissions or alter repository access/protection.

## Verification

Use [README](README.md) for the actual build/check commands. The current test project is an executable runner: `dotnet run --project tests/PhoneStore.Checks`, not `dotnet test`. Any failed assertion must produce a nonzero process exit code. Run relevant local checks before committing; docs-only changes do not need an extra local application run. CI still executes for every task PR.

Migration, stock, price, order, money, session and ownership changes require meaningful checks against isolated **SQL Server** databases. SQLite/EF InMemory or disabled UI buttons do not prove SQL Server concurrency, transactions or authorization. Never run destructive tests, seed, reset or cleanup against a shared database. The runner must validate an explicit QA target, generate its own database name and remove only databases it created. Do not print credentials/connection strings or upload private logs.

No new frontend is allowed until all K01-K15/A01-A22 server branches are implemented, traced and merged; migrations, validation, sessions, ownership, errors, contracts and pagination are verified; important SQL concurrency/transaction tests and independent API checks pass; latest `develop` backend CI succeeds. Printing hardware and other pure UI actions remain explicitly deferred until the UI phase.

## Trace and progress

Distinguish planned, blocked, in progress, awaiting CI/review, merged and verified. Record actual issue/branch/commit/PR, latest-head CI SHA/link and merge SHA; never use mock links or historical test counts as proof of new code. Keep acceptance criteria in issues and concise evidence in PRs/source tests. Do not generate another large historical QA checklist or rewrite the private Word report.
