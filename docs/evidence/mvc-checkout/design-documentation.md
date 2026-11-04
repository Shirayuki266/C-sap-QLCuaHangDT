# Checkout and read-only receipt: design documentation

Ordinary extension of the existing optionA Monophone system, in Operate mode. Scope is WEB.K12.01–10 checkout and the minimum read-only WEB.K14.01–02 receipt. The order list and cancellation workflow remain step 5. This document records observed implementation and evidence; root `DESIGN.md` and `.impeccable/design.json` are preserved. It introduces no replacement identity, normative token set or system rewrite.

## System inheritance

`PRODUCT.md`, root `DESIGN.md` and `direction.md` establish the navy/blue customer frame, Vietnamese labels, pale canvas, white navigation and panels, system typography and plain textual states. The current views retain these commitments. Shared `storefront.css` supplies ink, muted copy, blue actions, paper/canvas/line/soft colors and the 1200px container. No font, illustration or shipping raster asset is added by these views.

The new panels remain flat: one-pixel borders, gently rounded corners (14px), white fill and spacing establish separation without shadows. Controls have smaller corners (10px), comfortable height (at least 48px) and inherited system type. Shipping labels use semibold weight (650); legends and section titles use a clear heading scale (1.35rem). Product headings are smaller (1.05rem); bold line totals and the inherited flex total row keep money findable. The action inherits blue fill, white type, hover fill and keyboard focus from the shared button. COD uses the existing pale soft fill with a labeled native radio.

The mobile capture visibly retains the Monophone header/search/navigation/footer, navy headings, full-width controls, soft COD inset and blue priced action. Its textarea has a visible native focus border. Shared amber `:focus-visible` selectors cover links, buttons and inputs; the stylesheet does not apply that custom selector to textarea, so the native textarea treatment should not be described as the amber system ring.

## Layout

Checkout places a heading and back-to-cart link above the form. Desktop uses shipping on the left and summary on the right (1.3fr / minimum 280px, 1fr; 24px gap). Recipient and phone share a row; multiline address spans both columns. The explanatory copy and priced submit occupy a following full grid row with a readable width cap (72ch). This placement is a deliberate adaptation of UI06 to retain shipping → summary → submit reading order on mobile.

At 800px and below, checkout and receipt become single-column grids. At 480px and below, recipient/phone fields stack, panel padding changes from 24px to 20px and the inherited heading/back link stacks. The inspected 390px checkout shows complete labels, wrapped address help, a summary below shipping and the priced submit below summary without clipping.

Receipt begins with a wide code/status/timestamp panel. Desktop pairs recipient details on the left with product snapshot money on the right (1fr / 1.3fr; 24px gap); the ordered timeline spans the row below. The inspected 1440px receipt shows a soft success notice above its heading, then these white bordered panels. Addresses preserve line breaks and wrap anywhere; order code also wraps anywhere. Receipt sections stack at the same 800px breakpoint.

## Components and states

| Surface/state | Observed presentation and implementation | Evidence boundary |
| --- | --- | --- |
| Shipping | Fieldset and legend group required recipient, telephone and multiline address. Visible labels, shipping autocomplete, address help, lengths and field-validation spans accompany the controls. | Current Checkout view and mobile capture. |
| Review summary | Product/variant and quantity precede unit price, available quantity, line total and total money. COD is repeated as text. | Current view; mobile capture shows two units at 12.000.000 đ and 24.000.000 đ total. |
| Empty/unavailable cart | Empty summary links back to the cart; line issues explain shortage or removed product in text. Submit is disabled for no lines or cart issues; inherited opacity and local not-allowed cursor communicate disabled state. | Source-observed branches, not individually pictured in the two directly inspected captures. |
| Price changed | Heading becomes “Kiểm tra giá mới”; feedback explicitly states no order or reservation exists and asks for review. The action says “Xác nhận giá mới và đặt hàng COD” and repeats the new total. | Current view plus finish review's account of current desktop/mobile reprice captures. |
| Validation | Summary has alert semantics and a programmatic focus target; checkout JS focuses an error summary on load. Invalid fields receive a red border and adjacent text; shared styles provide red error text and pale error fill. | Source-observed behavior; finish review reports visible current error focus. |
| Submission and return | For a form already marked submitting by the shared flow, checkout JS changes button text to “Đang đặt hàng…”. `pageshow` restores the stored idle label. | Source describes presentation, not an independent duplicate-order guarantee. |
| Receipt | Order code, textual status/payment state, UTC+7 timestamp, recipient details, variant/SKU, quantity, snapshot prices and total appear before an ordered textual timeline. “Tiếp tục xem máy” and a stable receipt link provide navigation. | Current Details view and desktop receipt capture; receipt shows 26.000.000 đ after the supplied reprice flow. |

**Keep the review sequence.** Preserve the mobile shipping → summary → priced submit order and explicit price-reconfirmation wording. Keep meaningful states in text, including status/payment labels and recovery instructions. These are surface rules grounded in the current direction and implementation, rather than new global tokens.

## Evidence and disposition

Directly opened for this documentation: `browser/checkout-mobile.png` (390px) and `browser/receipt-desktop.png` (1440px). Sources inspected: `Views/Cart/Checkout.cshtml`, `Views/Orders/Details.cshtml`, `wwwroot/css/checkout.css`, `wwwroot/js/checkout.js`, with shared `storefront.css`, `cart.css` and `accounts.css` checked for inherited styling.

`finish-review.md` has disposition **ship** for the reviewed UI scope, with no material fixes. It records inspection of all seven current captures and UI06, reports matching inherited type/material/ground and explains the field semantics, richer summary and omitted artificial step strip as ordinary adaptations. `browser/results.json` reports viewport widths equal rendered widths and zero unnamed controls for the current desktop/mobile checkout, reprice, receipt and resumed-checkout records. Those supplied records support the reviewed states; this document does not independently re-run the browser or certify backend correctness, full MVC acceptance, or order lifecycle scope beyond the minimum receipt.

The supplied browser record distinguishes no-store Back behavior (`ERR_CACHE_MISS`, without a restored form) from explicit replay of a captured request. Preserve that qualification when describing replay evidence; screenshots alone cannot establish idempotency or order counts.

## Reported context findings

| Finding | Severity | Meaning and disposition |
| --- | --- | --- |
| `design-sidecar-stale` | mention | Parent context reports root DESIGN.md was edited after sidecar generation; panel tokens/snippets may contradict it. Preserve both files; no refresh or repair is part of this ordinary extension. |
| `config-build-path-unset` | mention | Parent context reports no recorded buildPath. This relates to Persuade/Experience defaults; the present Operate extension is unaffected. Preserve configuration; no repair is performed. |

These findings came from the supplied context, not a fresh doctor run. Root design artifacts remain the incumbent authority with the reported freshness limitation; this scoped record documents the extension without silently broadening them.
