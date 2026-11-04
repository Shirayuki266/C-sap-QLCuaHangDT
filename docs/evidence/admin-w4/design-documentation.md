---
name: Monophone Admin W4 native supply extension
description: Observed purchase drafts, protected stock-changing actions and authoritative stock readback within incumbent Option A.
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
  field-label:
    fontFamily: "Segoe UI"
    fontSize: "10pt"
    fontWeight: 700
spacing:
  list-inset: "16px"
  editor-inset: "20px"
  field-stack-inset: "24px"
  count-and-confirmation-inset: "28px"
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

W4 is an ordinary Operate-mode extension of the accepted Windows administration workbench. Vietnamese labels, Segoe UI, navy ink, blue commands, pale canvas, white tables and standard Windows inputs keep purchase and stock work within the W2/W3 grammar. Its organizing commitment is explicit stock-changing intention, retained recovery context and server-owned results.

This is a scoped scan-mode record under `.artifacts/admin-w4/direction.md`, following `reference/document.md` and the ordinary-extension rule in `reference/new-work.md`. No visual world or shared system change was introduced. Root `PRODUCT.md`, `DESIGN.md` and `.impeccable/design.json` remain preserved historical MVC records. This file records native tokens and behavior locally; it does not replace the root design or add an HTML/CSS preview sidecar.

Source checked: `src/PhoneStore.Admin/{AdminTheme,CatalogUi,ConfirmationDialog,PurchaseForms,StockForms,SupplyVariantPicker,ProductEditor}.cs` and `src/PhoneStore.Admin.Client/Supply.cs`. Incumbent comparison used W2/W3 design-documentation and directly opened `.artifacts/admin-w2/real-forms/UI08-real-qa-editor.png` and `.artifacts/admin-w3/real-forms/supplier-real-qa-list.png`. Current layouts, typography, button geometry and native table/tab treatment agree with those incumbents and AdminTheme.

This documentation pass directly opened these final W4 PNGs, each showing its named state without blank content or visible clipping:

- `forms/UI09-empty-draft.png`: persistent add-lines/save-first hint above the empty table; separate successful-save feedback remains below it.
- `forms/UI09-unsaved-confirm-hint.png`: retained edited lines and save-first hint while the saved server total remains identifiable.
- `forms/UI10-journal-loading.png`: existing rows remain visible with loading text and unavailable paging.
- `forms/UI09-picker-loading.png`: existing variants remain visible while search, selection and paging are unavailable.
- `real-forms/UI09-real-confirmed-stock-price.png`: confirmed purchase with server cost/price and H/R/available readback.
- `real-forms/UI10-real-count-saved.png`: server stock readback after count, retained reason and unavailable finished-operation inputs.

Both capture manifests and both finish reports were read. The final packet has **19 synthetic API PNGs** (`fakeApi: true`, `qaOnly: true`, seven check groups) and **10 real QA-backend PNGs** (`fakeApi: false`, `qaOnly: true`). Frames are declared 1280×800 at 120 DPI, with protected confirmations 620×330. The initial full review described the then-current 16 fake captures; three targeted fix captures account for the final 19. This documentation pass did not independently open every W4 capture or rerun the executables.

`finish-review.md` is the full review and originally returned **fix**. `finish-verdict.md` returns **ship only for the F1/F2 verdict pass**, resolving the disabled-confirm explanation and pending-pagination findings. It does not replace the full review or extend approval to the whole project. The full review and verdict remain the authority for their respective evidence and scope.

SQL/financial/concurrency proof is separate from screenshots. Synthetic API captures establish presentation and control state; real QA captures establish the displayed desktop workflow. They do not independently certify stored effects. `desktop-sql-results.json`, `sql-results.json` and `sql-states.json` are separately supplied backend evidence, discussed by the finish reviewer and not independently revalidated here.

## Colors

AdminTheme defines the five shared named colors above; paper is the explicitly used white surface. Blue carries primary commands, ink headings and ordinary copy, canvas the window ground, and paper tables and field stacks. Navy and muted remain incumbent shared tokens; this extension adds no palette. Business statuses use words such as “Nháp”, “Đã xác nhận” and “Đã hủy”. Stock quantities have labeled columns rather than color-only meaning.

Native selection blue, disabled text/background, gridlines, input borders and window chrome remain Windows-rendered. Their capture appearance is not a new custom token or a formal contrast acceptance result. A disabled primary button may retain its blue fill; its unavailable behavior follows native Enabled state, with recovery explained in adjacent text when confirmation is blocked by draft contents.

## Typography

Segoe UI supplies all observed roles. Forms inherit 10pt regular type; shared text inputs use 12pt; page headings use 22pt bold; the shared confirmation heading uses 18pt bold. Shared field-label calls use 10pt bold where specified. No custom line-height, tracking, webfont or raster typography is introduced. Shared labels auto-size with vertical margins of eight source units.

