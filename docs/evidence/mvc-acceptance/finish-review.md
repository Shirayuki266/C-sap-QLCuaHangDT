disposition: fix

Inputs and scope: ordinary Operate extension of Option A; no new identity or approved comp, quality-bar card, measured comp spec, or five-block concept contract was supplied. Comp reproduction and concept-roll gates do not apply to this extension. Reviewed PRODUCT.md, DESIGN.md, direction.md, craft-floor.md, detector.json, browser/results.json, all 47 actual PNGs via view_image, and the MVC gallery Razor/JavaScript. No browser execution, SQL verification, comprehensive source audit, screen-reader test, or backend/schema/admin API review was performed by this reviewer. Shipping files were read-only; this report is the only write.

## persistence

Pass within this scope. PRODUCT.md and DESIGN.md exist; the latter explicitly documents the accepted foundation rather than claiming all shopping flows were already complete. The current captures preserve its visual world. All 47 named captures exist, show their claimed sections/states from the document top, and have sensible viewport dimensions; required UI03–UI07/UI13 desktop 1440 and mobile 390 plus UI03 tablet 768 are present. No blank or wrong-section capture requires recapture. detector.json is an empty array; no second detector pass was run.

## fidelity

| Element | Result | Evidence |
| --- | --- | --- |
| TYPE | match | Existing heavy system headings, readable Vietnamese body type, and restrained hierarchy continue DESIGN.md's expressly accepted Option A system stack. |
| MATERIAL | match | Flat white/light panels, navy welcome surface, fine borders, and consistent line icons retain the established web material. QA image fixtures visibly identify themselves as test content; they are evidence assets, not a proposed shipping identity or marketing illustration. |
| GROUND | match | Cool pale canvas, white frame, and navy/blue actions visibly retain the documented palette; there is no approved comp against which to assert a pixel reproduction score. |
| Customer frame and home | match | UI13 preserves the welcome composition, navigation, secondary information, product section, and footer; narrow captures stack coherently. |
| Catalog/filter composition | adaptation | Three desktop product columns become two at 768 and one at 390; filters collapse on narrower screens. This implements direction.md's responsive-grid requirement and PRODUCT.md's customer filter task. |
| Product selection | match | Price, stock text, storage/color choices, quantity, COD explanation, and information hierarchy remain readable on both widths; sold-out action has explicit disabled text. |
| Gallery image failure | contradicted | UI04-sold-out-mobile.png shows a native broken-image glyph in the first small thumbnail immediately below an intact main-image fallback. Details.cshtml's thumbnail img lacks the existing data-product-image fallback path, and catalog.js handles only main/card product images. |
| Cart and checkout | match | UI05/UI06 retain clearly named controls, totals, shipping labels, and recovery links; mobile order summary follows shipping information as required by PRODUCT.md. |
| Orders | adaptation | UI07's desktop table becomes labeled mobile records, preserving order number, recipient, date, total, textual order/payment states, and details action; this supports the narrow-screen operating task. |
| Loading/busy feedback | match | Six loading captures show visible Vietnamese status text; register/login/checkout/cancel show a disabled processing action. results.json documents browser-only busy capture instrumentation separately from real duplicate submissions counted once. This is visual evidence, not an independent concurrency audit. |
| Empty/error/recovery | match | Catalog/cart/orders empty states offer a next action; invalid filters preserve entries; profile errors expose the field message and focus; price reconfirmation preserves shipping values and names the next action. Standalone 404/500 pages intentionally follow DESIGN.md and retain home recovery. |
| Accessibility evidence | match within supplied checks | results.json records zero unnamed controls and images missing alt, no horizontal document overflow, and visible 3px keyboard outline on the six core screens. Profile and busy images visibly show amber focus. These checks do not establish full keyboard-path or assistive-technology coverage. |
| Flow truth | match within supplied evidence | FLOW01–05 visibly cover merge/order/cancel, confirmed/dispatched/completed textual states, new-price reconfirmation, invalid-product blocking, and login recovery. SQL/service authority is reported by the parent; screenshots cannot independently prove the 31 SQL checkpoints. |

## ceiling

Reached for the ordinary Option A Operate extension apart from the gallery failure below. Unused expressive typography, added depth, marketing imagery, and ornament are not missing commitments in this task. Static captures cannot establish motion quality. The supplied system type is established authority rather than an invitation to invent a new display face.

## material_fixes

1. Floor states / direction.md error recovery: fix thumbnail image-load failure in MVC Views/Catalog/Details.cshtml and wwwroot/js/catalog.js (with scoped gallery CSS only if needed), so UI04-sold-out-mobile has no native broken-image glyph. A compact labeled fallback such as “Ảnh lỗi” inside the existing named thumbnail button is acceptable; retain keyboard access and ensure choosing it still produces the main fallback. Handle both already-failed cached images and later error events. Recapture the same UI04-sold-out-mobile.png plus healthy UI04 desktop/mobile after the fix; verify a healthy secondary image can still replace the fallback. No backend, schema, service, or admin API edits are needed.

## keep

Keep Option A's navy/blue actions, white/pale customer frame, Vietnamese labels and textual status/recovery, mobile stacking, visible focus, server-authoritative commerce behavior, and explicit QA-only evidence boundary.
