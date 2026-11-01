# W5 evidence provenance

W5 starts from accepted W4 develop `3dce8cec3eebcb60ea8a22ebbadde0c8cb3eb0ec`. Exact W5 commit and both CI results are bound in the PR verification comment. These are generated QA outputs, not production data or private Word/prototype files.

- `sql-results.json`:5 W5 real API/SQL scenario groups,48 cells (11 allowed/37 denied), independent literal section4 oracle, request methods/paths/sanitized command DTOs/statuses, race/retry/date/permission facts and before/after hashes. No authentication bodies, cookies or CSRF values.
- `sql-states.json`:68 full ordered snapshots keyed by SHA256; current order/lines/payment/history/journal, all stock/variant values and global commerce counts. Denied cells point to identical states. Credential/session tables are excluded.
- `desktop-sql-results.json`:9 parent-observed checkpoints while actual WinForms controls run through loopback Kestrel into another generated QA database. Includes complete/invoice-read/preview-closed identical data and correct H/R/COD/history/journal.
- `forms/results.json` +18 PNGs:actual STA controls with synthetic API,6 W5 groups/31 cumulative groups. Covers all6 states/return flag, reasons/busy/conflict, no-installed-printer and customer full-detail handoff. Proves controls, not money or transaction effects.
- `real-forms/results.json` +16 PNGs:actual QA-backed native list/order states/history/invoice/confirmations and installed-driver native PrintPreview. Uses an existing local driver without printing. Main surfaces1280×800/120DPI, confirmations620×330, native PrintPreview1886×1266 per manifests. Full W6 DPI/device/a11y acceptance is not claimed.
- `*-summary.log`:local executed checks, including unchanged322 regression,51 total Client groups and31 form groups. Ubuntu CI independently runs SQL/old regression; Windows CI runs fake form controls. Real installed-driver preview is local smoke, not CI proof of physical printing.
- `direction.md`, `finish-review.md`, `design-documentation.md`:pinned native incumbent extension, fresh independent full ship review of all34 images, ordinary-extension system comparison. Root historical MVC design artifacts are preserved. No product raster is generated or shipped.

The [W5 report](../../WINFORMS_W5.md) maps every ID and limitation. Remaining26 NOT RUN rows stay W6/cumulative final review; there is no whole-phase gate approval. No default credential or real connection string is included.
