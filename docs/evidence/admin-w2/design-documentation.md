---
name: Monophone Admin W2 native catalog extension
description: Observed Option A WinForms catalog, aggregate editor, detail and lookup surfaces.
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
spacing:
  list-inset: "16px"
  editor-inset: "20px"
  field-stack-inset: "24px"
  toolbar-inset: "12px"
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

W2 extends the incumbent W1 Windows workbench for product search, product details, complete product editing and brand/category maintenance. Vietnamese labels, Segoe UI, pale ground, white work surfaces and blue actions keep administrative data central. The catalog is a retained aggregate of product fields, variants and ordered image URLs; the interface exposes server-owned values and explicit business status.

This is a scoped scan-mode record of the implemented native extension, not a new identity or a root design refresh. Root DESIGN.md, PRODUCT.md and the historical MVC design files remain preserved. Current authority is `.artifacts/admin-w2/direction.md`, `src/PhoneStore.Admin/AdminTheme.cs` and the incumbent W1 documentation. The private UI08 Word reference remains private. No HTML/CSS detector applies to this WinForms artifact, and no HTML/CSS preview sidecar is introduced.

Source basis: `CatalogUi.cs`, `ProductListForm.cs` (including `ProductDetailsForm`), `ProductEditor.cs`, `LookupForm.cs`, `AdminTheme.cs`, and `src/PhoneStore.Admin.Client/Catalog.cs`. The W1 design documentation supplies continuity. This documentation pass opened the real-QA editor and refreshed fake detail PNG; the supplied finish review examined all eight fake captures, and the final verdict reopened the corrected detail and variant captures.

Evidence scope is explicit:

| Packet | Label and coverage | Limit |
| --- | --- | --- |
| `forms/results.json` and eight PNGs | `fakeApi: true`, `qaOnly: true`; five STA fake-API check groups; list, empty list, retained-error editor, general editor, variants editor, inactive lookup, stop confirmation and details | Synthetic appearance/state evidence, not API or SQL certification |
| `real-forms/results.json` and two PNGs | `fakeApi: false`, `qaOnly: true`; real-QA list and editor | A distinct real API/SQL QA packet; two captures do not certify all states |

Both manifests record 120 DPI. Seven fake forms and both real forms are 1280×800; the shared confirmation is 620×330. These dimensions and declared DPI autoscaling do not complete the W6 DPI/device matrix or establish formal accessibility conformance. The image tab, horizontally scrolled final variant columns, and keyboard/loading interactions lack separate visual captures. No CI pass is claimed here; CI remains pending. Audit PNGs are evidence, not shipping product rasters. No generated raster or new product raster asset is introduced.

The finish review initially scored one defect: the detail availability header split inside “bán”, leaving a lone character. `finish-verdict.md` resolves that sole finding after the detail and variants recaptures and records `disposition: ship`. The detail availability column and editor availability column reserve a minimum width of 150px. The verdict covers the scored header correction and visible regressions in those views, not a new whole-surface, API or SQL audit.

## Colors

The shared blue marks submission/search/create actions. Ink carries headings and ordinary copy. White tables and field stacks contrast with the pale canvas. Secondary actions retain a white background and ink text. Navy and muted slate remain incumbent theme tokens; this extension does not introduce a separate accent palette. Selection, text-field borders, tab states and disabled rendering use native Windows controls and should not be documented as custom fixed colors.

**The Status in Words Rule.** Business status appears as “Đang kinh doanh” or “Đã ngừng”, or a labeled business-state checkbox; color alone does not carry this distinction.

## Typography

Installed Segoe UI supplies regular body/input and bold heading/field-label roles. Shared form body, input and W2 heading sizes are recorded in frontmatter. TextBox-based field labels use bold body type; combo labels use the shared regular label helper. No bundled webfont or display asset is present.

Tables retain native typography and automatic header height. Cost and price cells use Vietnamese `N0` formatting, align right and show “Sau khi lưu” for null new-draft monetary values. Availability must remain readable at word boundaries; the scored header correction reserves width rather than shrinking the table text.

## Layout

Catalog forms inherit centered windows with a default and minimum outer size of 1280×800 and `AutoScaleMode.Dpi`. Native table/flow layouts and scrollbars provide adaptation; there is no web breakpoint system. Minimum-size declarations are source behavior, not evidence for every display configuration.

List and lookup layouts stack a heading, wrapping toolbar, expanding table and footer. The list footer contains previous/next actions and the count/status message. Product detail uses a horizontal split: scrollable read-only product text and image URLs above the variant table, with a persistent explanatory footer.

The editor places general information, variants/server prices and images in three native tabs. General information uses a vertically scrollable field stack. Save, reload/reconcile, disabled W4 handoff and the operation message remain outside the tabs. The variants table uses intentional horizontal scrolling to retain all aggregate columns at useful minimum widths. The current viewport need not show its final columns simultaneously. Toolbar wrapping is intentional at the minimum window width.

