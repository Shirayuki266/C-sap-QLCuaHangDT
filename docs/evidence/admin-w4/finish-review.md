# W4 independent full finish review

## Persistence

The direction is persisted in `.artifacts/admin-w4/direction.md`: Operate mode, ordinary native extension, pinned Option A, 1280×800 minimum, incumbent AdminTheme authority. No new identity, approved comp or production raster is owed. Historical root MVC PRODUCT/DESIGN must remain untouched; ordinary-extension documentation is still a subsequent handoff, not certified by this review. The contract's evidence count should be updated from 15 to 16 fake captures when documentation is finalized.

This is a full review after the earlier recapture, which bound nothing. All 26 W4 PNGs (16 fake-control, 10 real-backend) were opened individually using `view_image`, both manifests were read, and the final two horizontal-scroll replacements were reopened after replacement. Captures are valid: no blank regions, wrong tabs or incomplete confirmation descriptions remain. The fake source quantities 7/1 and real source quantity 4 are visible; separate destination captures expose the unsaved prefilled purchases. Frames declare 120 DPI, 1280×800, with protected confirmations 620×330.

## Fidelity

Pass at the incumbent-extension scope. Segoe UI, navy text, blue primary actions, pale canvas, white grids, native tabs/inputs and protected confirmation dialogs match AdminTheme and the W2/W3 incumbent evidence. W2 `UI08-real-qa-editor.png` and W3 `supplier-real-qa-list.png` were opened for comparison. Purchase/stock surfaces preserve the operating grammar rather than introducing a separate identity. Tables, command rows and metadata fit the tested minimum frame; ProductEditor intentionally uses horizontal scrolling for its existing wide variant table.

This is code-led work with no approved comp and no applicable comp-diff. No HTML/CSS detector applies to C# WinForms. UI09/UI10 private Word imagery is unavailable, so no fresh exact Word-image comparison is claimed. These limits are explicit rather than inferred passes.

## Craft

Two material findings require one batch. No cosmetic redesign or additional polish is requested.

| ID | Priority | Finding and evidence | Required result |
|---|---|---|---|
| F1 | Material | A saved empty draft shows a disabled **Xác nhận phiếu** and only the success message “Đã lưu nháp. Kho và giá chưa thay đổi.” (`forms/UI09-empty-draft.png`). `PurchaseEditor.Actions()` disables confirmation, while the useful empty/unsaved explanation exists only inside `ActAsync()`, unreachable through the disabled button. A real user cannot read why confirmation is unavailable or the required recovery. Edited drafts have the same undiscoverable save-first condition. | Add a visible persistent explanation for an empty saved draft and for unsaved draft changes, e.g. add lines and save before confirmation / save changes first. Keep confirmation disabled and preserve errors, busy and reconciliation status instead of replacing them with this hint. Verify empty, edited and clean saved draft states. |
| F2 | Material | `StockJournalForm.LoadAsync()` and `SupplyVariantPicker.LoadAsync()` set `busy` without disabling or guarding pagination before the handlers mutate shared `page`. Once multiple pages exist, repeated Next/Previous during an await can change `page` while the guarded second load returns immediately. Rows requested for page 2 can therefore be shown with a page 3 footer, or navigation can skip pages. Purchase and stock list forms already prevent this through `Actions()`. | Guard or disable both pager controls before any page mutation while loading in the journal and variant picker; restore availability from authoritative current page/total afterward. Verify with a delayed multi-page response and repeated pager activation, including error recovery. |

Otherwise, the viewed states have readable hierarchy and practical density, sufficient whitespace between groups, no decorative scaffolding, and clear server-authoritative totals/H/R/available. Exact fake and real confirmation copy fits its dialog, with quantity/total or absolute count/delta/reason/consequences. Native dismissal is initially focused and confirmation is explicit. Stock failure remains distinguishable from empty stock; list actions disable without a selected row. The purchase-error capture retains the changed quantity 8 and public server message. Error-message recovery, uncertainty locks and readback paths are present in source. A full keyboard/screen-reader/high-DPI/device sweep has not been performed here and belongs to W6.

## Completeness

The reviewed W4 scope is the official 17 IDs (9 P0 / 8 P1), limited to Purchase/Stock UI and the ProductEditor purchase handoff. Source inspected: PurchaseForms, StockForms, SupplyVariantPicker, purchase-related ProductEditor, AdminTheme, shared CatalogUi/ConfirmationDialog and Admin.Client/Supply. No backend files were edited or certified by this reviewer.

Purchase list query/status/paging, supplier selection, multiline draft editing, empty save/cancel, server metadata/total, protected confirmation and post-confirmation cost/price/stock tabs are present. Stock query/brand/explicit low-stock threshold, read-only H/R/available, paged journal and absolute-count/reason dialog are present. Source preserves a creation requestKey and pending payload, transition/count operationKey and version, blocks further writes during uncertain readback, and reconciles counts through the movement key rather than blind resubmission. Suggested product quantities remain local until explicit draft save; handoff destination is a new unsaved purchase.

Evidence separation is respected: fake screenshots and six fake scenario groups prove controls/states. `forms.log` reports 24 total form checks; `client.log` reports 13 W4 client checks. Real images plus `desktop-sql-results.json` support the QA smoke sequence: product/handoff purchases 0, H 0; draft purchases 1, H 0, journal 0; confirmation H 4, journal 1; count H 5, journal 2. Orders/payments stay 0 in those desktop snapshots. Separate `sql-results.json` and `sql-states.json` hold the financial/concurrency proof: 13 checks, 18 snapshots; the recorded race has two arrivals on distinct connections, SUCCESS/CONCURRENT_UPDATE, stock 10/5 and two receipt journal rows. This review reads those artifacts; it does not independently rerun or extend their financial guarantees. A reported 322 regression checks is supporting build evidence, not a fresh reviewer-run result.

Cumulative acceptance remains bounded: file 09 cross-stage rows remain partial/NOT RUN until their final stage. Referenced file 10 is unavailable and its question remains pending. This review cannot certify that missing tracker, unavailable Word comparison, all DPI/device classes, all overlong-content cases or W6 accessibility. No screenshot of SupplyVariantPicker states is supplied; its paging finding is established from source and needs targeted interaction verification. Do not present W4 visual approval as completion of later stages.

## Verdict

Disposition: **fix**.

Apply F1 and F2 in one batch, rebuild once, recapture the same evidence files/viewports affected by those changes, and return to this reviewer for a verdict scoring F1/F2 resolved, partial or unresolved. Preserve fake/real provenance and the SQL evidence boundary. The rest of this reviewed native desktop surface meets the incumbent direction at the supplied 120 DPI/minimum-size scope; this is not a whole-project or W6 approval. Only this review file was written by the reviewer.
