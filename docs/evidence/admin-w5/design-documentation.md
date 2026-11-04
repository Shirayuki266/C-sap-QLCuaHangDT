---
name: Monophone Admin W5 native orders extension
description: Observed order snapshots, conditional commands, COD reconciliation and native invoice preview within incumbent Option A.
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
  confirmation-heading:
    fontFamily: "Segoe UI"
    fontSize: "18pt"
    fontWeight: 700
spacing:
  list-inset: "16px"
  detail-inset: "20px"
  invoice-inset: "24px"
  confirmation-inset: "28px"
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

W5 extends the incumbent Windows administration workbench in Operate mode. Its task is to expose the saved order, separate COD and physical-return truth, and offer only the commands appropriate to the current snapshot. Vietnamese labels, Segoe UI, blue commands, navy ink, pale canvas, white data surfaces and familiar native controls continue W1–W4.

This scoped scan follows `reference/document.md` and the ordinary-extension rule in `reference/new-work.md`, using `direction.md` as the surface contract. No durable shared-system change was introduced or owed. Root `PRODUCT.md`, `DESIGN.md` and `.impeccable/design.json` remain preserved historical MVC records. AdminTheme and W4 native documentation remain the desktop authority. This file records the extension; it does not create another root identity or web preview sidecar.

Source inspected: `src/PhoneStore.Admin/{AdminTheme,OrderForms,InvoiceForm,CustomerForms,MainForm,CatalogUi,ConfirmationDialog}.cs` and `src/PhoneStore.Admin.Client/Orders.cs`. W4 `design-documentation.md` and root PRODUCT/DESIGN were read for authority and scope. The private `.artifacts/admin-w3/UI11-reference.png` was opened as report context: order/customer utility, status, table and confirmation emphasis. Its web-like card/sidebar/progress treatment is not an exact approved desktop comp.

This documentation pass directly opened the six `real-forms/UI11-real-<status>.png` states, `UI11-real-return-received.png`, `UI11-real-history-actors.png`, `invoice-real-snapshot.png`, `PrintPreview-real-installed-driver.png`, and synthetic `forms/PrintPreview-no-installed-printer.png` and `forms/customer-to-full-order.png`. These representative images agree with the source and incumbent tokens. The full five-section `finish-review.md` independently records its review of all **34** supplied PNGs and disposition **ship**, with no material findings; this document does not claim a fresh full-capture review.

Both results manifests were inspected. `forms/results.json` identifies **18 fake-API QA PNGs** and six W5 STA groups; `real-forms/results.json` identifies **16 real-backend QA PNGs**. Ordinary frames are 1280×800 at the reported 120 DPI; protected dialogs are 620×330. Actual installed-driver PrintPreview is **1886×1266**, not evidence of a 1280×800 preview inspection. The packet supplies 31 total Windows form groups including six W5, 51 combined client groups including five W5 and its 48-cell matrix, and 322 prior regression passes. These are local supplied results, not reruns by this documenter. Windows CI is not yet published.

Financial/concurrency proof is separately attributed to `sql-results.json`, `sql-states.json` and `desktop-sql-results.json`, with `real-ui.log` reporting nine real desktop SQL checkpoints. Their structure, result summaries and a stored initial-order/payment/stock snapshot were sampled. No independent SQL execution or exhaustive snapshot recomputation occurred here. Synthetic control captures prove presentation and branching, while real QA desktop captures show the displayed workflow; neither alone certifies financial effects.

## Colors

The frontmatter records the five existing AdminTheme colors plus the explicit white surface. Blue carries primary commands, ink readable content, canvas the form ground and paper the data regions. Navy remains the shell navigation ground and muted remains an incumbent shared token. W5 adds no palette or status-color taxonomy. Status, COD and return outcomes are explicit words and amounts.

Selection blue, disabled controls, field borders, gridlines and window chrome are Windows-rendered. Their observed appearance is not a newly defined theme token or a formal contrast result. Cancel and fail use the shared white secondary button; physical/financial consequences are explained in confirmations rather than encoded solely in color.

## Typography