## Elevation & Depth

Solid surfaces, native borders, tabs, gridlines and window chrome establish separation. There are no custom shadows, gradients, texture, animation or decorative imagery. Image URLs are editable catalog data and read-only detail text, not rendered preview assets or broken image placeholders.

## Shapes

The extension uses flat rectangular theme buttons and native rectangular fields, tabs, checkboxes, combo boxes and tables. Shared buttons have a minimum size of 120×42. Tables omit row headers and outer borders, select one complete row at a time, allow both scroll directions and disallow direct row addition/deletion. New variants enter through the explicit add action. There is no custom corner-radius scale.

## Components

### Search, paging and selected actions

The read-only product list exposes code, name, brand, category, business status and variant count. Search sends query, brand and state filters to the server, resets to page one and shows 20 rows per page. Enter triggers search. Previous/next enablement follows page and total; detail, edit and business-state actions require a selected row. Busy work disables filters, actions and the grid and displays “Đang tải…”. An empty response gives a recovery instruction to change the query or filters. Load failure clears rows and shows the mapped error.

### Read-only detail

Details show code/name, status, specifications, description, main image URL and supplemental image URLs in order. The read-only variant table exposes SKU, color, storage, cost, profit percentage, server price, on-hand, reserved, available and version. A persistent explanation defines available as on-hand minus reserved and identifies price/balances as server values. Loading and errors use the separate status label.

### Product aggregate editor

General fields collect code/name, brand/category, specifications, description and business status. Active lookups plus the product's currently selected inactive lookup remain available on edit, preserving existing associations. The image tab accepts an optional main URL and at most 12 ordered supplemental URLs, one per line. Client validation accepts HTTPS without credentials or permitted `/images/` paths; invalid/blank supplemental entries produce text guidance.

Variants retain IDs and versions, with no delete action. Editable fields include SKU, color, storage, new-variant cost, profit percentage, business status and suggested quantity. Existing variant cost cannot be edited; request construction retains original cost. Server price, on-hand, reserved, available and version are read-only. New drafts start without authoritative price/balances, and the interface waits for server readback instead of calculating a pretend server price.

Save first completes the active cell and validates required fields, selected lookups, 1–100 variants, positive storage/cost/quantity and allowed percentage precision/range. Grid conversion errors provide numeric guidance. Client/server errors keep the draft and show actionable mapped text. During work, inputs and save/reload/add actions disable and a separate label shows “Đang xử lý…”. Successful save rebinds authoritative product data and reports refreshed price, stock and version. If write succeeds but readback fails, workspace state requires readback/reconciliation before another write; reload can reconcile by known ID or exact product code. An unmatched reconciliation preserves the entered draft and explains the next action.

Reload on an already loaded product normally confirms replacement of unsaved changes; reconciliation follows its dedicated recovery path. Product or existing-variant business-state changes require confirmation before save. These are retained-state changes, not deletion.

**The Authoritative Readback Rule.** Keep server price, balances and version visible as server-owned values; show success only after readback, and preserve the draft when recovery is required.

### Lookup management and confirmation

Brands/categories use a read-only table plus name and business-state inputs. All lookup pages are fetched in batches of 100, including inactive entries; the footer makes that inclusion explicit. New clears selection and the name, resets active status and focuses the name field. Save validates a 1–100 character name, preserves the submitted name/state on failure, and reloads server data after success. Busy controls disable with an external operation message. Empty lists invite creation.

Changing an existing lookup's business state confirms that affected products cease appearing on the website while data/history remain. Product-list status change fetches the full current aggregate and confirms the named product before saving the retained aggregate with the new active flag. Editor status confirmation similarly describes retained data/history. The shared confirmation offers return/confirm, with initial focus on return and native Escape/Enter wiring inherited from W1. The fake confirmation capture demonstrates presentation, not a real state-changing transaction.

### W4 handoff

“Lập phiếu nhập · W4” remains disabled in the constructor and every busy-state update. Typed handoff data exists for saved variant IDs/SKUs and suggested quantities, but W2 does not expose an operational purchase workflow. Suggested quantity does not save a receipt or increase stock. W4 wiring is a known handoff, not a completed W2 capability.

## Do's and Don'ts

- **Do** preserve the incumbent native palette, Vietnamese labels, explicit state text and separate operation messages.
- **Do** retain all existing variants, show read-only server fields and preserve draft input on error.
- **Do** keep native scrolling and useful column widths, including readable availability headers.
- **Do** distinguish synthetic fixtures from real-QA captures and qualify the exact review scope.
- **Don't** publish private UI08 references or treat audit PNGs as product assets.
- **Don't** imply that W4 purchasing, full W6 DPI coverage, formal accessibility certification or CI validation is complete.
- **Don't** transplant historical MVC HTML/CSS design rules into this native extension or overwrite root DESIGN.md.
