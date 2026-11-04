---
name: Monophone Admin W3 native contacts extension
description: Observed native customer maintenance, supplier maintenance and bounded read-only handoffs.
colors:
  ink: "#19263d"
  navy: "#142d5c"
  blue: "#2454dc"
  canvas: "#f4f6fb"
  muted: "#52647f"
  paper: "#ffffff"
typography:
  body:
    fontFamily: "Segoe UI"
    fontSize: "10pt"
    fontWeight: 400
  input:
    fontFamily: "Segoe UI"
    fontSize: "12pt"
    fontWeight: 400
  heading:
    fontFamily: "Segoe UI"
    fontSize: "22pt"
    fontWeight: 700
  field-label:
    fontFamily: "Segoe UI"
    fontSize: "10pt"
    fontWeight: 700
spacing:
  list-inset: "16px"
  field-stack-inset: "24px"
  toolbar-inset: "12px"
  input-bottom: "12px"
components:
  button-primary:
    backgroundColor: "{colors.blue}"
    textColor: "{colors.paper}"
    padding: "5px 12px"
  button-secondary:
    backgroundColor: "{colors.paper}"
    textColor: "{colors.ink}"
    padding: "5px 12px"
---

## Overview

**Creative North Star: "Option A, native administration"**

W3 extends the W1/W2 Windows workbench for safe customer and supplier maintenance. Vietnamese labels, Segoe UI, navy ink, blue actions, white work surfaces and pale canvas preserve the incumbent system. Contact fields, textual business status and explicit consequences lead the interface. Authentication secrets and order mutation remain outside this maintenance surface.

This is a scan-mode record of the ordinary native extension pinned by `direction.md`. Root PRODUCT.md and DESIGN.md remain historical MVC records and are preserved. The incumbent native authority is `src/PhoneStore.Admin/AdminTheme.cs` and `docs/evidence/admin-w2/design-documentation.md`. No new visual identity, approved comp or concept tournament is owed. The private UI11 Word reference remains private and supplies critique context for the deferred full order surface; it is not an approved W3 reproduction target. No HTML/CSS detector applies to C# WinForms. No HTML/CSS component sidecar or new product raster asset is introduced; audit PNGs are evidence, not shipping assets.

Source basis: `AdminTheme.cs`, `CatalogUi.cs`, `ContactLists.cs`, `CustomerForms.cs`, `SupplierEditor.cs` and `src/PhoneStore.Admin.Client/Contacts.cs`. This documentation pass directly opened the corrected synthetic profile-saved capture and real-QA supplier picker. `finish-review.md` records examination of all eleven synthetic and five real-QA captures; `finish-verdict.md` records reopening all sixteen after the copy correction.

| Packet | Evidenced states | Boundary |
| --- | --- | --- |
| `forms/results.json`, eleven PNGs | `fakeApi: true`, `qaOnly: true`, five check groups; customer list, empty list, details, retained profile error, saved profile, locked customer, customer orders, read-only order detail, supplier list, retained supplier error, active supplier picker | Actual STA native controls backed by synthetic API fixtures; presentation and control-state evidence, not SQL/session certification |
| `real-forms/results.json`, five PNGs | `fakeApi: false`, `qaOnly: true`; real-QA customer profile/list and supplier editor/list/picker | Distinct real WinForms/Kestrel/SQL QA packet; five captures do not cover every state or prove stored effects by themselves |
| `sql-results.json` | Nine QA check groups; eight rejected-write records with unchanged before/after hashes; preserved old-order snapshots; session evidence | Separate supplied API/SQL evidence, not independently rerun by this documentation pass |
| `desktop-sql-results.json` | Distinct desktop QA database: one customer, one active supplier, zero orders, inventory rows, payments and journal rows | Desktop QA data scope, not the populated order/session fixture database |

Every capture manifest records 1280×800 and 120 DPI. Source declares DPI autoscaling; the packet does not complete the full W6 DPI/device matrix or establish formal accessibility conformance. Confirmation, keyboard, loading and long-content behavior have source/control evidence rather than separate screenshots here. CI remains pending.

The finish review found one product-copy issue: administrator-facing text exposed GET and W3/W4/W5 delivery terms. The revised saved-profile text explains preserved old-order recipient information; order footers state the read-only limitation; the picker explains that selecting a supplier does not create or save a receipt. `finish-verdict.md` marks that sole finding resolved and records `disposition: ship`, with no visible regression attributable to the copy batch. This is a score of that finding only, not a fresh whole-surface review or independent API/SQL/session certification.

