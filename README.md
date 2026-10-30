# Phone Store

Clean rebuild of the phone store backend, started on 2026-10-30. The previous implementation and its historical QA claims have been removed from this branch. Git history and existing SQL databases are preserved.

The shared backend implements the revised report’s K01-K15 and A01-A22 server flows: accounts, public/internal catalog, variants/pricing, suppliers, persistent purchase drafts, stock/journal, carts, COD orders and fulfillment, customer administration, invoice data and reports. SQL Server is the only database. New customer pages, WinForms screens and physical printing remain the next phase. See [commerce contracts](docs/COMMERCE.md), [database/demo operations](docs/DATABASE.md) and [requirement evidence](docs/TRACEABILITY.md).

## Structure

- `PhoneStore.Core`: shared domain rules and service contracts.
- `PhoneStore.Infrastructure`: EF Core and SQL Server persistence.
- `PhoneStore.Web`: shared ASP.NET Core API/MVC host. No customer UI is implemented yet.
- `PhoneStore.Checks`: executable checks; failures produce a nonzero exit code. Use `dotnet run`, not `dotnet test`, for this runner.

## Local development

Requires .NET 10 SDK and SQL Server. The dependency versions follow the previous repository's .NET 10 baseline; no major upgrade was made. Run from this directory:

```powershell
dotnet restore PhoneStore.slnx
dotnet build PhoneStore.slnx -c Release --no-restore
$env:ConnectionStrings__Store = 'Server=localhost;Database=PhoneStore_Rebuild;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
# Explicitly create a NEW database and apply the clean schema, without accounts or sample data (use explicit bootstrap commands afterward):
dotnet run --no-build -c Release --project src/PhoneStore.Web -- --migrate
dotnet run --no-build -c Release --project src/PhoneStore.Web -- --urls http://localhost:5080
```

`GET /health/live` checks the process. Successful readiness includes `environment` and a stable SHA-256 `fingerprint` of the configured application/environment/server/database target, excluding credentials. Remote ADMIN clients compare those with their configured expected values before login and must separately validate HTTPS/TLS; this fingerprint is not a certificate fingerprint. `GET /health/ready` returns 503 until the exact migration lineage/checksum is installed. Normal startup does not create, migrate, seed or reset a database. The explicit `--migrate` command creates the new schema or verifies/replays its version without duplication; it rejects legacy/unmanaged schemas and altered applied checksums. Do not point it at an old application database. There are no default credentials. `TrustServerCertificate=True` is for local development only; configure a trusted SQL certificate for deployment.

## Registration API (K01)

Fetch `GET /api/security/token`, preserve the returned anti-forgery cookie, then send its `token` value in `X-CSRF-TOKEN` to `POST /api/auth/register`. JSON fields are `username`, `password`, `fullName`, `phone`, optional `email` and `address`. Unknown fields are rejected; clients cannot set role or passwordHash. Input policies are in [contracts](docs/CONTRACTS.md).

Success returns HTTP 201 `{id,username,role}` with role CUSTOMER. The service saves the account and profile in one SQL transaction, hashes the password, enforces unique username/phone/email, and does not log the customer in. Missing/invalid CSRF, malformed input and duplicates return safe Vietnamese errors. There are no registration pages or public ADMIN registration.

`POST /api/auth/login` accepts `{username,password,surface}` with surface `customer` or `admin`, returning a safe identity/expiry and protected session cookie. Sessions last eight hours without sliding renewal and are validated from SQL Server on each request. `GET /api/auth/session` requires a live session; `GET /api/admin/session` requires ADMIN. `POST /api/auth/logout` revokes only the current session. `POST /api/account/password` verifies the current password and revokes all sessions of that account when changing it. `GET/PUT /api/account/profile` reads/edits only the caller's CUSTOMER profile. Mutations require CSRF; successful logout/password/profile updates return 204. Use a fresh CSRF token after identity changes. See [account contracts and evidence](docs/ACCOUNTS.md). Explicit ADMIN provisioning and atomic demo seeding are documented in [database operations](docs/DATABASE.md). Account UI remains deferred.

## Checks

```powershell
dotnet run --no-build -c Release --project tests/PhoneStore.Checks
$env:PHONESTORE_QA_SERVER = 'localhost'
$env:PHONESTORE_QA_DATABASE = 'PhoneStore_QA_Local'
$env:PHONESTORE_TEST_CONNECTION = 'Server=localhost;Database=PhoneStore_QA_Local;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --integration
```

The HTTP checks start and stop their own backend on a loopback port. Integration checks require an explicit matching QA connection, create unique databases and remove only those databases afterward. They do not touch `PhoneStore`, `PhoneStore_Rebuild` or a pre-existing QA database. Checks cover migrations/constraints/rowversions; account/session/role/CSRF and ownership; catalog validation and pricing; receipt/stock reconciliation; cart merge and checkout idempotency; last-unit and competing order-action races; late SQL rollback for passwords, purchases, checkout, lock and COD completion; invoices and UTC+7 reports. A separate disposable SQL database proves the complete 20-product/customer demo, audit, non-destructive rerun and actual HTTP checkout-to-paid-invoice/report workflow.

See [requirements trace](docs/TRACEABILITY.md), [data/service contracts](docs/CONTRACTS.md), [decisions](docs/DECISIONS.md) and [contribution rules](CONTRIBUTING.md) for the implemented server contracts, actual merged evidence and UI/deployment boundaries.

Private Word requirements, the implementation prompt and the option A design archive remain local and ignored by Git. They must not be uploaded to issues or PRs. Shop contact details and product assets must be configured from verified data before any public release.
