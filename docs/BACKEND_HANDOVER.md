# Backend handover — PR89 review

This is a documentation-only follow-up to PR89. No merge, tag, branch deletion, runtime repair or Git-history rewrite is performed. Backend runtime baseline: **dbc888ab962bbfd9b3f19514ce06b00d0d328e1c**. Initial reviewed PR89 head: **7431544feab95fce368da6a88a5f231530a77183**. The amended documentation head and its exact successful CI job are recorded in the final PR review; [PR89 Checks](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/89/checks) always resolves the current head. A previous head's success must not be used to pass GATE.05 after a push.

## Run and test

Requires .NET10 SDK and SQL Server, with externally supplied ConnectionStrings__Store and the permissions needed for the chosen fresh DB. Follow the full [README](../README.md) restore/build/migrate/start sequence. Never run the rebuild migration on a live/legacy DB; use a new QA/dev DB. Normal startup does not migrate, seed or create accounts. Local example uses Windows integrated authentication and has no password; production credentials/certificates are external configuration.

```powershell
dotnet restore PhoneStore.slnx
dotnet build PhoneStore.slnx -c Release --no-restore
# Set ConnectionStrings__Store externally to a NEW QA/dev database, and choose the environment.
dotnet run --no-build -c Release --project src/PhoneStore.Web -- --migrate
dotnet run --no-build -c Release --project src/PhoneStore.Web -- --urls http://localhost:5080
```

Check /health/live and /health/ready. Stop that host before reusing its port. For tests, set matching PHONESTORE_QA_SERVER, PHONESTORE_QA_DATABASE and PHONESTORE_TEST_CONNECTION; the target name must start PhoneStore_QA_. The runner creates/disposes its own unique QA DBs, not the configured live DB. Use `dotnet run`, not `dotnet test`:

```powershell
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --backend-smoke
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --integration
```

Both --no-build commands require the preceding Release build. An independently opened Test section is not a substitute for restore/build. The optional --audit-db CLI is read only; Development logging can precede its JSON, so do not parse all stdout as one JSON document without accounting for logs.

## Independent reproduction and runtime diff

A new, initially empty directory received a non-shallow clone of every advertised GitHub branch/tag and checked out7431544. There were no existing bin/obj outputs. The README was executed in order; only illustrative DB names were replaced by a generated QA name. Restore/build/migrate/start all succeeded, smoke passed6checks and integration passed251checks. A supplemental audit confirmed18application/session tables at0rows and1migration-ledger row. No README command failed. The first automation parser incorrectly expected audit stdout to be pure JSON; that parser was corrected without changing a runtime command or source.

`git diff --exit-code dbc888a <PR89-head> -- src` is empty, covering Core, Infrastructure, Web host/API/security, services and migrations. The subsequent review changes only Markdown and sanitized documentation evidence; tests/tool remain byte-identical to7431544. See [review evidence](evidence/backend-closeout/pr89-review.json) and the final current-head CI check. The final report lists every changed path; the original PR consists of documentation, four test files and one inventory tool.

## Accepted differences and boundaries

| ID / decision | Accepted behavior |
| --- | --- |
| A03.02 | Shared own-password operation supports both roles; ADMIN uses the same service/API operation, never an arbitrary target account. |
| FE.04 | Backend error mapping is not bijective with the proposed contract; e.g. invalid cart/recipient/payment-method/empty-cart cases can share INVALID_INPUT. Reconcile actual cart and use documented mapping; do not invent stable codes. |
| K09.02 / WEB.K09.02 | API PASS for missing/inactive404, owned variant DTOs and no writes. The API does not bind selected variantId. Kiểm tra cặp sản phẩm–biến thể chuyển sang WEB.K09.02, khác biệt được chấp nhận. The read-only association guard belongs to the separately approved MVC adapter. |
| T9 / DEC.05 | Return reason optional: absent/blank/whitespace succeeds; provided text is trimmed and limited to1000characters. Version still required/advanced; same-state order history and return journal retained. |
| Repeated physical count | Same OperationKey and unchanged count with a refreshed version returns INVALID_INPUT; stale version can return CONCURRENT_UPDATE. No second stock effect. |
| Empty purchase draft | An empty draft may be saved and cancelled; confirmation requires lines, active supplier and valid version. |
| DEC.01 | Password10-128characters with a letter/digit; current phone normalization retained; nullable email normalized; no requirement that the new password differ from the old one. |
| DEC.02 | Guest merge max(server,guest), cap to live availability, drop inactive/missing/zero-available lines, return adjustments; unchanged-input retry does not accumulate. |
| DEC.03 | Inactive brand/category excludes associated products from public queries without deleting/cascading them; historical references remain. |
| DEC.06 | Same owner/key/same immutable checkout intent returns the existing order; changed payload conflicts. Other operations reject repeats with current version/state/error mapping and no second effect. |
| DEC.07 | Current UTC+7 calendar month, top8products, quantity/revenue/ID tie-breaking, no previous-period fallback. |
| BASE.05 | **Dựng DB mới + chạy lại: PASS; nâng cấp từ DB cũ: N/A.** Only new-DB rebuild001 is supported; no legacy003 upgrade path or backfill claim. |

