> Review history: the initial full review found one material issue (fix). The final verdict below scores that single correction resolved (ship), at that scope only.

disposition: ship

Current disposition covers the single scored fix below. The original full-review finding is retained as historical evidence.

Inputs: code-led extension with the user's precise optionA direction; no approved comp or separate QUALITY BAR card supplied. Review covers public HOME/K06–K09 read/selection UI only. No browser was launched by the reviewer.

## persistence

Pass at this scope. PRODUCT.md, existing DESIGN.md and the existing foundation surface brief are present. The extension retains their navy/blue/white/pale world. The foundation-only documentation is older than this catalog extension; the reported pre-existing documentation drift is not authorization to rewrite the system. This precisely specified extension requires no new identity round or comp gate.

All eleven named captures were opened: home desktop/mobile; catalog desktop/mobile/tablet; details desktop/mobile/selected mobile/sold-out mobile; catalog empty/error mobile. Each shows the named content from the document top without missing or blank capture regions. browser/results.json reports 3/2/1 columns, viewport-width documents, zero unnamed controls, and no Cart/Add submission.

## fidelity

| Element | Finding | Evidence |
| --- | --- | --- |
| TYPE | match | The inherited Vietnamese system typography, bold product headings and tabular prices retain the pinned optionA voice. The pinned system-face choice takes precedence over an unrelated own-world display-face preference. |
| MATERIAL | match | Flat white/pale surfaces, thin borders and consistent stroke SVGs preserve the inherited frame. Explicitly marked QA fixture panels are test content, not shipping product imagery. Missing imagery has an explicit placeholder. |
| GROUND | match | Pale cool canvas, white header/footer and navy welcome panel match the direction contract and existing palette. |
| Catalog composition | match | Three desktop columns beside filters, two tablet columns and one mobile column; mobile filters collapse and invalid filters remain open with recovery text. |
| Filter disclosure | contradicted | The authored SVG chevron appears beside a second +/− glyph from catalog.css summary::after in desktop, tablet and mobile captures. The duplicate cue weakens the control and violates the craft-floor glyph-icon rule. |
| Detail selection and availability | match | Storage/color selections update displayed server-provided price, stock, SKU and specification rows coherently. Selected and sold-out captures show distinct states; unavailable text and disabled quantity/action are explicit. |
| Gallery and image recovery | match | Gallery thumbnails, main image and missing-image placeholder are present; supplied interaction evidence reports gallery and broken-image fallback checks passing. |
| Homepage | match | Current-month bestsellers sit below the inherited welcome panel, followed by introduction/contact/footer. No fabricated sales totals or discount claims appear. |
| Cart boundary | acceptable adaptation | The user's explicit cross-step decision authorizes rendering the POST form and antiforgery fields while postponing its action and execution to step4. This review certifies neither a working purchase nor a complete store. WEB.K09.02–.04 remain NOT RUN with partial GET/UI evidence. |

## ceiling

The specified restrained optionA composition reaches the intended visual scope once the duplicate disclosure indicator is removed. Additional display lettering, illustration, depth, ornaments or entrance animation would change an already pinned direction and are not owed here. Supplied browser evidence covers keyboard, no-JS selection, filter/paging behavior and quantity constraints; the full regression result is outside this visual review.

## material_fixes

1. Remove the +/− summary::after glyph rules from catalog.css; use the existing stroke SVG chevron as the sole disclosure indicator and show open/closed state consistently. Recapture the same viewport/state files and return for a verdict on this fix.

## keep

Preserve the user's optionA frame, labeled filters and recovery copy, server-provided selection values, explicit stock states, service-fed images/bestsellers, and the step3 Cart/Add/acceptance boundary.

---

## verdict

1. **Resolved — duplicate disclosure indicators.** Fresh catalog-desktop, catalog-mobile, catalog-tablet and catalog-error-mobile captures each show exactly one stroke SVG chevron: upward for the open desktop/error panels, downward for closed mobile/tablet panels. The +/− glyphs are absent. CSS confirms that the pseudo-element rules are removed and the SVG alone receives alignment and open-state rotation. These captures are valid and show no regression attributable to this fix. Browser results dated 2026-10-31T11:23:52.555Z retain the 3/2/1 grid and passing public UI assertions.

## remaining

Clear for the single scored fix. This verdict does not certify purchases, Cart/Add execution, backend acceptance or all 99 MVC criteria. WEB.K09.02–.04 remain NOT RUN with partial GET/UI evidence.

disposition: ship
