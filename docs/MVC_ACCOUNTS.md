# Customer MVC accounts — step2 of6

Account K01–K05 extends the foundation merged by PR87 at **be460cb59c0bfc8f0a49f1787bead0b0487579d7**. [Foundation post-merge CI](https://github.com/Shirayuki266/C-sap-QLCuaHangDT/actions/runs/41178900613) succeeded with262 checks. Issue90 starts from that develop revision. The old pre-rebase foundation SHA is retained only inside explicitly historical captures, not current status or source links.

## Boundary and behavior

AccountController calls the in-process IStoreService adapter directly. The previously added interface/adapter gain delegating account methods using IAccountRegistration, IAccountSessions, IAccountProfiles and IAccountPasswords. Existing Infrastructure services, schema/migrations, API Program/Endpoints and session validator are unchanged. Presentation validation mirrors DEC.01; service validation, transactions, ownership, role checks, hashing and session revocation remain authoritative.

| Route | Result |
|---|---|
| GET/POST /Account/Register | Typed allowlist; CUSTOMER registration, no automatic login; PRG to Login with TempData |
| GET/POST /Account/Login | Customer-only service surface; generic credential error; fixed8-hour cookie; local returnUrl only |
| POST /Account/Logout | Current SQL session revoked, cookie cleared, PRG home; GET returns405 |
| GET/POST /Account/Profile | CUSTOMER policy, actor from validated session; read/update own profile; PRG and notice |
| GET/POST /Account/Password | Field syntax/current-password feedback; successful change revokes every session and redirects to Login |

The shared global antiforgery filter protects every Account POST. Account responses are no-store. Successful writes use redirects; errors render the actual form with safe input retained. Password values are cleared from both model and ModelState before redisplay. Duplicate INVALID_INPUT responses remain a form-level summary, without parsing their text to guess a field. Current-password INVALID_INPUT is mapped by the operation contract only after new-password syntax has passed the exact DEC.01 presentation validator; no service-message parsing is used. Login intentionally preserves the existing generic locked-account behavior without revealing account existence or role.

Profile ViewModels contain only display/input fields. Customer identity, role and active flags supplied by a form are ignored. Catalog cost/stock internals, actor/operation IDs and password hashes never enter account ViewModels. Razor encodes profile values, local return URLs are checked on GET and POST, and no controller calls the application's own HTTP API.

## Executed evidence

Release build completed with zero warnings/errors. **277 checks passed** in the full SQL/API/MVC integration runner: the merged262 plus15 new account groups. WebApplicationFactory uses generated, guarded PhoneStore_QA_Rebuild_GUID databases, disposed by the fixture. No test uses PhoneStore_Rebuild, demo or shared data. Detailed snapshots including password hashes/session identifiers remain in memory; they are compared, never exported.

| Actual HTTP/SQL evidence | IDs / assertions |
|---|---|
| Registration + uniqueness | WEB.K01.01–05: one account/profile, no session; ignored role/active/id; duplicate username/phone/email retain safe input and exact SQL state; multiple NULL emails; valid10/128 and invalid password boundaries |
| Login | WEB.K02.01–03,05 and WEB.BASE.05: generic unknown/wrong/locked/ADMIN failures without sessions; local redirect allowlist; actual cookie flags/SQL expiry; already-authenticated visits controlled |
| Logout | WEB.K03.01–03: GET405; exactly current session removed; old cookie replay challenged; other device retained; profile/cart/order/price/stock/history snapshots unchanged; expired logout redirects |
| Profile | WEB.K04.01–04: foreign identity/role overposts ignored; own unchanged phone/email accepted; duplicate edits fully roll back; saved values and notice displayed; old order snapshot preserved |
| Password | WEB.K05.01–03: field errors and no SQL changes on rejection; all sessions removed on success; both devices challenged, old password fails/new succeeds; secrets absent from responses and browser-host logs |
| Shared partial coverage | Every real Account POST missing/bad CSRF returns400 with unchanged SQL. ADMIN Profile/Password return403 and existing admin API works. Script-like profile name/address render encoded. These do not complete whole-site criteria spanning later cart/order pages. |

Browser evidence uses actual Kestrel, SQL Server and Playwright Chromium. Four forms are captured at1440px and390px, plus duplicate/password errors. Each initial Register/Login double submission sends exactly one POST, shows a disabled “Đang xử lý…” action and aria-busy; the later login after password change is counted separately. Browser SQL ends with one QA account/profile and zero sessions. The script checks labels, horizontal overflow, keyboard form access, JavaScript errors and password-free host logs.

- [Browser measurements](evidence/mvc-accounts/browser-results.json)
- [Register desktop](evidence/mvc-accounts/register-desktop.png), [390px](evidence/mvc-accounts/register-mobile.png), [duplicate error](evidence/mvc-accounts/register-duplicate-mobile.png)
- [Login desktop](evidence/mvc-accounts/login-desktop.png), [390px](evidence/mvc-accounts/login-mobile.png)
- [Profile desktop](evidence/mvc-accounts/profile-desktop.png), [390px](evidence/mvc-accounts/profile-mobile.png)
- [Password desktop](evidence/mvc-accounts/password-desktop.png), [390px](evidence/mvc-accounts/password-mobile.png), [field error](evidence/mvc-accounts/password-error-mobile.png)
- [Independent finish review: ship, account extension only](evidence/mvc-accounts/finish-review.md)
- [All99 official checklist rows and counts](MVC_CHECKLIST_RESULTS.md)

## Reproduce

Use README for restore/Release build and externally supplied SQL QA settings. The executable runner, not dotnet test, runs these checks:

```powershell
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --mvc-accounts
dotnet run --no-build -c Release --project tests/PhoneStore.Checks -- --integration
```

The committed browser script currently uses local Windows SQL authentication and sqlcmd. It creates a new random PhoneStore_QA_MvcAccounts_GUID DB, migrates that DB explicitly, starts only its own loopback host, closes its own processes and drops only that generated DB in finally. Credentials for optional provisioning/seeding are withheld. Install browser tooling in the ignored local artifact directory:

```powershell
npm install --prefix .artifacts/mvc-browser playwright@1.56.1
& .artifacts/mvc-browser/node_modules/.bin/playwright.cmd install chromium
$env:PHONESTORE_PLAYWRIGHT_MODULE = (Resolve-Path .artifacts/mvc-browser/node_modules/playwright).Path
$env:PHONESTORE_BROWSER_QA_SERVER = 'localhost'
node tests/browser/mvc-accounts.mjs
```

Browser execution is separate from the existing Linux SQL CI; WAF/account and all regression groups are included in that CI's normal --integration run. QA fixture passwords exist only in test code, not shipping configuration or default accounts. Sanitized evidence deliberately excludes cookies/tokens, connection strings and password/hash snapshots.

## Remaining scope and decisions

**WEB.K02.04 remains NOT RUN**: actual guest localStorage and /Cart/Merge belong to the cart/checkout PR. No merge placeholder is implemented and guest storage is not cleared by account code. Complete WEB.BASE/WEB.UI/WEB.FLOW rows that also require catalog, cart, checkout or orders remain NOT RUN with explicit reasons. The future UI03–UI07/UI13 suite is not replaced by account screenshots. No newly discovered backend defect requires a service/schema/API change.

The review verdict is ship for this ordinary account extension only. It preserves the existing design system. The skill reported pre-existing design-sidecar drift; it was not repaired as part of this feature. The PR must have successful required CI on its exact latest head and user review before merge; this task does not merge automatically.