## MVC transfers and final224 counts

| Backend ID | Status | Destination |
| --- | --- | --- |
| K10.03 | NOT RUN — chuyển sang MVC | WEB.K10.03/WEB.K10.04, real localStorage/merge/cleanup |
| BASE.12 | NOT RUN — chuyển sang MVC | WEB.BASE.07/WEB.K10.03/WEB.K10.04, RAM/browser/server state and cache |
| FE.08 | NOT RUN — chuyển sang MVC | Real UI/form/responsive/focus/confirmation evidence; printing follows its applicable client |

The original224IDs and priority labels are unchanged. BASE.05 keeps one ledger N/A for its unsupported upgrade clause, while fresh/replay evidence is explicitly PASS. GATE.01/.03 pass for the accepted server scope; GATE.05 passes **only** when the required CI on the actual latest PR head is SUCCESS. Pending/skipped/cancelled does not qualify. Until that new-head check completes, subtract1PASS/add1NOT RUN from the conditional final table below.

| Priority | Total | PASS | FAIL | BLOCKED | NOT RUN | N/A |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| P0 |114|112|0|0|1|1|
| P1 |89|88|0|0|1|0|
| P2 |21|20|0|0|1|0|
| Total |224|220|0|0|3|1|

Full ledger: [BACKEND_CHECKLIST_FINAL](BACKEND_CHECKLIST_FINAL.md). Exact final SHA/job result is recorded in PR89 and the local .artifacts/contract-audit/CHECKLIST_RESULTS.md. Initial head7431544 had [251 successful checks](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41165344352/job/124431599485); that historical check is not a substitute for the amended documentation head's required CI.

## Security and private inputs: precise scope

The committed Node inventory tool scans tracked text and locally reachable refs; the independent review additionally scans every decodable text blob, without extension filtering, including JWT and protected cookie/antiforgery patterns. No credential is sent to a service for validity testing and no matched value is exported. Original full GitHub clone:5advertised branches,0tags,156tool-selected current text files/529historical text blobs; expanded scan160current/601historical text blobs. No real application secret/token/cookie/remote-connection candidate remained. QA fixtures contain29current matches; historical vendor event namespaces are false positives, not credentials. These are occurrence counts, not live-account counts. Binary content is not decoded; private input filenames are independently inventoried.

No Word/archive/extracted prototype file is tracked at PR89 head or in history reachable from all fetched GitHub branches/tags. Local authoring refs are different: Codex recovery trees reach21prototype blobs and8illustrative prototype password-literal occurrences. They are not remote branch/tag history and were not removed. The extracted directory is currently untracked but not ignored; Word/archive patterns are ignored. Consequently, this handover does **not** falsely confirm that every local Git object contains no private/demo material.

It also does not claim zero sample identifiers outside tests: DemoSeeder contains the accepted non-secret illustrative names and receives passwords externally. It runs only on explicit command and is restricted to QA/dev by procedure, not by a runtime DB-name guard. No default application password is seeded by migration/startup; the newly audited QA application DB has0accounts/sessions. Existing live databases, ignored local credential stores and arbitrary encodings are outside the review. See [security review](BACKEND_SECURITY_REVIEW.md).

## Known limitations

