# MVC acceptance design extension

This records the observed ordinary Operate extension of the accepted Option A storefront. PRODUCT.md, the foundation DESIGN.md, .impeccable/design.json and configuration remain preserved. This supplemental artifact does not replace their token authority or propose a new visual world. The direction is taken from direction.md and the existing implementation; no new identity interview or comp is required for this scoped extension.

## Overview

The customer frame continues Monophone's navy welcome panel, blue actions, white navigation and pale canvas. Vietnamese system typography, fine borders, rounded controls and plain status text carry the shopping task across home, catalog, product selection, cart, checkout and orders. The accepted foundation documentation describes its original PR1 boundary; this document records the later implemented surfaces without silently rewriting that history.

No new shipping raster is introduced. The phone illustrations marked “ẢNH QA / Fixture kiểm thử” in captures are QA SVG evidence fixtures, not product photography or marketing assets to ship. Private report/prototype material remains visual authority only.

## Colors

Existing ink, muted, blue, navy, paper, canvas, line and soft tokens continue their documented roles. Loading uses the soft surface with navy text. The shared focus treatment retains the established amber. Error and availability messages retain explicit words; color does not carry the message alone.

## Typography

The incumbent system font stack remains in place. Strong headings separate each task; price and total figures have clear emphasis; secondary product, stock, COD and delivery explanations remain readable beneath the primary label. No display font or webfont asset was added.

## Layout

Catalog cards use three columns on desktop, two in the inspected 768px capture and one at 390px. Desktop filtering sits beside results; narrower layouts use a collapsible filter panel. Product details pair gallery and buying information on wide screens and stack them on mobile. Cart lines and summary, checkout shipping information and order summary, and order records retain a clear reading order on narrow screens. The inspected mobile checkout places its summary below shipping information.

The shared white header, scrollable navigation row, pale main area and footer remain recognizable across these surfaces. The empty-home capture omits bestsellers while preserving the welcome panel, informational content, navigation and contact recovery.

## Elevation & Depth

The extension remains flat: white panels, pale fills, fine borders and spacing provide separation. Loading feedback is a visible inline panel below navigation, rather than an overlay obscuring the task.

## Shapes

Existing gently rounded panels and controls continue. Gallery thumbnails remain compact square buttons with readable fallback text inside their existing bounds. Existing line icons and the main-image placeholder retain their established style.

## Components

### Shared loading and focus

_Layout.cshtml supplies a hidden polite status region reading “Đang tải trang…” and a focusable main landmark. storefront.js reveals the status and marks the main region busy for eligible same-origin navigation and valid form submission. Its pageshow handler clears loading and restores controls disabled by its submit guard. This is interface feedback; service transactions remain authoritative for repeated requests.

ui-feedback.css styles the loading panel with existing colors and adds a visible amber outline to select and textarea keyboard focus (3px, offset 4px), matching the incumbent focus vocabulary. The supplied browser results record visible 3px focus on the six core screens; that is a bounded browser assertion, not full keyboard or assistive-technology coverage.

### Cart feedback

cart.js reports checking and merging in the existing polite cart feedback region and marks the cart page busy during the request. Completion removes that busy state and clears unchanged loading text; recoverable failures provide text and a retry action where implemented. Guest-cart persistence and server quotes retain their existing responsibilities. The source does not make client display totals or image state an inventory authority.

### Gallery recovery

Details.cshtml keeps accessible names on thumbnail buttons, empty alternative text on their decorative images, and a hidden “Ảnh lỗi” label. catalog.js handles both cached failures and later error events, hides the failed thumbnail image and reveals that label. Selecting a healthy secondary image can recover the main display; selecting a failed source retains the explicit main fallback. ui-feedback.css ensures hidden thumbnail content stays hidden.

The inspected gallery-recovered capture shows the healthy second image displayed with the first thumbnail labeled “Ảnh lỗi”. The inspected gallery-late-error capture shows the same labeled treatment for a later thumbnail failure. These directly support the resolved original broken-thumbnail finding.

### Empty, error and busy states

The evidence inventory contains catalog, cart and orders empty states, invalid-filter and profile validation, standalone error recovery, sold-out selection, loading states, account submit feedback and order-flow recovery. The actual inspected cart loading image shows the shared Vietnamese loading panel while cart content remains readable. The actual inspected checkout image retains labeled shipping fields, COD explanation, summary and final action in a single mobile column.

Busy screenshots use the capture instrumentation described in browser/results.json; separate real duplicate submissions are reported there. A captured disabled action alone does not prove transaction concurrency behavior.

## Do's and Don'ts

- Do continue the accepted navy/blue, white/pale system and Vietnamese textual recovery.
- Do retain named gallery controls, visible keyboard focus, loading text and meaningful empty/error next actions.
- Do keep price, inventory and order transitions authoritative in existing services.
- Don't promote QA fixtures, screenshots or private reference images into shipping assets.
- Don't treat browser snapshots or these limited DOM assertions as formal WCAG conformance, a screen-reader audit, independent SQL verification or completion of every acceptance criterion.

## Evidence and review boundary

Read-only source inspection covered Views/Shared/_Layout.cshtml, Views/Catalog/Details.cshtml, wwwroot/css/ui-feedback.css, wwwroot/js/storefront.js, cart.js and catalog.js under src/PhoneStore.Web; catalog and checkout styles were also consulted. Representative actual images opened with view_image were UI13-desktop.png, UI13-empty-mobile.png, UI03-tablet.png, UI06-mobile.png, UI06-empty-mobile.png, UI05-loading-mobile.png, UI04-gallery-recovered-mobile.png and UI04-gallery-late-error-mobile.png in browser/. Other screen/state coverage above is attributed to the supplied inventory and browser/results.json, rather than claimed as an independent image-by-image inspection.

finish-review.md preserves the original gallery finding. finish-verdict.md records disposition: ship for that original gallery fix list only, following refreshed healthy/sold-out captures and recovery/late-error evidence. It does not grant whole-surface acceptance. The parent reports a successful final rerun with 51 screens and 32 SQL checkpoints. These are attributed test results; this documentation did not execute the browser suite, SQL checks or an accessibility audit.

UI06-empty-mobile.png was subsequently opened and shows the accepted empty-checkout redirect to the empty cart, the explanatory notice “Giỏ hàng đang rỗng. Vui lòng chọn sản phẩm trước khi đặt hàng.” and the “Xem điện thoại” recovery action. It is evidence of that redirect/recovery destination, rather than an empty checkout form. The final reviewer also closed its supplemental capture-validity item without changing gallery-only ship scope.

Preexisting context drift remains out of scope: design-sidecar-stale reports that the sidecar may lag DESIGN.md, and config-build-path-unset reports no recorded buildPath. Neither was repaired as a side effect of this extension.
