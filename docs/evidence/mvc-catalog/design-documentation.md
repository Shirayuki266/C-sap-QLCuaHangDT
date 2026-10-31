# Catalog/home/detail design documentation

Documentation pass: 2026-10-31. This is a local record of the step 3 optionA extension, not a replacement visual world or a revision of the root design system. `PRODUCT.md`, `DESIGN.md`, the inherited foundation surface brief and `reference/document.md` were read. No design-system refresh is authorized; the foundation-era wording and pre-existing sidecar drift remain unchanged.

## Overview

The final catalog, details and monthly-bestseller section continue the incumbent navy/blue frame: white navigation/footer, pale canvas, blue actions, Vietnamese system typography and flat bordered surfaces. The existing welcome, introduction and configurable contact content remain on the homepage. The extension adds product comparison and selection within that frame without introducing promotional discounts, fabricated sales counts or a new identity.

Source inspected under `src/PhoneStore.Web/`: `Views/Catalog/Index.cshtml`, `_Filters.cshtml`, `Details.cshtml`; `Views/Shared/_ProductCard.cshtml`, `_Layout.cshtml`; `Views/Home/Index.cshtml`; `Models/CatalogViewModels.cs`; `wwwroot/css/catalog.css`, `wwwroot/js/catalog.js`. The shared `storefront.css` and `Controllers/HomeController.cs` were also checked for inherited styling and the bestseller service binding.

## Colors

Catalog styles consume the existing ink, muted, blue, paper, canvas, line and soft CSS variables. Blue plus a visible outline/background marks selected variants; availability is expressed in text. Error summaries retain the existing red text/pale error surface, while keyboard focus uses the incumbent amber outline. These observations describe local use of existing values; they do not add global palette tokens.

## Typography

The inherited system stack supplies headings, labels and body copy. Product headings wrap, specifications preserve newlines, and long names/SKUs can break safely. Prices use bold tabular numerals. Catalog and detail titles share the existing responsive headline scale. Vietnamese field labels, availability, recovery instructions and image placeholders communicate meaning beyond color.

## Layout

The catalog uses a 220px filter sidebar beside the results, changing to 210px at widths up to 1100px. Catalog cards use three columns by default, two at up to 1100px, and one at up to 600px. The generic homepage product grid changes from three to two at up to 899px, then one at up to 600px. Thus the 3/2/1 behavior is shared while intermediate breakpoint behavior differs intentionally by available content width.

At up to 899px the filter becomes a bordered in-flow collapsible panel above the results. It is a native `details` disclosure, not a modal overlay. JavaScript opens it at widths of at least 900px and keeps validation errors open on narrow screens. Without JavaScript it begins open, preserving access to the GET form. Detail gallery and buying information form equal desktop columns and stack at up to 899px; specification rows stack at up to 600px.

## Elevation & Depth

No shadows are added. White cards, pale image areas, fine borders and spacing distinguish components. The navy welcome surface retains its incumbent emphasis.

## Shapes

Cards, main gallery and information panels inherit the shared rounded radius; inputs, variant choices, thumbnails and smaller actions use the existing gently rounded control shape. Product cards reserve a 4:3 image area; detail imagery uses a square area and contained images. Inline stroke SVGs remain consistent with the frame.

## Components

