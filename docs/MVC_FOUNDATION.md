# Customer MVC — foundation PR

This is step1 of six customer website PRs. It adds the MVC shell, shared layout, HTML error handling, customer-aware cookie events, automatic antiforgery and WebApplicationFactory SQL QA configuration. The catalog, account forms, cart/checkout and order controllers follow in separate PRs; navigation destinations for those screens are not implemented here. This PR is not final UI13 or K01-K15 acceptance.

## Approved boundary

The controlling inputs are the revised01_API_CONTRACT.md and05_CHECKLIST_MVC_KHACH_HANG.md supplied together for this MVC phase. The checklist has99 unique WEB IDs:48P0/43P1/8P2. Its WEB.GATE.04 explicitly permits the new Core IStoreService interface, new MVC adapter and host configuration. Existing infrastructure services, migrations, admin endpoints and session validator are unchanged. Program.cs only imports/registers/maps MVC and inserts its website middleware.

MvcStoreAdapter calls the existing services directly. Before an add it reads CatalogService and verifies that variantId occurs in the returned product's public variants. Missing, discontinued or mismatched selections return NOT_FOUND/404 before CartService.Set. This approved association check does not calculate prices, reserve inventory or replace service authorization. SQL remains authoritative when the cart service executes.

The cookie event wrapper delegates principal validation and every API redirect/error to the existing SessionAuthentication. Website challenges redirect locally to /Account/Login; denied website access returns HTML403. The original fixed eight-hour cookie/session policy remains unchanged. Actual login form/returnUrl validation will be tested in the account PR.

## Errors and forms

MVC unsafe methods have AutoValidateAntiforgeryToken globally; the existing X-CSRF-TOKEN header is retained. Website errors use encoded Razor HTML, no-store and generic recovery messages; unexpected500 responses expose only a traceId. API/health paths retain their existing middleware and JSON behavior. A full per-action error-to-form/redirect mapping will be added with each controller; the fallback is not a substitute for checkout PRICE_CHANGED reconfirmation.

FormErrors.AddSummary places a supplied service message at the top of ModelState without guessing a field from its text. The user explicitly approved this behavior for duplicate registration/profile data. The fixture exercises username/phone/email collisions, unchanged row snapshots and preserved model values. Real registration/profile form rendering remains NOT RUN until the account PR; no fake form is shipped as evidence.

## Executed verification

Release build:0 warnings/errors. The complete existing SQL/API suite plus11 new WebApplicationFactory groups passes **256 checks**. Tests create and dispose generated PhoneStore_QA_Rebuild_GUID databases under the existing explicit QA target guard. They do not use the demo database or PhoneStore_Rebuild. Run the normal --integration command for CI coverage, or --mvc-foundation for the focused groups.

| Evidence | Assertions actually executed |
| --- | --- |
| MvcFoundationChecks / Razor and assets | HTML encoding of configured script-like text; CSS/JS200; currency precision and UTC+7 day boundary |
| HTML/API errors | Website404/500HTML; API401/403/404JSON; Production500 hides exception details |
| Authentication | Fixed eight-hour SQL session and cookie flags; no sliding Set-Cookie; expired/revoked sessions redirect; ADMIN website rejection and unchanged ADMIN API access |
| Antiforgery | Missing/invalid JSON header rejected400; valid header accepted; missing token on a form that would add a real SKU rejected with unchanged DB |
| Duplicate policy | Real registration unique failures for username, phone and email; accounts/profiles/sessions snapshots unchanged; retained ModelState values and summary-only errors |
| Association | Valid SKU from another product and missing product return404; EF interceptor sees **zero cart-table commands**, with identical before/after stock, variants/prices, cart, orders/lines, payments, history and movement snapshots |
| Positive control | Matching pair calls real cart service, creates exactly one quantity1 row; H3/R0 per SKU remains unchanged, zero orders, only the two receipt movements |
| Discontinued product | Returns404, does not increase intercepted cart-command count and preserves the before/after snapshot |

The interceptor is tested with a positive control so “zero cart commands” is not inferred merely from a rolled-back transaction. Snapshot values, session IDs and password hashes stay in memory; they are not printed or exported. Duplicate rollback assertions use the existing service directly, not a substitute implementation.

Playwright Chromium renders the actual Kestrel host at1440px and390px: no horizontal overflow, keyboard skip link,404status, no JS exceptions, one submitted event for two requests and restoration after pageshow without enabling intentionally disabled buttons. The guest-only preview uses a QA-only configuration and performs no SQL reads/writes. These browser assertions cover the shell script, not unimplemented login/checkout flows or localStorage merging.

## Visual evidence and limits

The existing optionA vocabulary is preserved: navy/blue, white navigation, pale canvas, visible status text and mobile layout. No prices, stock balances or promotional product facts are invented. Storefront:Name, Introduction and ContactNote are configurable. The initial welcome panel is foundation content; final home/bestsellers and product imagery are step3.

- [Foundation desktop](evidence/mvc-foundation/foundation-desktop.png)
- [Foundation390px](evidence/mvc-foundation/foundation-mobile.png)
- [HTML404 desktop](evidence/mvc-foundation/error404-desktop.png)
- [HTML404390px](evidence/mvc-foundation/error404-mobile.png)
- [Browser measurements](evidence/mvc-foundation/browser-results.json)

All99 rows retain their official labels in the local .artifacts/mvc-audit/CHECKLIST_RESULTS.md, with explicit partial-coverage notes. Framework probes are registered only by WebApplicationFactory; they are not production routes. WEB.K09.02 is not marked complete until the actual catalog/add actions have evidence. WEB.BASE.02 still needs the real Quote/Merge routes; full UI03-UI07/UI13 screenshots and WEB.FLOW belong to later PRs. No automatic merge.
