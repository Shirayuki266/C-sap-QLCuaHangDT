# MVC orders: observed design extension

## Overview

This documents the implemented customer order list, receipt timeline and pending cancellation as an ordinary Operate extension of optionA/UI07. The direction is to make an owned order easy to find, inspect and deliberately cancel, with recoverable validation. The supplied brief and incumbent implementation provide direction; no new identity workshop or simulated user choice was substituted.

Authority and inspected evidence: `PRODUCT.md`, root `DESIGN.md`, `.artifacts/mvc-orders/direction.md`, the Impeccable document reference, and `finish-review.md`; `src/PhoneStore.Web/Views/Orders/Index.cshtml`, `Details.cshtml`, `wwwroot/css/orders.css` and `wwwroot/js/orders.js`; and supplied `browser/results.json`. Actual images opened for this documentation were `browser/orders-desktop.png` (1440px), `browser/cancel-pending-mobile.png` (390px) and `browser/order-long-reason-mobile.png` (390px, full-page). The supplied browser records and finish review support other state observations; this documentation did not run another browser or inspect every capture independently.

This artifact supplements the existing foundation documentation. Root `DESIGN.md`, `.impeccable/design.json` and configuration are preserved. The inherited token definitions remain authoritative; surface observations below do not create a second normative token set. No new raster asset ships: the browser images are QA evidence.

## Colors

The opened list retains the white Monophone navigation and footer, cool pale canvas, navy headings, blue links and filled primary action. The table header uses the inherited soft fill; fine line-colored separators distinguish rows. White receipt and cancellation panels use the same quiet border treatment. States remain written Vietnamese text, with a navy bold order state and smaller muted payment text; color is supplementary.

The focused reason field in the opened pending mobile capture has the established amber outline. There is no new cancellation color palette or decorative status-chip system.

## Typography

Vietnamese system typography and bold navy hierarchy continue from the customer frame. Table headings are compact (.9rem); order state and form labels use weight 650, while payment metadata uses .85rem. The empty-state and cancellation headings use 1.35rem. Ordinary links identify details, list recovery and filter reset without an added icon dependency.

Long order codes can break within table cells. The timeline now also preserves arbitrary reason text with wrapping rather than truncating it.

## Layout

The desktop capture places heading and browsing link above a wrapping filter row, then a six-column table and pagination. Columns expose code, recipient, UTC+7 placement date, amount, written order/payment state and details. The table has a fixed layout, padded cells (18px/16px), a wider first column (23%) and narrow details column (9%).

At widths up to 800px, the same table becomes a sequence of white bordered cards: the header is visually clipped, rows become grids, and cells show their `data-label` text beside values in a 100px/minmax(0,1fr) grid. The final details link occupies a block row. This is responsive presentation of the existing table, not a separate duplicate order list. The supplied Chromium record reports `table-row` at 1440px and `grid` at 390px, with document width equal to viewport in both cases.

Filters wrap with 16px gaps; search has a larger desktop minimum than the other fields. At up to 480px, every filter field takes the full row and loses its minimum-width constraint. The opened 390px receipt stacks status, delivery, ordered items/total, timeline and cancellation in the inherited customer frame. The header/search/account/navigation order and footer follow the incumbent mobile layout.

The final long-reason capture shows the timeline growing vertically while its white panel, page and footer stay within the narrow viewport. The supplied browser record also reports width 390 for this state.

## Elevation & Depth

Flat white panels, pale ground, spacing and fine borders provide separation. The order extension adds no shadows or decorative motion. Native confirmation is browser UI; its visual chrome is not part of these page styles.

## Shapes

Controls retain gently rounded corners (10px), line borders, white fill and a minimum height of 48px. Empty, mobile order and cancellation panels use the shared larger corner (14px). Mobile order cards have 20px padding; cancellation uses 24px, reducing to 20px below 480px. The reason field can resize vertically and has a 3px amber focus outline offset 4px.

## Components

### Filters, empty state and errors

The labeled GET form supports code/name/phone search, written order status, local date bounds and page sizes 5, 10, 20 and 50. A primary “Lọc đơn” action submits; “Xóa bộ lọc” returns to the unfiltered list. Entered filter values are rendered back from the view model. A validation summary has alert semantics and a focus target; JavaScript focuses an existing filter validation error. An unreadable filter result supplies explanatory recovery copy instead of a misleading order table.

An empty result renders a white bordered status panel with a heading, guidance to adjust filters and a link to all owned orders. Pagination includes current page and total count, with previous/next links when available. The supplied finish review records retained invalid-date input and contained invalid-filter/empty mobile states; those two images were not independently opened in this documentation pass.

### Receipt and safe timeline

Receipt content groups delivery information, ordered lines and total, and an ordered list of timestamped textual transitions. Razor outputs reason text through normal encoded expressions. The pending mobile image visibly contains script-like text inside the reason input as literal content; the finish review records the encoded script-like reason in recaptured cancelled states. This documents the rendering boundary, not a general security certification.

The scored finish fix is present as `.receipt-timeline p { overflow-wrap: anywhere; }`. The reason input accepts up to 1000 characters; an unbroken reason previously risked escaping a narrow timeline panel. The opened `order-long-reason-mobile.png` visibly preserves the long unbroken reason across contained lines. The fix retains full content and scopes wrapping to timeline paragraphs.

### Deliberate pending cancellation

Only a pending (`CHO_XAC_NHAN`) receipt renders the native `details` cancellation panel. Its summary opens a required, labeled reason field and a clear primary confirmation action. Server validation reopens the panel, retains the view-model reason and exposes summary/field feedback; JavaScript focuses the error summary. Hidden ID/version and the antiforgery field accompany the POST form.

The confirmation handler calls `window.confirm` before an eligible submission. Dismissal prevents the submit event and stops later submission handling. The page also supplies “Đang hủy đơn…” feedback for a submission already marked as in progress and restores its idle label on `pageshow`.

Supplied Playwright Chromium results report PASS for `native-confirm-dismiss-no-post-accept-double-guard-and-reload`: two native confirmation dialogs and one accepted POST, with dismissal producing no POST according to the finish review. These are supplied behavior observations, not behavior inferred from a still image or newly rerun here. Native dialog appearance was not captured in the page screenshots. The finish review also records that confirmed/cancelled receipts omit cancellation controls and cancelled states show written success/status/transition feedback.

## Do's and Don'ts

- Do retain the inherited customer frame, written Vietnamese statuses, labeled fields, mobile value labels and visible keyboard focus.
- Do retain full encoded timeline reasons, including 1000-character unbroken input, within the receipt panel.
- Do preserve filter/error recovery, pending-only cancellation and deliberate native confirmation.
- Don't use the QA screenshots as shipping imagery or publish private visual-reference inputs.
- Don't interpret supplied UI behavior records as backend, ownership, inventory, concurrency or complete MVC acceptance certification.

The finish review initially returned `fix` for timeline overflow. Its subsequent fix-only verdict is `ship`, with the scoped wrapping rule and recaptured ordinary cancelled states plus the long-reason mobile evidence resolving that single scored issue. This documentation records that UI disposition; the follow-up verdict is not a new whole-surface audit or backend certification.

Pre-existing `design-sidecar-stale` and `config-build-path-unset` remain mention-only context findings. A later explicitly requested document refresh can synchronize the sidecar while preserving DESIGN.md; a separately requested configuration choice can record the build path. Neither was repaired or expanded into this task.