- Seeder target-name guard is not implemented; explicit QA/dev-only use is mandatory and live DB use prohibited. Hardening is a later task.
- No scheduled backup automation is supplied; no unattended backup/recovery schedule is claimed.
- No production deployment, certificate/key operations, access/protection changes or load benchmark is accepted by correctness tests.
- No legacy database upgrade path; never apply the rebuild to a live/legacy DB.
- Private local recovery objects remain; no history cleanup is authorized by this review.

## Facts for updating the Word report

**Actual migration path:** [src/PhoneStore.Infrastructure/Migrations/001_InitialStore.sql](../src/PhoneStore.Infrastructure/Migrations/001_InitialStore.sql). It already defines the final schema, including Cost/ProfitPercent/Price. Migration ID=rebuild/001_initial_store; real ledger=dbo.__PhoneStoreMigrations.

**Migration locking:** [DatabaseMigrator](../src/PhoneStore.Infrastructure/Data/DatabaseMigrator.cs) first obtains an Exclusive SQL Server sp_getapplock in master, resource PhoneStore:create:<database>, owner Session, timeout30000ms, while creating a missing DB. It releases that lock on success/error. It then opens the application DB, begins a Serializable transaction, and obtains an Exclusive transaction-owned sp_getapplock on PhoneStore:rebuild:migrations with30000ms timeout. The SQL script and ledger/checksum write commit together; replay verifies the single lineage/hash. Unknown/altered lineages are refused. This is distinct from the business writer lock PhoneStore:write.

**Current services:** account interfaces are in PhoneStore.Core/Accounts; implementations in PhoneStore.Infrastructure/Accounts. Commerce services below are in PhoneStore.Infrastructure/Commerce and called directly by existing endpoints. There is no one current runtime class named StoreService replacing all of them.

| Services | Scope |
| --- | --- |
| AccountRegistration / IAccountRegistration | Atomic CUSTOMER/account/profile registration, validation, hashing/uniqueness; no automatic login |
| AccountSessions / IAccountSessions | Role-surface login, fixed8h SQL sessions, per-request validation and current-session revocation |
| AccountProfiles / IAccountProfiles; AccountPasswords / IAccountPasswords | Own CUSTOMER profile; own password verification/change and all-session revocation |
| CatalogService; PublicBestsellerService | Public/internal product queries and eligible variants/live prices/available; public month/top8ranking |
| CartService | Read-only guest preview, own persisted cart, add/update/remove and approved merge; no stock reservation |
| CheckoutService | Place/resolve COD order, confirmation comparison, request-key retry, snapshot/reservation/payment/history/cart transaction |
| OrderQueryService; OrderWorkflow (OrderDecisions/OrderFulfillment partials) | Owned/admin order queries; cancel/confirm/dispatch/complete/fail/receive-return with version/state/stock/COD checks |
| ProductService; LookupService | ADMIN products/variants/pricing and brand/category management; public lookups |
| SupplierService; PurchaseService (PurchaseConfirmation partial) | ADMIN suppliers and persisted purchase drafts/confirmation/cancellation with stock/cost/price effects |
| InventoryService; CustomerAdminService | ADMIN stock/count/journal; customer search/profile/lock and session revocation |
| InvoiceService; ReportService | Read-only eligible invoice snapshots; revenue/bestsellers/current-stock reports |
| BusinessKernel / Rules / DateWindow / OrderReadModel | Shared authorization/transactions, validation and query/read-model helpers; not an extra UI or HTTP backend |

Operational helpers in Infrastructure/Data are DatabaseMigrator, AdminProvisioner, DemoSeeder and DataAudit; StoreDbContext supplies EF mappings/unit-of-work behavior.

**IStoreService location and current merge status:** the interface is absent from the immutable backend-v1.0 release. The separately approved [PR87](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/87) was subsequently merged into develop as **be460cb59c0bfc8f0a49f1787bead0b0487579d7**, with [post-merge CI SUCCESS](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41178900613). Its merged [Core interface](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/blob/be460cb59c0bfc8f0a49f1787bead0b0487579d7/src/PhoneStore.Core/IStoreService.cs) and [MVC adapter](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/blob/be460cb59c0bfc8f0a49f1787bead0b0487579d7/src/PhoneStore.Web/Storefront/MvcStoreAdapter.cs) expose ReadCartAsync and AddToCartAsync at that merge. The MVC account step extends this MVC boundary by delegating to existing account interfaces; it does not alter those services. The earlier PR89 inventory captures its historical, pre-merge state and must not be read as current branch status.
