# Admin W6 implemented design record

Recorded 2026-11-02. Scope: the native WinForms report/dashboard extension and the associated Admin finish. This is an ordinary Operate extension of incumbent OptionA/AdminTheme, not a replacement visual world. This document is descriptive evidence; it creates no new tokens, branding, product claims or acceptance gates.

## Overview

The implemented task sequence is: choose a report and UTC+7 period, load one server report, inspect its summary and a shared table/chart projection, then select a plotted value or page through rows. The dashboard exposes a period and low-stock threshold, independently loaded metrics, their timestamps, a seven-day revenue chart and drilldowns. Precision, familiar native controls and explicit recovery carry the design.

Inherited authority was checked against root `PRODUCT.md`, `DESIGN.md`, `.impeccable/design.json`, `.artifacts/admin-w6/direction.md` and `user-acceptance.md`. The root documents and sidecar describe the historical MVC customer foundation. Their OptionA navy/blue/white palette, system typography, Vietnamese copy and visible status conventions explain the family resemblance; their web breakpoints, rounded CSS components and customer-flow scope are not WinForms specifications. All three root files are preserved. Their web/PR1 scope and earlier WinForms exclusion are pre-existing historical scope drift relative to this separately authorized Admin work, not authorization to rewrite them.

The implementation evidence read was `src/PhoneStore.Admin/AdminTheme.cs`, `ReportScreen.cs`, `DashboardView.cs`, `MainForm.cs`, `ProductEditor.cs`, `LoginForm.cs`, `CatalogUi.cs` and the Admin project file. Capture methods were checked in `tests/PhoneStore.Admin.Tests/DpiCloseoutChecks.cs`, with the forms and DPI manifests and the independent finish review/verdict. This documentation pass directly opened the recaptured dashboard, report and login 150-native PNGs; the independent reviewer owns the full 65-image review inventory.

## Colors

The existing `AdminTheme` fields remain the code source of truth. No separate Admin palette is introduced here.

| Existing field | Observed value | Implemented use |
| --- | --- | --- |
| Ink | RGB 25, 38, 61 | Headings, body copy and secondary-button text |
| Navy | RGB 20, 45, 92 | Main navigation backing and selected chart bar |
| Blue | RGB 36, 84, 220 | Primary actions and chart bars |
| Canvas | RGB 244, 246, 251 | Form background |
| Muted | RGB 82, 100, 127 | Secondary connection information |

White surfaces hold charts, inputs, login content and secondary actions. Main navigation selection uses the existing pale blue fill (RGB 234, 240, 255). Native grid selection and focus treatments retain operating-system behavior. Loading, failures, invalid periods and empty results have visible text; color alone does not carry them.

## Typography

`AdminTheme.Frame` establishes Segoe UI at 10 pt with DPI autoscaling. Shared labels use Segoe UI at 10 pt by default, with bold section/form headings at 22–24 pt, the main overview title at 23 pt, the sidebar brand at 17 pt and text inputs at 12 pt. The chart paints Segoe UI labels at 9 pt and its title at 10 pt bold. These are observed point sizes at the base form configuration, not CSS or a new type scale. Vietnamese labels and vi-VN money formatting remain visible; reports identify UTC+7 and currency in đồng, while chart revenue axes use millions with `tr` and full selected values remain in text.

## Layout

Shared frames default to and have a minimum of 1280×800. MainForm uses a 230-unit navy sidebar, a flexible body and a 36-unit status row, with account actions above the dashboard. Navigation opens normal native management dialogs; the overview remains available and reloads its metrics. The footer identifies environment/fingerprint, account and ADMIN role.

Reports use a padded vertical layout: heading, wrapping native filter toolbar, period error, server summary, chart, grid, status and pager. The flexible chart/grid allocations are 48%/52% of their shared remaining region. The dashboard places its wrapping filters over six two-column metric rows (38% action / 62% value), then the seven-day chart and timestamp/disclaimer text. This is a restrained native metric list rather than a new card system.

`CatalogUi.Toolbar` wraps controls. Scrollable stacks, DataGridView horizontal/vertical scrollbars, native tab pages and the sidebar's vertical scrolling are intentional access boundaries. Wide variant columns have minimum widths and use horizontal scrolling; long tables use vertical scrolling. Unselected tabs do not need to fit simultaneously. ProductEditor preserves its three native tabs, general-information columns and persistent save/reload/purchase actions.

Date inputs use `dd/MM/yyyy`. Shared frame setup measures a full sample date using the control's rendered font and allocates native checkbox/dropdown space. It recomputes width on `FontChanged`, permitting the toolbar to reflow after DPI changes. This is the actual fix for the independently observed clipped final year digit.

## Elevation & Depth

Depth is provided by navy, pale and white surfaces, flat control borders, native grid lines and spacing. The Admin implementation adds no custom shadows, background imagery or animation system. Native focus, selection, disabled states and dialog chrome supply interaction feedback.

## Shapes

The implementation keeps standard rectangular WinForms controls, flat rectangular buttons and native tab/table/form chrome. Shared buttons have a base minimum of 120×42 with 12/5-unit padding; navigation and metric buttons have their own observed compact sizes. Web corner radii and pill-search geometry in the historical root DESIGN are not promoted into the desktop interface.

## Components

