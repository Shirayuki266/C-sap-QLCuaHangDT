## verdict

- Resolved — loading readability. The refreshed UI01-login-busy.png visibly shows bold dark “Đang đăng nhập…” on white above the disabled action. The current operation no longer depends on the disabled button's dark-on-blue text. LoginForm.SetBusy uses the same enabled ProgressLabel for readiness checking. The normal, error and offline recaptures retain complete, readable forms without overflow introduced by the fix.
- Evidence validity: reopened all eight fake-form captures and both real-QA captures at their original paths. All show the named surface, valid dimensions and complete content. No regression attributable to this fix is visible in those captures. Original finish-review.md is preserved.
- Historical, superseded sizing recapture: the earlier Shown correction produced 1600×1000 login/main captures and enlarged auxiliary captures; those files were valid when reviewed. That correction has since been removed and this paragraph does not describe the current implementation or current fake-form captures.
- Current local sizing evidence: reopened regenerated UI01-login-1280.png, UI01-login-busy.png and UI02-main-1280.png. Each is again 1280×800 pixels, shows its named surface completely and has no blank or clipped content. The enabled bold busy status remains readable, so the sole scored finding remains resolved. Current forms/results.json records actual 1280×800 main/login bounds and minimums at 120 DPI, and eight passing STA checks. The parent's CI change now configures a 1920×1080 runner desktop; pending Windows CI results are not certified by this local visual verdict. The older real-QA captures were not regenerated or re-certified in this latest limited pass.

## remaining

Clear for the sole scored fix. This verdict covers the loading-readability correction and its visible regressions; it is not a new whole-surface review or independent runtime, SQL or WCAG audit.

disposition: ship
