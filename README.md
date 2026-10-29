# Phone Store

Clean rebuild of the phone store backend, started on 2026-10-30. The previous implementation and its historical QA claims have been removed from this branch. Git history and existing SQL databases are preserved.

The requirements cover K01-K15 (customer website) and A01-A22 (WinForms administration). Only COD payments are in scope. Business functionality is pending; the foundation does not implement the 37 use cases. New customer pages and WinForms screens are deferred until all backend requirements, SQL Server integration tests and CI pass on `develop`.

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
dotnet run --no-build -c Release --project src/PhoneStore.Web -- --urls http://localhost:5080
```

`GET /health/live` checks the process. `GET /health/ready` returns 503 until the database schema is installed. Startup does not create, migrate, seed or reset a database. There are no default credentials. `TrustServerCertificate=True` is for local development only; configure a trusted SQL certificate for deployment.

## Checks

```powershell
dotnet run --no-build -c Release --project tests/PhoneStore.Checks
$env:PHONESTORE_QA_SERVER = 'localhost'
$env:PHONESTORE_QA_DATABASE = 'PhoneStore_QA_Local'
$env:PHONESTORE_TEST_CONNECTION = 'Server=localhost;Database=PhoneStore_QA_Local;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --integration
```

The HTTP checks start and stop their own backend on a loopback port. Integration checks require an explicit matching QA connection, create a unique database and remove only that database afterward. They do not touch `PhoneStore`, `PhoneStore_Rebuild` or a pre-existing QA database. At this foundation stage they check configuration, safe errors, liveness/readiness and SQL transaction rollback; they do not certify business transactions or concurrency.

Private Word requirements, the implementation prompt and the option A design archive remain local and ignored by Git. They must not be uploaded to issues or PRs. Shop contact details and product assets must be configured from verified data before any public release.
