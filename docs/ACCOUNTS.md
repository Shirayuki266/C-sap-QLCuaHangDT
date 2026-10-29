# Account API and verification

All user-facing messages are Vietnamese. Public registration always creates CUSTOMER, never ADMIN. No default or sample operational credentials are provisioned.

## Login and session queries (K02/A01)

`POST /api/auth/login` accepts `{username,password,surface}`. Surface is `customer` (default) or `admin`; it must match the active account's stored role. Unknown JSON fields and surfaces are rejected. Missing/incorrect credentials, disabled accounts and wrong-role credentials fail without creating a session. Password values/hashes and session IDs never appear in the response.

Before every mutation, obtain `GET /api/security/token` with the same cookie container and send its `token` in `X-CSRF-TOKEN`. Obtain a fresh token after identity changes: a token issued anonymously is not a token for the signed-in identity. Successful login returns `{id,username,role,expiresAt}` and the protected `PhoneStore.Session` cookie. It does not put tokens into localStorage. Signed-in registration/re-login fails with INVALID_STATE; sign out before switching accounts once logout is implemented.

The source fixes the session lifetime at eight hours without sliding renewal. Both the protected cookie ticket and SQL PHIEN expiry are fixed. Every authenticated request checks the stored session/account relationship, exact expiry, current active account and unchanged role. A missing/revoked/expired session or a modified cookie fails authentication. The cookie is HttpOnly, SameSite Lax and Secure outside Development/Testing; local HTTP use requires Development. Testing uses isolated ephemeral protection keys; deployment key storage/rotation and HTTPS remain deployment tasks.

`GET /api/auth/session` returns the current safe identity and expiry for either role. `GET /api/admin/session` returns the ADMIN identity and requires ADMIN. Anonymous/invalid sessions receive JSON 401 UNAUTHORIZED; wrong-role access receives JSON 403 FORBIDDEN, with no login-page redirect. Neither query renews the session or cookie.

Implementation uses the framework's [cookie authentication and principal validation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0). Actual application evidence is [SQL session checks](../tests/PhoneStore.Checks/SessionChecks.cs) and [HTTP session checks](../tests/PhoneStore.Checks/SessionHttpChecks.cs): failed credentials/role injection/CSRF, safe protected-cookie login, exact expiry/no sliding renewal, session revocation, disabled accounts, role changes, 401/403, and K01's authenticated-user guard. Tests use their own SQL databases and users; no operational accounts are seeded.

## Logout (K03/A02)

`POST /api/auth/logout` requires the authenticated cookie and its current CSRF token. It accepts no caller-selected actor/session: these are taken from the validated server context. It atomically deletes only the current PHIEN row, clears the cookie after commit and returns 204. Other devices of the same account remain signed in. Expired/revoked/unauthenticated requests return 401; failed CSRF returns 400 and deletes nothing. Replaying a captured old cookie or concurrent logout cannot revoke another device or restore the deleted session. See [SQL/HTTP logout checks](../tests/PhoneStore.Checks/LogoutChecks.cs).

## Password change (K05/A03)

`POST /api/account/password` accepts `{currentPassword,newPassword}` and requires a live session plus CSRF. Actor/session come from server context; clients cannot choose an account. The service revalidates the current SQL session/account under the shared write lock, verifies the old password, validates the replacement with the same policy as registration, stores a salted hash and deletes all sessions of that account in one transaction. Only after commit does the API clear the current cookie and return 204. Every device must log in again; other accounts are unaffected.

Wrong current password, invalid replacement, foreign JSON members or failed CSRF preserve all state. A competing password change cannot use a session revoked by the winner. A late SQL write failure rolls back both the hash update and session deletions. [Password service/HTTP checks](../tests/PhoneStore.Checks/PasswordChecks.cs) prove CUSTOMER/ADMIN success/failure, old-cookie replay, old/new credential behavior, account boundaries, concurrency and forced SQL rollback. Profile editing and customer lock/unlock management remain separate tasks.

Reading Active during validation is not a substitute for the future lock command atomically revoking every related session.
