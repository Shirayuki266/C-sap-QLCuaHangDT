## verdict

1. **Resolved — period readability.** Reopened the same UI12-reports and UI11-orders tab0 captures at 125-real and 150-native. Both from/to controls now show complete dd/MM/yyyy dates, including the final year digit, with checkbox and dropdown chrome separate from the text. The period toolbars and actions remain within the 1280×800 form. AdminTheme now fits dates from rendered font width plus native chrome and responds to FontChanged; DpiCloseoutChecks now checks date text plus chrome. The recaptured results manifest records no findings.
2. **Resolved — login eyebrow.** Reopened both UI01-login tab0 captures. The duplicate uppercase eyebrow is absent; the primary heading, top-bar identity, fields and action labels remain legible at both DPIs.

Also reopened UI02-dashboard tab0 at both DPIs for date-fit reflow: dates, threshold, reload action, metric rows and chart remain visible without a regression caused by the fix. Reopened invoice tab0 at both DPIs: these files now show a populated confirmed-order invoice with readable content, server total, status and print/reload controls. They do not show the print-preview dialog. All ten reopened PNGs are valid native form captures with the expected form/state and dimensions.

Scope: this is a verdict pass scoring the two original material fixes and their immediate visual regressions, not a fresh whole-surface audit. Both fixes are resolved. No fix-induced material regression was found in the ten named recaptures. The initial review's evidence inventory and limitations otherwise remain applicable. Actual desktop 125% and simulated native WM_DPICHANGED 150% remain explicitly distinct; fixture captures do not establish real API/SQL behavior. This pass does not certify GATE05, the actual Release walkthrough, SQL H/R/Available/order/COD evidence, print-preview rendering, or whole-phase acceptance.

## remaining

Clear for the two scored material fixes. Populated invoice form evidence is now present at both accepted DPIs; print preview and broader QA remain outside this verdict.

disposition: ship
