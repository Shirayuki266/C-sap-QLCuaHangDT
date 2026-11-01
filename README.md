# Phone Store backend

.NET10 + ASP.NET Core API + EF Core/SQL Server. The accepted backend implements K01-K15/A01-A22 server behavior; MVC/WinForms UI is a separate stage. See [API mapping](docs/API_COMPATIBILITY.md), [final backend checklist](docs/BACKEND_CHECKLIST_FINAL.md) and [database details](docs/DATABASE.md).

## Run from a new database

Requires the .NET10 SDK and SQL Server. Run at the repository root. The following is a **local Windows-authentication example**, not a production connection or credential:

```powershell
dotnet restore src/PhoneStore.Web/PhoneStore.Web.csproj
dotnet restore tests/PhoneStore.Checks/PhoneStore.Checks.csproj
dotnet build src/PhoneStore.Web/PhoneStore.Web.csproj -c Release --no-restore
dotnet build tests/PhoneStore.Checks/PhoneStore.Checks.csproj -c Release --no-restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__Store = 'Server=localhost;Database=PhoneStore_Dev_New;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --no-build -c Release --project src/PhoneStore.Web -- --migrate
dotnet run --no-build -c Release --project src/PhoneStore.Web -- --urls http://localhost:5080
```

Use a genuinely new DB name; never point this command at a legacy or live DB. `--migrate` explicitly creates/verifies the **rebuild001** lineage and checksum. It installs the final19-table schema, including BIEN_THE.Cost/ProfitPercent/Price; the actual ledger is **dbo.__PhoneStoreMigrations**. No002/003 migration is required or supplied for this rebuild. **Chỉ hỗ trợ dựng DB mới, không có đường nâng cấp từ DB cũ.** Legacy003 price-preserving upgrade coverage is N/A.

Normal host startup does not migrate, seed or create accounts. Check `GET /health/live` and `GET /health/ready`; readiness is503 before migration and200 afterward. There are no default passwords. Stop the host with Ctrl+C. Use a trusted SQL certificate and `TrustServerCertificate=False` outside local development; remote application access requires HTTPS.

## Configuration

| Setting | Required use |
| --- | --- |
| `ConnectionStrings__Store` | Explicit application SQL target, supplied externally; never commit a real connection or password |
| `ASPNETCORE_ENVIRONMENT` | Development locally; Production for a configured deployment |
| `--urls` / `ASPNETCORE_URLS` | Host listening URL; local example above, HTTPS for remote use |
| `PHONESTORE_QA_SERVER`, `PHONESTORE_QA_DATABASE`, `PHONESTORE_TEST_CONNECTION` | Matching explicit QA target for SQL checks; QA DB name must start with PhoneStore_QA_ |
| `PHONESTORE_BACKEND_SMOKE_REPORT` | Optional path for sanitized smoke/schema JSON; never includes connection strings, cookies or tokens |

SQL-authentication deployments must inject credentials through environment/secret management. The repository contains no real application connection or production password; disposable QA fixture credentials are identified in the [security review](docs/BACKEND_SECURITY_REVIEW.md).

## Test and reproduce the empty-DB smoke

The checks are an executable runner: use `dotnet run`, **not** `dotnet test`. This section requires the restore and Release build above before either `--no-build` command; do not start here in a fresh checkout without building first.

```powershell
$env:PHONESTORE_QA_SERVER = 'localhost'
$env:PHONESTORE_QA_DATABASE = 'PhoneStore_QA_Local'
$env:PHONESTORE_TEST_CONNECTION = 'Server=localhost;Database=PhoneStore_QA_Local;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --backend-smoke
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --integration
```

The smoke creates an isolated empty QA DB, proves startup does not migrate, runs the actual `--migrate` CLI twice, inspects INFORMATION_SCHEMA/sys, runs the read-only audit, starts the host and checks public/protected/CSRF behavior. All18 application/session tables stay empty. It removes only the unique DB it created. The full CI integration run also executes this smoke. No checks target PhoneStore_Rebuild or a demo/live database.

## Customer MVC

Customer MVC is delivered in separate reviewable steps. Foundation PR87, Account PR91, Catalog PR93, Cart PR95, Checkout PR97 and Orders PR99 are merged. Final step6 starts at `3bc247dd79ee3eab7919cdfe60fa34671a448f26` and adds complete browser/admin-API/SQL flow acceptance, shared loading/error feedback and whole-site checks. See [customer website handover and browser test commands](docs/MVC_HANDOVER.md), [source boundary audit](docs/MVC_ARCHITECTURE_AUDIT.md) and [all99 official MVC results](docs/MVC_CHECKLIST_RESULTS.md). Function-specific evidence remains in the accounts/catalog/cart/checkout/orders reports. Existing backend services, schema and admin API are unchanged. This acceptance PR awaits user review and does not merge itself.

## Windows Admin

W1 adds a separate HTTP-only native WinForms application and net10.0 client. W2 adds products, multi-variant editing, brands and categories; W3 adds customers/suppliers, an active-supplier picker and minimal read-only customer-order details. On Windows: `dotnet run --project src/PhoneStore.Admin -c Release`. Configure the backend URL on Login; there are no default credentials. See [W1 setup/boundaries/tests](docs/WINFORMS_W1.md), [W2 catalog evidence](docs/WINFORMS_W2.md), [W3 contact/session/SQL/UI evidence](docs/WINFORMS_W3.md) and the [113-ID Admin ledger](docs/WINFORMS_CHECKLIST_RESULTS.md). Purchasing/stock, full order operations and reports remain W4–W6; the W4 purchase handoff is disabled. This PR does not merge itself.

The full solution now includes Windows targets and is built on Windows. The explicit backend commands above and unchanged Ubuntu CI build only Web/Checks; a new cross-platform Admin.Client.Checks step uses isolated SQL QA. Windows form checks run separately with a fake API and publish actual control captures.

## Provisioning and sample data

ADMIN provisioning requires explicit credentials supplied externally via `PHONESTORE_ADMIN_USERNAME`/`PHONESTORE_ADMIN_PASSWORD` and the separate `--provision-admin` command. See [secure-input instructions](docs/DATABASE.md); do not put credentials in shell command arguments or source.

The optional seeder runs **only by the explicit `--seed-demo` command**, only on a separate **QA/dev DB**, and **must never run on a live DB**. It has no default password: `PHONESTORE_DEMO_ADMIN_PASSWORD` and `PHONESTORE_DEMO_CUSTOMER_PASSWORD` must be supplied externally. The existing runtime does not enforce a QA-name restriction; adding that guard is an accepted later improvement, not part of this closeout. The empty-DB startup/smoke never invokes it.

Private Word files and the prototype archive are ignored and untracked. The locally extracted prototype directory is untracked, but is not covered by the current ignore rules: never stage it. The full GitHub branch/tag history was checked separately from local Codex recovery refs; those local recovery trees contain prototype objects, so do not claim every local Git object is free of private inputs. No refs were removed or history rewritten. See [handover and review scope](docs/BACKEND_HANDOVER.md).

Backend tagging remains pending explicit user approval; this PR neither merges itself nor creates `backend-v1.0`. See [closeout evidence and tag procedure](docs/BACKEND_CLOSEOUT.md).