Forms inherit Segoe UI 10pt regular; shared text inputs use 12pt. Order list/detail and invoice headings use 22pt bold; shared confirmation headings use 18pt bold. Shared labels auto-size with eight-unit vertical margins. No custom webfont, tracking or line-height scale was introduced. Read-only multiline summaries retain native text behavior rather than a raster layout.

Vietnamese amounts use the existing money formatter. Date filters and history explicitly identify UTC+7; the list explains that the ending date is inclusive. Snapshot labels distinguish saved product name/variant/price from current catalog data. History names the actor, transition, time and reason.

## Layout

AdminTheme centers ordinary forms at default/minimum outer size 1280×800 using `AutoScaleMode.Dpi`. Values here are WinForms source layout units; captured 120-DPI coordinates need not equal unscaled source pixels. Native scrolling and wrapping are the adaptation model. No web breakpoint rules were added.

OrderList uses CatalogUi's four rows: 22pt heading, wrapping filter/action toolbar, expanding read-only grid, and wrapping paging/status footer. Outer inset is 16 and toolbar inset 12. Query width is 270, status selector 175, and each optional checkbox-enabled date picker 150. The seven columns cover code, recipient, phone, order status, COD, UTC+7 creation time and total, with 180 minimum widths and both-axis scrolling. Paging uses 20 rows. Busy guards execute before page mutation; loading disables filters, search, detail, grid and paging. Empty, permission/error and populated messages remain distinct.

OrderDetail uses an eight-row one-column layout with inset 20: heading, fixed 155-unit scrollable summary, expanding snapshot/history tabs, reason guidance, reason input, local reason error, operation feedback and wrapping commands. Snapshot name minimum width is 250 and other line columns 170; history reason minimum is 350 and others 210. Main summary keeps recipient/contact/address, saved total, independent COD state/amount/time, return flag and version together. Tabs retain the snapshot and actor timeline without displacing the reason/recovery/actions area.

Invoice uses inset 24 and five rows: server title, wrapping server notice, expanding scrollable read-only content, feedback and preview/reload toolbar. Notice width is constrained to 1150. The shared confirmation frame is 620×330 with inset 28, 530-wide description and a scrollable stack for long text. MainForm continues its 230-unit navy sidebar and opens the order list from “Đơn hàng”. Customer navigation opens the same full OrderDetailForm, then reloads the customer's order list when it closes.

## Elevation & Depth

Solid surfaces, native borders, tabs, selection/focus and standard window chrome convey grouping. No new custom shadow, gradient, texture, animation or product raster was introduced. Consequential commands use the incumbent modal confirmation boundary. Native PrintPreview supplies its own toolbar, page canvas, zoom and scrolling.

## Shapes

Shared buttons remain flat rectangles with minimum 120×42 and horizontal/vertical padding 12/5. Fields, selectors, date controls, tabs and grids retain native geometry; there is no custom radius scale. Grids suppress row headers and outer borders, select a single full row, disallow direct row creation/deletion, and support both-axis scrolling. Order snapshot/history/list grids are read-only.

## Components

### Conditional order commands

The display policy is centralized in OrderPresentation; server validation remains authoritative. Reload/reconciliation stays available when idle. CustomerCancel belongs to the separate customer contract and is not an admin command button.

| Saved state | Offered admin actions besides reload |
| --- | --- |
| CHO_XAC_NHAN | Confirm; cancel |
| DA_XAC_NHAN | Cancel; dispatch; invoice |
| DANG_GIAO | Complete + collect COD; fail; invoice |
| HOAN_TAT | Invoice |
| DA_HUY | None |
| GIAO_THAT_BAI, return not received | Receive return |
| GIAO_THAT_BAI, return received | None |

Reasons are required for cancel and delivery failure (also customer cancellation in the shared client policy), optional for receive-return, trimmed, and limited to 1000 characters. Local validation places feedback beside the field and focuses it. Errors retain the reason; success clears it only after authoritative readback.