- **Report filters and results:** revenue, bestsellers by variant and current stock share one report surface. Dates and monthly grouping enable only for applicable report types. A period error appears before a request. Loading disables conflicting filters/actions. A successful response supplies the authoritative server summary; monthly grouping projects that already loaded revenue result locally. Table and chart page together in groups of 20. Grid selection or a clicked chart bar selects a point; Left/Right keys on the focusable chart expose its complete label/value below the plot. Axis labels may be sparsified or shortened to fit; the selected text is the full-value path.
- **Report empty/error behavior:** an empty response explicitly says there is no data and displays zero server totals; the chart supplies empty text. Read failure clears current projection/rows, replaces the chart with an unavailable state and says there are no confirmed figures. The load action remains the recovery path after busy state ends.
- **Dashboard:** waiting, shipping, unreceived failed deliveries, period revenue, today's revenue and low-stock count load independently. Each successful metric shows its own load time and enables its drilldown; a failed metric displays its own error and stays unavailable while other successes remain usable. The seven-day chart also has its own load/error text. The visible disclaimer states these calls are separate and do not form an atomic snapshot. Changing dates invalidates period revenue; changing the threshold invalidates low-stock data until reload. Order/stock actions open filtered native lists; revenue actions open the appropriate report period.
- **Keyboard and labels:** native Tab order is assigned by visual top/left order on shown forms while grid, up/down and combo internals retain native behavior. Shared inputs and report/dashboard selectors have accessible names. Mnemonics are retained where explicitly present. Login uses Enter for submit and Escape for close; report Enter invokes load. Shared modal forms without a CancelButton close on Escape. Save forms discover their save button as AcceptButton and support Ctrl+S when that button is enabled. These are source-observed behaviors, not a claim that the final live keyboard walkthrough is complete.
- **Login and editing:** login focuses username, checks readiness, shows connection/progress/error text, prevents duplicate busy actions and clears the password on close. Its duplicate uppercase eyebrow has been removed; identity remains in the top bar and form title. ProductEditor preserves explicit field/table validation, entered values on validation failure, server price/stock/version reloads, confirmation before replacing unsaved data or changing trading status, and a save-first purchase handoff. Failures use known API messages or the shared plain recovery message.
- **Chart renderer:** shipping `ReportChart` is a native GDI+ custom control, not a generated raster. The preferred ScottPlot.WinForms 5.1.59 restore failed under the project's warning-as-error policy with NU1701 for OpenTK/OpenTK.GLControl and SkiaSharp.Views.WindowsForms. The 5.0.56 retry also failed NU1701 for the OpenTK dependencies. The exact local logs are `.artifacts/admin-w6/scottplot-5.1.59-restore.log` and `scottplot-5.0.56-restore.log`. The current Admin project has no ScottPlot PackageReference. This records the scoped fallback and its evidence, not a claim that ScottPlot is generally unusable.

## Do's and Don'ts

- Retain the incumbent Segoe UI/native-control, navy/blue/white system and server-authoritative summary/result relationship.
- Keep the selected period, threshold, individual load times and plain error/empty/recovery messages visible.
- Preserve native keyboard operation, named inputs, toolbar reflow and intentional table/tab scrolling.
- Keep historical MVC design artifacts intact; record drift without repairing it as a side effect.
- Do not interpret fixture capture values as real business data, or visual review as SQL/whole-phase acceptance.
- Do not turn this ordinary extension into a new identity, token system or generated-image exercise.

Evidence and limits:

`dpi/results.json` records **56 native form/tab captures**, 1280×800, with no findings. `dpi-closeout.log` records PASS for native DPI/layout/labels. The current automated checks cover visible control bounds, label/button text fit, named inputs and DateTimePicker text plus checkbox/dropdown chrome. Actual desktop DeviceDpi is 120 (125%). The 150% case sends WinForms `WM_DPICHANGED` with 144 DPI, confirms the event and records font change (10→12 pt) before constraining the native form to 1280×800. The desktop itself remains at 125%; this is a native DPI-event simulation, with no bitmap resizing. Bounds checks inspect the rendered, unscrolled control tree; table cell contents remain governed by native scrolling rather than an all-columns-at-once promise.

`forms/results.json` identifies **nine** 1280×800 report/dashboard state captures: revenue, monthly, date error, stock, bestsellers, empty, read error, complete dashboard and partial dashboard failure. Its `fakeApi: true` / `qaOnly: true`, and the DPI manifest's `fakeApi: true`, are material provenance. All 65 PNGs are QA evidence generated from native controls with fixture APIs, not shipping product imagery or generated assets. No raster asset is introduced by this document or these UI changes.

`finish-review.md` is the initial independent review and preserves the original date-clipping and duplicate-eyebrow findings. The subsequent `finish-verdict.md` scores both fixes **resolved**, checks immediate dashboard date reflow, and records disposition **ship at that verdict's scope**. It also recognizes populated confirmed-order invoice captures at both DPIs; these do not show print preview. The verdict is not a new full-surface audit and does not establish real API/SQL behavior.

At this documentation handoff, the actual built Release application's QA walkthrough, stepwise FLOW01/02 screenshots and SQL H/R/Available/order/COD before/after evidence, print-preview verification and whole-phase acceptance remain pending. Capture fixtures and visual checks do not close those gates. The accepted handoff also retains a later user manual run in addition to the agent-assisted Release walkthrough. No gate status is changed by this record.

## Integration addendum

The paragraph above records the documenter's handoff time and scope. The parent subsequently completed the actual Release walkthrough:19 acknowledged SQL checkpoints and21 pictures, separately reviewed in release-evidence-review.md. The final ledger and CI verification are owned by WINFORMS_W6.md/WINFORMS_HANDOVER.md, not this design-only record. W5 installed/no-printer preview proofs remain applicable; no new physical printer output or OS print-preview DPI certification is claimed.