Amounts and times use the incumbent Vietnamese money formatter and UTC+7 time formatter. Journal/list time columns explicitly name UTC+7. Purchase totals and current cost/price are marked as server values; unsaved quantities do not silently replace the saved total. User-facing feedback uses recovery language rather than request transport or delivery-stage terminology.

## Layout

AdminTheme centers windows, sets default/minimum outer size to 1280×800 and uses `AutoScaleMode.Dpi`. Exact spacing and widths in this document describe WinForms source layout units, not a promise that every capture coordinate equals an unscaled source pixel. The supplied 120-DPI frame is a bounded acceptance sample; there are no web breakpoints.

Purchase list, stock list, journal and variant picker inherit the CatalogUi four-row structure: heading, wrapping toolbar, expanding grid, wrapping paging/status footer. Shared outer inset is 16 and toolbar inset 12. Purchase search is 300 wide with a 180-wide state selector. Stock search is 240 wide, brand selector 190 and explicit low-stock threshold 90. Picker search is 300 wide. Native tables retain useful minimum column widths and both scroll directions.

PurchaseEditor uses a 20-unit inset and five rows: supplier/metadata/confirmation hint, line toolbar, expanding native tabs, operation message and command toolbar. “Dòng nhập” permits quantity/cost editing; SKU/name stay read-only. “Tồn và giá từ máy chủ” is read-only. Line minima are SKU 160, name 280, quantity 140 and cost 180; impact columns are 170. The operation message remains separate from the top `ConfirmHintLabel`, so recovery guidance does not erase error, busy or reconciliation feedback.

The stock-count form uses a white top-docked scrollable stack with inset 28, heading, snapshot/projected-delta summary, absolute count, multiline reason, message and commands. Reason height is 110 source units. Protected confirmations use the shared 620×330 frame, inset 28 and a description constrained to 530 units wide. ProductEditor retains its existing tabs and intentionally horizontal-scrolling variant table, including local suggested quantities and server-owned stock/version columns.

## Elevation & Depth

Solid backgrounds, native table lines, tabs, focus/selection, standard field borders and window chrome establish grouping. There are no new custom shadows, gradients, textures, motion effects or production raster assets. Confirmation dialogs provide the incumbent modal consequence boundary.

## Shapes

Flat rectangular buttons accompany native rectangular fields, combo boxes, numeric controls, tabs and grids. AdminTheme buttons have minimum size 120×42 and padding 12 horizontal / 5 vertical. No custom corner-radius scale is defined. Grids have no outer border or row headers, select one full row, disable direct row addition/deletion and permit native scrolling. Purchase-line quantity/cost cells are editable only in an idle editable draft; stock, journal, impact and selection tables are read-only.

## Components

### Purchase search and draft editing

Purchase search supports query, textual status and 20-row paging. Open requires selection. Loading disables query/filter/grid/list commands and paging; empty and failure messages are distinct. A new purchase selects an active supplier and can contain zero to 100 distinct variant lines. Quantity is a positive integer; input cost is positive with at most two decimal places. The picker displays SKU/name/variant/current cost/business state and returns a variant choice; duplicate lines are rejected with guidance to edit the existing line.

Saving a draft reads the saved purchase back and binds code, status, creator, timestamp and authoritative total. Empty draft save is supported and does not confirm stock impact. Errors retain entered lines. Confirm is available only for a clean saved nonempty draft with the same supplier, while edit/pending/busy conditions control write availability. Confirmed/cancelled records cannot be edited as drafts.

**Persistent confirmation guidance.** `ConfirmHintLabel` sits above the lines. For an empty saved draft it says “Phiếu rỗng không thể xác nhận. Thêm dòng và lưu nháp trước.” For changed saved drafts it says “Có thay đổi chưa lưu. Lưu nháp trước khi xác nhận.” A clean saved draft has no incorrect warning. `Actions()` derives the hint separately from `MessageLabel`; it does not repurpose the operation result channel. This is the F1 resolution, not a new alert palette or component identity.

### Consequential commands and authoritative recovery

Confirmation describes the saved receipt, supplier, server total, total quantity/line count and stock/price consequences. Cancel explains that stock/prices remain unchanged and unsaved edits are discarded. The shared native dialog initially focuses “Quay lại”, supplies an explicit confirm action, and retains standard accept/cancel keyboard behavior. This source evidence is not a full keyboard or screen-reader audit.

Purchase creation retains one session request key and the pending creation payload. Purchase transitions retain a snapshot version and operation key. Ambiguous outcomes or failed readback disable further writes and expose “Tải lại / đối soát”. Reconciliation reads authoritative state before another command; it does not silently create a different intention. Confirmed purchase readback loads current stock and product prices into the impact tab. Ordinary reload asks before replacing changed loaded draft contents.