Confirmations identify the order and recipient. Completion names the saved server total as COD to collect. Cancel explains release of held quantity without changing physical stock; dispatch explains physical issue and reservation reduction; failure says goods have not returned and COD is uncollected; receive-return requires actual physical receipt and describes a single server stock return. The shared dialog initially focuses “Quay lại”, has explicit confirm/dismiss actions and native accept/cancel bindings. These source protections do not amount to an exhaustive keyboard or assistive-technology audit.

### Snapshot, COD and authoritative recovery

The response exposes payment status as a string, not separate amount/collection-time fields. Presentation derives displayed collected amount as the saved order total only when server payment is DA_THU; otherwise zero. Collection time is saved CompletedAt only for DA_THU. This contract-based derivation must not be described as independent payment-amount fields or local accounting authority. ReturnReceived is separate from order status and COD.

Each intention retains the saved version, operation key, action and trimmed reason. Busy state blocks commands. Uncertain transport/global reconciliation or operation readback requirements disable action buttons and reason editing; reload resolves the retained intention before another write. Successful execution binds a fresh server order before reporting success. On 409, the form attempts authoritative resolution, refreshes offered actions and retains the entered reason for inspection; a failed resolution leaves reconciliation required. A failed explicit load clears Current, summary and grids, preventing stale actionable content. Reconciliation compares version, state and return flag before determining that the old intention is finished.

Production CustomerDetails wiring subscribes to the customer-order handoff and opens full OrderDetailForm, then refreshes the customer list. The legacy read-only peek remains only the no-subscriber fallback; it does not describe the production handoff path.

### Invoice and native printing

The invoice preserves server Title, Notice and order snapshot in the on-screen content and rendered preview. The shown notice identifies an internal sales document rather than a tax electronic invoice. PrintDocument renders the same InvoiceText in Segoe UI 10pt, using page margins and continuing remaining text across pages; the actual installed-driver capture shows a rendered page.

Printer availability is tested before Preview. With no installed printer/driver, feedback explains installation and keeps the invoice readable; the injected branch test reports that Preview is not called. Driver/preview failure also retains content and supplies recovery guidance. Preview/reload disable while busy. Closing preview reports that order/COD did not change. The separate real desktop SQL checkpoints support unchanged stored state around invoice read/preview in that QA workflow; screenshots alone do not establish this property. Source requests a 1280×800 preview with minimum 800×600, while the actual captured native dialog is larger as its manifest states.

## Do's and Don'ts

- **Do** extend AdminTheme/CatalogUi and preserve native tables, tabs, explicit status words and Vietnamese recovery copy.
- **Do** retain snapshot identity, separate COD/return truth, reasons and operation keys across errors or uncertainty.
- **Do** reconcile stale or uncertain outcomes and bind server results before enabling fresh writes or reporting completion.
- **Do** retain the server invoice notice in both screen and preview, and keep the no-printer branch recoverable.
- **Do** distinguish synthetic control proof, real desktop QA and SQL evidence in acceptance claims.
- **Don't** replace the native grammar with the private report image's web composition or root MVC radius/breakpoint rules.
- **Don't** infer financial correctness from fake captures or claim independent SQL reruns from this documentation scan.
- **Don't** reuse QA PNGs or private report imagery as production assets.

All PNGs are QA evidence with manifest provenance. No product rasters were generated or changed; no production-image prompt is owed. This pinned native extension owes no concept tournament, approved comp, comp-diff or HTML/CSS detector.

Pre-existing documentation drift is reported only: root PRODUCT/DESIGN and sidecar describe MVC, while native authority is AdminTheme and scoped W2–W4 documents. Earlier customer-order peek/deferred-handoff stage descriptions are historical; W5 now wires the production full-detail drilldown. No historical/root files were repaired.

The bounded official W5 scope has 16 criteria (8 P0 / 7 P1 / 1 P2); file10 A09.02/A12.01 also completes the full-order customer drilldown. The finish ship disposition applies to this extension and supplied local evidence. Remaining BASE/flows/UI/whole-phase gates, full DPI/device/accessibility coverage and cumulative acceptance remain deferred to W6. Unpublished Windows CI is not certified. This artifact closes the ordinary-extension documentation handoff without expanding those acceptance boundaries.
