# WinForms W1 — foundation and accounts

Base: green develop / `web-v1.0`, `f8a0801e055b1e6c3dc310eee6b2607a2770c31e`. [Issue #102](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/issues/102); [PR #103](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/pull/103), branch `feature/102-admin-foundation`. Inputs are the user's latest 08/09 files `(2)`; official 113 IDs retain 48 P0 / 59 P1 / 6 P2. W1 plans 31 IDs (15/14/2): 30 PASS and BASE.15 NOT RUN. Later modules remain explicitly unimplemented; no automatic merge.

## Application boundary

Four new projects are added. `src/PhoneStore.Admin.Client` targets net10.0 and owns HttpClient, an in-memory CookieContainer, exact request DTOs, CSRF, readiness identity, safe error translation, intent keys, recovery guards and display formatting. `src/PhoneStore.Admin` targets net10.0-windows and uses native WinForms; Login/Main/password forms receive IAdminApi. The application composition root constructs the client. Neither shipping project references Core, Infrastructure, Web, EF or a SQL package.

`tests/PhoneStore.Admin.Client.Checks` is a separate net10.0 executable using the actual Web host and disposable SQL QA databases. Its permitted backend references and SQL assertions are test-only. `tests/PhoneStore.Admin.Tests` is a Windows STA executable using actual form controls with a fake IAdminApi. It also has an explicitly invoked real-QA UI mode, launched by the separate QA runner; no QA switch/endpoint is added to either shipping application.

Existing Core/Infrastructure/Web, migrations, API and `tests/PhoneStore.Checks` have empty diff. Solution entries are added; existing Ubuntu CI steps remain byte-for-byte unchanged, with one new client-check step appended. A separate Windows job builds/runs form checks and uploads captures. Ubuntu does not build Windows projects or the whole solution.

## Run and configuration

Requires Windows and .NET10 SDK with Desktop reference/runtime support. Local verification found SDK10.0.401 and Microsoft.WindowsDesktop.App.Ref/runtime10.0.12; the complete Windows solution built without warnings/errors. The new Windows CI job supplies independent runner evidence.

```powershell
dotnet build src/PhoneStore.Admin -c Release
dotnet run --project src/PhoneStore.Admin -c Release --no-build
```

Backend startup/migration remains the accepted README process; Admin does not migrate, seed or provision. Configure only the backend origin URL through Login's configuration dialog; default is local port5080. HTTP is accepted only for loopback; other targets require HTTPS and normal certificate validation. Optional expected environment/fingerprint values block mismatches. The fingerprint identifies the application/database target, not a TLS certificate.

Allowlisted settings are stored in `%LOCALAPPDATA%/PhoneStore.Admin/settings.json`: BaseUrl, ExpectedEnvironment, ExpectedFingerprint only. Passwords/cookies/tokens are not persisted or logged. There are no default credentials or remember-password feature. QA fixture credentials are generated in memory by test code and never passed in command arguments or written to evidence.

## Session and transport behavior

Login verifies `/health/ready` and sends `{username,password,surface:"admin"}`. Each POST obtains a new `/api/security/token` value and sends `X-CSRF-TOKEN` using the same memory cookie jar. Automatic redirects and blind retries are disabled. Client errors are controlled Vietnamese messages; server500 exposes only a sanitized trace identifier, not body text or stack traces.

Passwords are removed from controls before the async operation and again on completion/close; immutable request strings exist transiently in managed memory while sending, not in settings/logs. Bad login/CUSTOMER/locked identities get generic feedback. SQL session expiry/revocation clears local identity and the actual application context returns to Login; subsequent commands are rejected locally without another POST. Invalid password changes preserve the account hash; valid changes revoke all sessions and force login.

An uncertain POST result locks subsequent writes. Failed GET reconciliation does not release that guard. A lost or malformed login response is reconciled through the existing session GET: an authoritative401 permits explicit login again; a valid received cookie recovers its session without repeating credentials. The Login connection-check action performs that recovery when needed. Other future commands must choose their object/list/journal read according to the actual API; W1 does not claim those later workflows are implemented.

Logout clears the local cookie jar even if the server cannot be reached. Closing Main makes a best-effort logout with a two-second bound, then exits; ordinary logout returns to Login. If connectivity prevents revocation reaching the server, its fixed-expiry SQL session may survive until expiry; clearing a client cookie cannot guarantee remote revocation without a response. No claim of guaranteed server logout under network loss is made.

## Verification and evidence scope

Configure matching `PHONESTORE_QA_SERVER`, `PHONESTORE_QA_DATABASE`, `PHONESTORE_TEST_CONNECTION` externally as described in README. The test guard requires a PhoneStore_QA_ target; it creates and drops only its own random database. Never use a demo or live target.

```powershell
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release
dotnet run --project tests/PhoneStore.Admin.Tests -c Release
# Windows only; build both runners first. Actual controls -> Kestrel -> SQL QA:
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release --no-build -- --desktop-smoke
```

13 client groups exercise real authentication/CSRF/session/password/reconciliation endpoints with SQL before/after assertions. Backend business tables, inventory, order money, history and journal stay unchanged. The only expected changes are the tested account hash and session rows. Error transport injections are explicitly test-only. Architecture scans cover source/projects and restored assets; Windows checks inspect Admin assets because Ubuntu deliberately does not restore/build Windows targets.

Eight STA form groups use PerformClick and the actual Enter-dialog-key path, busy/duplicate guards, cleared password fields, retained username, configuration validation, settings roundtrip, production application-context session navigation and bounded close. They generate eight DrawToBitmap captures with QA/fake-API labeling. Two separate actual Kestrel/SQL QA captures prove Login/Main connectivity and logout session removal. They are automation evidence, not a claim of manual full W2–W6 operating flows. Existing backend/MVC regression remains 322 groups and is rerun unchanged.

The independent rendered review and fix verdict are retained at their stated scope. Native disabled-button colors are supplemented by readable enabled progress text. No HTML/CSS detector applies to native WinForms. 125%/150% DPI and whole-phase UI acceptance remain W6, not implied by AutoScaleMode.Dpi or these captures.

The declared main/login minimum is 1280x800 logical pixels; physical captures scale with DeviceDpi. Capture JSON records actual bounds/DPI. WinForms constrains MinimumSize to the screen working area; a 1024x768 hosted runner cannot validate this surface. The Windows job therefore sets its disposable desktop to 1920x1080 with the built-in ServerCore cmdlet before running the unchanged strict minimum assertions. Local screen resolution is not changed. Full W6 DPI coverage remains deferred.

Sources: [WinForms MinimumSize implementation](https://github.com/dotnet/winforms/blob/main/src/System.Windows.Forms/System/Windows/Forms/Form.cs), [Microsoft Set-DisplayResolution](https://learn.microsoft.com/en-us/powershell/module/servercore/set-displayresolution?view=windowsserver2025-ps).

## Remaining work and ledger

See [113-ID ledger](WINFORMS_CHECKLIST_RESULTS.md) and [evidence](evidence/admin-w1/). `WIN.BASE.15` remains NOT RUN: a shared safe-default confirmation dialog is implemented and tested, but confirmation wiring for cancellation/failure/customer lock/purchase confirm/inventory/product/lookup actions belongs to W2–W5. That partial infrastructure proof does not pass the entire cross-module criterion. All other out-of-W1 IDs remain NOT RUN with their planned stage; no WinForms gate is claimed complete.

No service/API discrepancy requiring a backend repair was found. Functional modules, domain command/race proofs, report charts/printing, full DPI matrix, deployment and phase handover remain their later stages. Exact final PR-head Ubuntu/Windows CI is bound in PR verification after checks complete; any new commit requires fresh checks.

Implementation snapshot `4b5f3d940fcf0291edeeb9c2da9267355ced47ff` passed both jobs in [CI run 41302051431](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41302051431): unchanged 322 regression groups, 13 new real-backend Client groups, 8 Windows form groups. Downloaded CI Login/Main images were opened and inspected at 1280x800 / 96 DPI; [CI capture metadata](evidence/admin-w1/ci-form-results.json) is distinct from local captures. The documentation/evidence amendment after that snapshot requires a new exact-head run; the final PR verification comment binds its SHA and CI link. Earlier Windows failures were retained in the audit trail and resolved by configuring the runner display, without weakening size assertions or changing backend code.