## Colors

The primary blue marks search, save and create actions. Ink carries headings, field labels and ordinary copy; white fields and tables sit against the pale canvas. Secondary buttons use white with ink. Navy and muted slate remain shared incumbent tokens without introducing another W3 palette. Native selection, focus, disabled treatment, input borders and read-only backgrounds remain platform-rendered; do not infer fixed custom tokens from the Windows chrome in captures.

**The Status in Words Rule.** Customer state is “Đang hoạt động” or “Đã khóa”; supplier state is “Đang hoạt động” or “Đã ngừng”, with a labeled activity checkbox in the editor. Color alone does not carry business state.

## Typography

Installed Segoe UI supplies the body, inputs, bold headings and bold field labels recorded in frontmatter. No webfont, ornamental heading family or bitmap type is present. Labels use the shared helper with automatic sizing and vertical margins. Search and edit fields use the larger input role; read-only details inherit the native form typography.

Order status and payment codes map to Vietnamese text. Order times use the shared UTC+7 formatter and amounts use the shared money formatter. The customer order table explicitly labels its time column “Thời gian UTC+7”. Technical transport and delivery-milestone vocabulary belongs in evidence and source, not in operation feedback.

## Layout

Forms open centered with a default and minimum outer size of 1280×800 and `AutoScaleMode.Dpi`. Native table/flow layouts and scrolling handle available space; no web breakpoint system exists. Numeric spacing tokens above describe source layout units, not a new DPI acceptance claim.

Customer and supplier lists stack a heading, wrapping search/state/action toolbar, expanding table and paging/status footer. The shared query box is 280 units wide and the state selector 180. Customer search identifies name, phone and email; supplier search uses name, phone or code. Columns have useful minimum widths with native horizontal scrolling: customer name 240, other customer columns 150; supplier name/address 240 and other supplier columns 150.

Profile and supplier editors place a white, scrollable field stack at the top, using the shared inset. Address is multiline with a source height of 100. An operation message and save/reload toolbar follow the fields. Customer details use an expanding scrollable read-only text area with operation feedback and explicit edit/state/orders/reload actions below. The active supplier picker similarly has a heading, expanding native list, explanatory status and choose action. These views preserve the W2 hierarchy without a dashboard or web layout scaffold.

## Elevation & Depth

Solid surfaces, native borders, gridlines, focus/selection and window chrome establish separation. There are no custom shadows, gradients, textures, animations or decorative rasters. Protected-focus native dialogs are the incumbent pattern for editing and consequential confirmation.

## Shapes

Flat rectangular theme buttons accompany standard rectangular native fields, checkboxes, combo boxes, list boxes and tables. Shared buttons have a minimum size of 120×42 with the padding recorded above. There is no custom corner-radius scale. Tables hide row headers and outer borders, select one full row at a time, permit both scroll directions and prohibit direct row addition/deletion. Contact and order tables are read-only.

## Components

### Search, paging and operation feedback

Search resets to page one, sends the query and optional active-state filter to the server, and shows 20 rows per page. Enter invokes search. Previous/next enablement follows page and total. Customer detail and supplier edit require a selected record; supplier creation and picker entry remain available when idle. Busy list loading disables query, filter, search, grid and actions and displays “Đang tải…”. An empty list invites changing the query or state; a failure clears displayed rows and shows the mapped failure message.

Customer rows show account, name, phone, optional email and state. Supplier rows show code, name, phone, address and state. Business actions belong in explicit buttons, not editable table cells.

### Four-field customer profile

`CustomerProfileRequest` and the editor expose exactly full name, phone, optional email and address. Account name, credentials, role and activity state are not profile fields. The editor loads an existing customer before enabling save. Name is required and limited to 120 characters; phone accepts 9–15 digits with an optional leading plus; email is optional and validated up to 254 characters; address is limited to 500. The text box's phone capacity is not a substitute for the stricter phone validation.

Client/server errors preserve the entered fields and use a separate operation message. Busy work disables fields and save/reload. Save rebinds the authoritative customer readback before reporting success; the current success copy states that old-order recipient information is preserved. Reload normally confirms replacement of unsaved changes, while reconciliation uses its recovery path. A failed post-write readback marks the pending customer and blocks another workspace write until readback/reconciliation succeeds.

