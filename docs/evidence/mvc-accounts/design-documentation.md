# Account extension design documentation

The K01–K05 customer account addition is an ordinary Operate extension of the approved optionA Monophone frame. The finished views preserve the incumbent visual world. This pass records local component behavior; it does not approve a durable system change. `DESIGN.md`, `.impeccable/design.json`, the existing surface brief and all application source remain untouched by this documentation pass.

## Evidence checked

- Read `PRODUCT.md`, `DESIGN.md`, `.impeccable/design.json`, `.impeccable/surfaces/src-phonestore-web-views-shared-layout-cshtml.md` and the Impeccable `reference/document.md` operating specification.
- Compared the four `src/PhoneStore.Web/Views/Account/{Register,Login,Profile,Password}.cshtml` files, shared `_Layout.cshtml`, `wwwroot/css/accounts.css` and `wwwroot/js/accounts.js` with the inherited `storefront.css` and `storefront.js` rules.
- Opened all ten full-page captures in `.artifacts/mvc-accounts/browser/`: register, login, profile and password at 1440px and 390px; register-duplicate-mobile; and password-error-mobile. Every file shows its named screen from the document top with header and footer, complete content, and no blank capture region or visible clipping.
- Read `browser/results.json`, timestamped `2026-10-31T07:48:43.681Z`, and `finish-review.md`, whose disposition is `ship`. The account source files checked were last written before these evidence artifacts. This pass reviewed the evidence; it did not rerun the browser or SQL scenarios.

The results record reports eight normal-screen checks with document width equal to viewport width and zero unlabelled controls; no recorded errors; keyboard access to the form; and busy/disabled registration and login buttons reading “Đang xử lý…”. It records one registration POST, one login POST during the double-submit scenario, and two login POSTs across the full scenario including login after a password change. Its generated QA database had one account/profile, zero sessions after password change/logout, and no passwords in host logs. These are the recorded scenario's observations, not independent backend certification by this design pass.

## Comparison with the incumbent system

| Aspect | Finished account extension |
| --- | --- |
| Palette and material | Retains navy ink, blue links/actions, the white frame and form, pale cool page ground, fine line borders and flat surfaces without shadows. Forms reuse the existing paper, ink, muted, line, blue and radius CSS variables. |
| Type | Inherits the readable Vietnamese system stack and body rhythm. The local task heading uses `clamp(2rem,3.5vw,3rem)`; labels use weight 650; supporting notes use .88rem and field errors .9rem. These are account-local measurements, not new global type tokens. |
| Shape and focus | Forms reuse the shared rounded radius; inputs retain 10px corners and a 48px minimum height. Blue submit actions reuse the existing button. The shared amber keyboard outline remains visible, including on the captured password error field. |
| Navigation | Preserves the masthead, search, horizontal primary navigation, footer and skip-link markup. Signed-in masthead links add the account destination and POST logout. Local account navigation marks the current page with text weight and `aria-current=page`. |
| Composition | Desktop places task introduction/navigation beside the form. At 390px the introduction and form stack, account navigation wraps and the form stays inside the shared mobile container. |

No material contradiction with the approved optionA direction or the reviewer's `ship` disposition was found in the checked source and captures.

## Local account components and behavior

The account region is capped at 1040px with desktop columns of 1fr and 1.15fr, and a responsive gap of 32–88px. At widths at or below 800px it becomes one column capped at 560px with a 20px gap. The white form has a shared line border and radius, with padding of 32px on desktop, 24px at or below 800px and 20px at or below 400px. The 800px and 400px breakpoints are source-verified; captures directly cover 1440px and 390px only.

Register presents username/password/name/phone and optional email/address. Login presents username/password and the eight-hour session explanation. Profile presents editable contact details, optional email/address, local account navigation and logout. Password change presents current/new password plus the explanation that changing a password signs out all devices and requires another login. The new-password guidance says 10–128 characters with letters and digits. The registration completion note directs the customer to sign in after creating an account.

Visible labels are associated through Razor label/input helpers, appropriate autocomplete/type attributes are supplied, and validation spans are connected through `aria-describedby`. Server-invalid inputs receive `aria-invalid`. Form-level errors use an alert summary with a focus target; field errors include explanatory text and an error border. The captured duplicate registration uses a form-level summary naming username, phone or email without asserting which field collided. It preserves ordinary values and leaves the password empty. The captured wrong-current-password state places feedback by that field, shows the focus outline, and leaves both password fields empty.

The shared submit handler marks valid POST forms busy, disables unnamed submit buttons and prevents another submit while processing. The account handler adds the Vietnamese processing label after that guard runs. Both handlers restore their presentation state on `pageshow`. Account validation focuses the first summary or invalid input. Authentication and duplicate safety remain server responsibilities; the client guard communicates progress.

## Scope and preserved limits

This documentation covers customer Account K01–K05 presentation and the supplied evidence. Guest-cart merge WEB.K02.04, catalog, cart/checkout, order pages and complete 99-item MVC acceptance remain deferred. Links and introductory copy referring to those later destinations do not establish their implementation. No old backend service, schema, admin API or WinForms behavior is certified here. No merge is authorized or performed.

The root design documents still describe the foundation PR. That historical boundary is intentionally preserved under the ordinary-extension instruction; this local record supplies the account-specific evidence. The earlier context run reported `design-sidecar-stale`: `DESIGN.md` was edited after `.impeccable/design.json` was generated, so ramps, shadows, motion or snippets may contradict it. This existing notice is reported without further drift diagnosis or repair. No account-only heading size, form composition, error color, breakpoint or inherited font is promoted into a new global design-system rule.

Preservation checksums (SHA-256):

- `DESIGN.md`: `0B3BA8552855AEFE7DB61C5DBF4EF411430739D03A93F639B6DAFB4E8E60E797`
- `.impeccable/design.json`: `6DB787CE846EE3A780B71137AF1A9F3B2AC430AB5C3A0B1CC712B2E729B39543`
