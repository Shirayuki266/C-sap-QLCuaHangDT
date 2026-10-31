# Backend credential inventory

Scope: tracked current text files and all locally reachable Git refs, with manual review of candidate findings. Ignored IDE/tool settings, external environment stores and external databases are outside the repository scan. The tool never prints matched values, full source lines or fingerprints of secret values. It performs no network validation of credentials.

Independent PR89 review separates two scopes. A full, non-shallow GitHub clone fetched every advertised branch and tag:5remote branch tips and0tags at review start. The original script inspected156current text files/529historical text blobs; a supplementary no-extension-filter scan inspected160current/601historical decodable text blobs, with JWT and protected-cookie/antiforgery patterns added. No real credential/token/cookie candidate remained after review. QA fixture literals and Bootstrap event namespaces are explicitly classified, not claimed absent. Binary/container content was inventoried by path, not decoded.

No Word/archive/extracted prototype file is tracked at PR89 head or reachable from the fetched GitHub branch/tag history. **The local authoring repository differs:** Codex recovery refs reach21prototype blobs and8illustrative prototype password-literal occurrences. They are absent from the remote clone, are not real application accounts, and were not deleted or rewritten. Word/archive patterns are ignored; the extracted local prototype directory is untracked but not ignored. Therefore an absolute assertion that every local Git object is free of private/demo input would be false. These findings and scope limits are recorded in [handover](BACKEND_HANDOVER.md).

**No real application password, private key, service token or real remote SQL connection was found in the reviewed repository content.** This does not mean there are zero password-like strings: the SQL integration tests and the disposable CI SQL Server service contain explicit QA fixture credentials. They are not application defaults or deployment secrets and must not be reused outside QA. The fixed loopback Windows-authentication examples in README contain no username/password.

The four initial historical non-QA candidates are Bootstrap's DATA_API_KEY event-namespace constants, not credentials. Other broad-pattern candidates were variable assignments that receive externally supplied inputs; the final rules require credential literals/connection-string context. The sanitized inventory records paths, line numbers, object IDs and classifications only. No history rewrite or credential rotation was performed.

The expanded scan sees additional occurrences in vendor source maps; these are the same event-namespace category. Its counts differ from the original extension-filtered inventory because more text formats and a clean remote-only ref set are inspected. No claim of zero sample identifiers outside tests is made: DemoSeeder intentionally retains the separately accepted non-secret identifiers and no default password. Existing live databases were not inspected or changed.

Normal startup and migration do not create accounts. AdminProvisioner requires an explicitly supplied username/password. DemoSeeder retains non-secret illustrative usernames and requires external passwords; there is no default seed password. Per the user's accepted exception, seed only by explicit command on a separate QA/dev DB and never on live data. A future improvement should enforce the QA/dev target restriction in runtime; it is deliberately not implemented in this docs/test-only PR.

Reproduce the read-only inventory with Node.js:

```powershell
node scripts/check-repository-secrets.mjs --history --output .artifacts/secrets-inventory.json
```

REVIEW findings require inspection; a match is not proof a value is live. Review any new candidate without copying the value into logs, issues or PRs. The committed [sanitized review summary](evidence/backend-closeout/security-review.json) identifies reviewed findings and scan counts. Binary assets are not parsed by this pattern tool; it is evidence from the named scope, not a mathematical guarantee about every possible encoding.