### Product-to-purchase handoff

ProductEditor enables purchase handoff for saved matching products with saved variant IDs and positive local suggested quantities. The typed handoff carries product/variant IDs, SKUs and quantities. Destination load fetches current product data for SKU/name/cost and creates local purchase lines. Opening or choosing the destination leaves a **new unsaved purchase**; explicit draft save is still required. Returning to ProductEditor reloads stock/prices from the server. Suggested quantities remain local UI input and do not directly change inventory.

### Stock query and absolute count

Stock search combines SKU/name/color query, brand and the explicit “Chỉ sắp hết, khả dụng ≤” threshold. The threshold is applied only when its checkbox is selected; user configuration stores the value with fallback 5. A local preference-save failure leaves the entered search threshold usable and explains that configuration was not saved. Read-only columns show H (“Tồn thực”), R (“Đang giữ”) and available (“Có thể bán”) from the server. Count and journal require selection and disable during loading.

Count starts from the displayed on-hand snapshot, accepts an absolute nonnegative actual count and requires a reason of at most 1000 characters. Client validation rejects counts below the snapshot reserved quantity and unchanged counts; confirmation explains that the server checks version and reserved stock again. The summary names SKU, current H/R, proposed absolute count and projected delta. This is a counted quantity, not a free delta field.

Count requests retain snapshot version, reason, counted value and operation key. Uncertain outcomes lock input and route to reconciliation. Recovery checks the movement journal by operation key and reads current stock: an applied journal blocks resubmission; no journal with unchanged version permits the same intention to retry; changed stock requires inspection and fresh confirmation. Finished counts remain unavailable for repeat submission while readback remains accessible. These are observed source workflow protections, not independent financial guarantees.

### Pending paging and journal

Journal rows show UTC+7 time, textual movement kind, delta H/R, document, actor and reason. Page size is 20. Journal and variant picker now guard both pager handlers with `!busy` **before mutating page**. Load disables both pagers; the picker also disables search/query/grid/choose. `finally` restores controls using the held current page/total, including the error path. Selection is restored only for an available loaded row. F2 targeted captures show the pending state; the verdict reports delayed total-45 interaction tests in which repeated navigation sends no extra request and successful release yields page 2. The held-read scenario tests successful release; error restoration is supported by source paths rather than a separately rerun error scenario in this pass.

## Do's and Don'ts

- **Do** extend AdminTheme and CatalogUi, preserving native tabs, table density, Vietnamese copy and explicit textual status.
- **Do** keep confirmation eligibility guidance visible beside the draft and independent from operation feedback.
- **Do** freeze pager mutation before an awaited read and restore availability from current page/total afterward.
- **Do** keep local suggestions and unsaved lines distinguishable from saved server totals and stock.
- **Do** retain keys, snapshots and input across uncertainty; reconcile before another write and bind authoritative results before reporting completion.
- **Do** keep synthetic control evidence, real QA desktop evidence and backend SQL proof separately attributed.
- **Don't** treat a selected supplier/variant or opened purchase handoff as a saved receipt or inventory mutation.
- **Don't** infer financial/concurrency guarantees from fake screenshots or extend F1/F2 ship to whole-surface acceptance.
- **Don't** transplant MVC web radii, typography or breakpoint rules into native WinForms controls.
- **Don't** reuse audit PNGs or private Word images as shipping product assets.

No product rasters were made or changed; these PNGs are generated native QA evidence with provenance in their manifests, not authored image assets requiring a production image-generation prompt. No HTML/CSS detector or comp-diff applies to this code-led native extension.

Pre-existing documentation drift is preserved: root PRODUCT/DESIGN and sidecar describe the MVC web foundation, while the active desktop design lives in AdminTheme and the scoped W2/W3/W4 documentation. Earlier W2 documentation describes the purchase handoff as deferred and W3 describes its future consumer; those are historical stage records, not current W4 behavior. No unrelated root or historical documentation was repaired.

Private UI09/UI10 Word imagery is unavailable for a fresh exact pixel comparison. Full W6 DPI/device/accessibility coverage is not claimed. File09 cross-stage criteria remain partial/NOT RUN pending their final stages; the optional referenced file10 tracker is unavailable with its user question pending. This file completes only the ordinary-extension documentation handoff and preserves all those acceptance boundaries.

Later source reconciliation (2026-11-02): file10 was supplied after this design review/documentation. The W4 report and acceptance ledger now reconcile all its24 represented IDs, holding A09.02 until W5 and reporting69 PASS/44 NOT RUN. The historical missing-file statement above records review-time context; no UI, design token, screenshot or SQL result changed.
