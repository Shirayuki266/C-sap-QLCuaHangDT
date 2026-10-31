# MVC cart design documentation

Disposition: ordinary extension of the incumbent optionA customer frame. The finish review is **ship** for the step4a cart UI only. This record documents the shipped surface; it does not replace or regenerate `DESIGN.md` or `.impeccable/design.json`.

## Observed inheritance

`PRODUCT.md`, `DESIGN.md` and this surface's `direction.md` establish the existing Monophone world: white navigation and footer, pale canvas, navy/ink headings, blue actions, Vietnamese system typography, visible amber keyboard focus and restrained rounded controls. The cart retains these choices. Its CSS consumes the incumbent `--line`, `--paper`, `--navy`, `--muted` and `--soft` properties and the shared button styles. It introduces no new palette, font, identity, shadow or decorative motion.

Cart cards and the summary use white surfaces with thin line-colored borders, rounded corners (14px) and padding (24px; 20px below 480px). Quantity controls retain the shared control corner language (10px) and use a minimum height (44px). These are observed cart-level applications of the incumbent system, not newly declared global tokens.

## Composition and responsive behavior

The heading and continue-shopping link precede editable cart lines. Desktop uses a two-column grid with lines at left and the money summary at right (1.45fr / 1fr, a 280px minimum summary column, and a 24px gap). The layout becomes one column at 800px. Below 480px the heading/link pair also stacks. The cart inherits the existing shared container, responsive header and footer.

Each text-first card presents the phone name, unit price, storage/color and availability before labeled quantity/update and remove controls, followed by a separated line total. The summary groups COD context, total and the server-price/stock-reservation explanation before its action. No shipping raster imagery is introduced by the reviewed cart template or script; the browser captures are review evidence, not application assets.

## States and evidence

Directly inspected source: `src/PhoneStore.Web/Views/Cart/Index.cshtml`, `wwwroot/css/cart.css`, `wwwroot/js/cart.js` and the incumbent `wwwroot/css/storefront.css` under that project.

Directly opened rendered evidence:

- `browser/guest-cart-desktop.png` (1440px): inherited white header and pale canvas; heading/link pair; one editable line at left; summary and blue sign-in action at right. Unit price, quantity, line total and total are readable and separated without shadows.
- `browser/merged-cart-mobile.png` (390px): shared mobile frame; login notice; merge adjustment feedback above two stacked cards; summary beneath them. Feedback explicitly shows quantity reduction and removal, the badge shows the merged count, and the disabled “Đặt hàng — sắp có” action explains the deferred ordering boundary. No clipped controls or horizontal overflow is visible in this capture.

Source also defines an empty-cart catalog action, guest loading and no-JavaScript guidance, validation/line issues, update feedback, failed quote/merge recovery with “Thử lại,” and refreshed badge/summary content after successful operations. Labels are associated with quantity fields; remove controls name their product; feedback uses a polite live status region and validation uses an alert. These source observations do not establish runtime coverage of every state.

`finish-review.md` supplies the broader review of six browser captures, including empty and failed-merge states, and reports no material UI fixes. Its supplied browser results cover reload/update/merge retry/badge scenarios. This document independently inspected only the two captures named above; static screenshots do not prove keyboard traversal, hover behavior, transient loading timing or server correctness.

## Existing drift and scope limits

The incumbent design documents remain foundation-era records: their overview still describes PR1 and excludes implemented account/catalog/cart screens; the sidecar's search description still labels catalog behavior as later work. The finish review also records pre-existing sidecar drift. These are reported as historical documentation drift and left unchanged. Cart-specific breakpoints and card applications are recorded here without silently broadening the normative foundation tokens or refreshing the sidecar.

Scope is MVC step4a cart K10–K11, the product-detail POST handoff and post-login guest merge feedback. Checkout/order UI remains future work. The guest sign-in action is an account handoff; the signed-in ordering action is visibly deferred. This artifact and the ship disposition provide no backend, SQL integrity, stock authority, checkout, complete accessibility or wider MVC checklist certification.