**The Narrow Maintenance Rule.** Keep the four-field request boundary intact and show account-state changes separately with their own consequences.

### Customer details and confirmed account state

Details show account, contact fields, textual activity state and creation time from the server. “Khóa khách” or “Mở khách” follows the current record. Lock confirmation states that every customer session will be revoked while profile/order data remain. Unlock confirmation states that the customer must log in again and old sessions will not return. The request carries `ExpectedActive` from the displayed snapshot to protect against a stale state change; success rebinds server readback.

The confirmation injection used by native QA proves control behavior and copy, not real revocation by itself. Separate supplied `sql-results.json` session evidence records two sessions before lock, zero after lock, old tokens returning 401/401 after unlock, and successful fresh login. Old-order snapshot comparisons remain unchanged. These are bounded QA results, not a claim derived from the locked screenshot or independently reverified here. Native confirmation uses the shared return/confirm dialog and its incumbent keyboard/focus behavior.

### Customer orders and working read-only detail fallback

The customer order list is operational: it fetches paged server orders and shows order code, Vietnamese status, UTC+7 time, total and COD status. It handles loading, empty, error and page boundaries; detail requires selection. A typed `OrderRequested` event hands off the selected order ID when a consumer is attached. With no consumer, the shipping action opens `OrderPeekForm`, which fetches the selected order from the server.

The fallback displays order code/status/time, recipient, phone, address, total, COD status and line SKU/name/quantity/unit price/line total in a scrollable read-only text area. Its persistent message identifies information as read-only and recipient/prices as preserved at order time. Load errors clear detail text and show a mapped message. This is a working minimal W3 fallback, not a disabled placeholder; full W5 fulfillment, history, invoice and order-mutation controls remain deferred. The private UI11 full order composition is not claimed as implemented.

### Supplier editor and list-based readback

The five supplier fields are code, name, phone, address and activity. Code/name/phone/address are required; code allows up to 30 characters, name 200, address 500, and phone uses the shared validation. New suppliers start active. Editing waits for a loaded record. Errors preserve draft fields. Changing an existing supplier's activity confirms that inactive suppliers leave selection for new receipts while data/history remain.

Supplier persistence deliberately reads back from the server list, not an invented supplier-detail endpoint. After create/update, `FindSupplier` scans 100-row pages queried by code, matching the returned/known ID; a known ID may fall back to an unfiltered scan if needed. Pending ID/code preserve recovery context. Success binds the found authoritative row and reports refresh from the server list. Readback failure keeps pending state, requires readback/reconciliation and prevents a second workspace write; reload uses known ID/code and confirms replacing loaded unsaved input on the ordinary path.

**The Authoritative Readback Rule.** Report save success after binding server data; preserve input and reconciliation context when the write outcome or readback needs recovery.

### Active-only supplier picker

The reusable picker fetches every active-supplier page in batches of 100, with `active=true`. It rejects an inactive response row or a prematurely empty page rather than silently accepting an inconsistent list. The choose action starts disabled, enables only for a nonempty loaded list and checks that the selected record is active before returning `Selected` with `DialogResult.OK`. Empty/error paths provide guidance and prevent selection.

The footer states that only active suppliers appear and selection does not create or save a receipt. This is a real reusable selection component; purchase creation and the W4 receipt workflow remain deferred. Choosing a supplier does not mutate stock, payments or purchasing records.

## Do's and Don'ts

- **Do** preserve the incumbent native palette, Vietnamese labels, useful table widths and separate operation feedback.
- **Do** keep customer profile editing within four contact fields and confirm account-state consequences explicitly.
- **Do** preserve draft fields on errors and use authoritative readback/reconciliation after writes.
- **Do** retain the working read-only order fallback and active-only supplier result contract until their consuming workflows arrive.
- **Do** separate synthetic appearance evidence, real desktop QA and supplied API/SQL/session proof.
- **Don't** expose credentials, mutate orders or imply selection creates a purchase in this maintenance workbench.
- **Don't** expose GET or W3/W4/W5 delivery planning terms in product feedback.
- **Don't** publish private UI11 input or turn audit screenshots into product assets.
- **Don't** claim full W4/W5 workflows, CI validation, full W6 DPI coverage or formal accessibility acceptance.
- **Don't** transplant historical MVC HTML/CSS rules into native controls or overwrite root PRODUCT.md/DESIGN.md for this extension.