| Component | Final behavior and evidence |
| --- | --- |
| Filter disclosure | One authored SVG chevron shows open/closed state by rotation. `list-style:none` and the WebKit marker rule suppress the native marker. Final CSS has no `summary::after` plus/minus rule. Desktop/error captures show one upward chevron; mobile/tablet captures show one downward chevron. The former duplicate indicator is resolved. |
| GET filters, chips and paging | Labeled query, brand, storage, color, price, sort and page-size fields retain submitted model values. Validation is textual and focused by the enhancement script. Removal chips clear their named condition, reset the page and retain other context; “Xóa bộ lọc” clears facet conditions while retaining query/sort/page size. Previous/next links preserve filters. Invalid input shows correction guidance; zero matches shows a clear all-products recovery link. |
| Product card | Service-fed name, image, display variant and price appear in a white bordered card. The display row is selected from server-provided public variants ordered by price then ID; the client does not calculate a selling price. Catalog stock reflects whether any public variant is available. The image link is removed from keyboard/accessibility duplication; title and named details link provide navigation. Bestseller cards omit brand/variant/stock when their supplied model does not contain them. |
| Variant selection | Real radio groups sit inside labeled fieldsets. A selected choice has a blue outline, pale fill and heavier label; unavailable color combinations are disabled and struck through. JavaScript selects an existing variant for the chosen storage/color and updates price, stock, SKU, storage/color rows and hidden variant ID using the server-embedded values. The polite live summary announces selection changes. This presentation does not grant client authority over inventory or price. |
| No-JS selection | The server-rendered select and “Xem phiên bản” GET action remain available before enhancement. JavaScript exposes the radio controls and hides that fallback only after initialization. The supplied browser results report the no-JS selection check passing. |
| Quantity and future cart form | Quantity is required, integral and bounded by selected availability; selection changes clamp it. Zero stock disables quantity/action and renders “Hết hàng.” The rendered POST form targets future `/Cart/Add` with `productId`, `variantId`, `quantity` and an explicit antiforgery token. This step implements no cart action and executes no cart POST test. |
| Gallery and image recovery | Deduplicated service image URLs supply a main image and named thumbnail buttons. Enhanced thumbnails replace the main image. Missing images show “Chưa có ảnh”; load-error enhancement hides a failed primary image and reveals its placeholder. Browser evidence reports gallery and broken-image recovery passing. Thumbnail interaction depends on JavaScript; no-JS evidence concerns variant selection. |
| Homepage bestsellers | `HomeController` obtains the bestseller model from `ReadBestsellersAsync`; the view renders it only when items exist, labels the returned month and completed COD basis, and reuses the product card. It does not substitute invented products when the set is empty. The inherited introduction and contact sections remain below it. |

## Do's and Don'ts

- Do preserve the navy/blue/white/pale optionA frame, textual states, labeled native controls and visible focus when extending these components.
- Do treat prices and availability as server/service values and revalidate commands in the future cart implementation.
- Do keep missing-image, invalid-filter and no-results recovery explicit.
- Don't treat the labeled synthetic QA image panels as production assets. They are mounted in a runner-owned test webroot and are not shipping catalog imagery.
- Don't promote this local component record into new global tokens or rewrite `DESIGN.md`/`.impeccable/design.json` without the required separate authorization.
- Don't describe this as completed purchases, all 99 MVC criteria or full shop-flow acceptance.

Evidence checked: all eleven PNGs in `.artifacts/mvc-catalog/browser/` were visually opened: home desktop/mobile; catalog desktop/mobile/tablet/empty mobile/error mobile; details desktop/mobile/selected mobile/sold-out mobile. Their companion `results.json` records `2026-10-31T11:23:52.555Z`, passing filter/paging/selection/gallery/image recovery/keyboard assertions, 3/2/1 columns, viewport-width documents, zero unnamed controls and `cartPostSubmitted:false`. No browser rerun or backend certification was performed in this documentation pass.

`finish-review.md` retains the original duplicate-indicator finding as history, then explicitly resolves it in the final verdict with `disposition: ship` for that scored fix and the public read/selection scope. Current source and the four relevant captures agree with that resolution; no material contradiction was found. WEB.K09.02–.04 remain **NOT RUN**: **bằng chứng một phần, phần POST ở bước 4**. The reported 291 passing SQL/WAF/API checks are separate regression evidence, not certification supplied by this document.

Preservation check: this pass writes only this artifact. Root `DESIGN.md` and `.impeccable/design.json` were fingerprinted before and after the write with unchanged SHA-256 values, respectively `0B3BA8552855AEFE7DB61C5DBF4EF411430739D03A93F639B6DAFB4E8E60E797` and `6DB787CE846EE3A780B71137AF1A9F3B2AC430AB5C3A0B1CC712B2E729B39543`. Application source was read only.
