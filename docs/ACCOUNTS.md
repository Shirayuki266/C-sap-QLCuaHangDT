# Account API and verification

All user-facing messages are Vietnamese. Public registration always creates CUSTOMER, never ADMIN. No default or sample operational credentials are provisioned.

## Login and session queries (K02/A01)

`POST /api/auth/login` accepts `{username,password,surface}`. Surface is `customer` (default) or `admin`; it must match the active account's stored role. Unknown JSON fields and surfaces are rejected. Missing/incorrect credentials, disabled accounts and wrong-role credentials fail without creating a session. Password values/hashes and session IDs never appear in the response.

Before every mutation, obtain `GET /api/security/token` with the same cookie container and send its `token` in `X-CSRF-TOKEN`. Obtain a fresh token after identity changes: a token issued anonymously is not a token for the signed-in identity. Successful login returns `{id,username,role,expiresAt}` and the protected `PhoneStore.Session` cookie. It does not put tokens into localStorage. Signed-in registration/re-login fails with INVALID_STATE; sign out before switching accounts once logout is implemented.

The source fixes the session lifetime at eight hours without sliding renewal. Both the protected cookie ticket and SQL PHIEN expiry are fixed. Every authenticated request checks the stored session/account relationship, exact expiry, current active account and unchanged role. A missing/revoked/expired session or a modified cookie fails authentication. The cookie is HttpOnly, SameSite Lax and Secure outside Development/Testing; local HTTP use requires Development. Testing uses isolated ephemeral protection keys; deployment key storage/rotation and HTTPS remain deployment tasks.

`GET /api/auth/session` returns the current safe identity and expiry for either role. `GET /api/admin/session` returns the ADMIN identity and requires ADMIN. Anonymous/invalid sessions receive JSON 401 UNAUTHORIZED; wrong-role access receives JSON 403 FORBIDDEN, with no login-page redirect. Neither query renews the session or cookie.

Implementation uses the framework's [cookie authentication and principal validation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0). Actual application evidence is [SQL session checks](../tests/PhoneStore.Checks/SessionChecks.cs) and [HTTP session checks](../tests/PhoneStore.Checks/SessionHttpChecks.cs): failed credentials/role injection/CSRF, safe protected-cookie login, exact expiry/no sliding renewal, session revocation, disabled accounts, role changes, 401/403, and K01's authenticated-user guard. Tests use their own SQL databases and users; no operational accounts are seeded.

Logout, password change, profile editing and customer lock/unlock management are separate tasks. Reading Active during validation is not a substitute for the future lock command atomically revoking every related session.
