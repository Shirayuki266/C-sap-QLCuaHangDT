# Draft DATA/CLEAN ledger

Snapshot of executed product/tool subject `842e0dfb33f19b36a96a0ad40f2f3c3d08a695e0`; final exact-head CI and any changed status are recorded in draft PR #122. This is not release approval. The machine ledger has exactly76 IDs; pending rows remain NOT RUN.

| Priority | PASS | FAIL | BLOCKED | NOT RUN | ACCEPTED_EXCEPTION |
|---|---:|---:|---:|---:|---:|
| P0 | 37 | 0 | 0 | 6 | 0 |
| P1 | 26 | 0 | 0 | 4 | 1 |
| P2 | 2 | 0 | 0 | 0 | 0 |

| ID | Priority | Status | Evidence / reason |
|---|---|---|---|
| DATA.SAFE.01 | P0 | NOT RUN | Await exact latest-head CI for safety/refusal tests; no repeated local regression (small) |
| DATA.SAFE.02 | P0 | NOT RUN | Await exact latest-head CI for safety/refusal tests; no repeated local regression (small) |
| DATA.SAFE.03 | P0 | NOT RUN | Await exact latest-head CI for safety/refusal tests; no repeated local regression (small) |
| DATA.SAFE.04 | P0 | NOT RUN | Await exact latest-head CI for safety/refusal tests; no repeated local regression (small) |
| DATA.SAFE.05 | P1 | PASS | Approved reduced scope: identical seed/as-of small manifests; full generated once, no new full rerun (small, full) |
| DATA.SAFE.06 | P1 | PASS | Small and full both seeded and independently SQL-verified (small, full) |
| DATA.SCALE.01 | P0 | PASS | Executed full SQL verifier and foundation checks: Số dòng của từng bảng nằm trong khoảng mục tiêu ở mục 2 của tài liệu này. (full, sql) |
| DATA.SCALE.02 | P1 | PASS | Executed full SQL verifier and foundation checks: Cả 19 bảng đều có dữ liệu hợp lý (bảng `PHIEN` có thể 0; sổ migration 1 dòng). (full, sql) |
| DATA.TIME.01 | P0 | PASS | Executed full SQL verifier and foundation checks: Có đủ 8 quý đầy đủ cộng quý hiện tại đến ngày `--as-of`; số đơn mỗi quý đúng mục tiêu ±15%. (full, sql) |
| DATA.TIME.02 | P1 | PASS | Executed full SQL verifier and foundation checks: Có mùa vụ: quý 4 > quý 2; tháng 11–12 và đợt trước Tết nổi bật; năm sau tăng trưởng so với năm trước. (full, sql) |
| DATA.TIME.03 | P0 | PASS | Executed full SQL verifier and foundation checks: Có ít nhất 20 đơn đặt trong 30 ngày gần nhất và ít nhất 8 sản phẩm khác nhau có đơn `HOAN_TAT`/`DA_THU` với thời điểm hoàn tất trong tháng hiện tại (UTC+7, kể cả đơn đặt cuối tháng trước), để mục "bán chạy" ở trang chủ (DEC.07, tính theo hoàn tất, không lấy kỳ trước) không rỗng. Nếu `--as-of` rơi vào vài ngày đầu tháng, công cụ điều chỉnh thời điểm hoàn tất của các đơn cuối tháng trước để điều kiện vẫn đạt. (full, sql) |
| DATA.TIME.04 | P0 | PASS | Executed full SQL verifier and foundation checks: Mọi mốc thời gian lưu UTC, không có mốc tương lai, thứ tự hợp lý: sản phẩm tạo trước nhập hàng, nhập hàng trước bán; khách tạo tài khoản trước đơn đầu tiên; tài khoản ADMIN tạo trước phiếu nhập đầu tiên. (full, sql) |
| DATA.CATALOG.01 | P0 | PASS | Real catalog SQL constraints and licensed offline photographs/source-backed original prose; approved simulated price bands (sql, image, imageReview, credits, facts, prices) |
| DATA.CATALOG.02 | P0 | PASS | Real catalog SQL constraints and licensed offline photographs/source-backed original prose; approved simulated price bands (sql, image, imageReview, credits, facts, prices) |
| DATA.CATALOG.03 | P0 | PASS | Real catalog SQL constraints and licensed offline photographs/source-backed original prose; approved simulated price bands (sql, image, imageReview, credits, facts, prices) |
| DATA.CATALOG.04 | P0 | PASS | Executed full SQL verifier and foundation checks: Mọi biến thể thỏa `Price = Round(Cost × (1 + ProfitPercent/100), 2, AwayFromZero)`, `Cost > 0`, `ProfitPercent` trong 0–10.000. (full, sql) |
| DATA.CATALOG.05 | P1 | PASS | Real catalog SQL constraints and licensed offline photographs/source-backed original prose; approved simulated price bands (sql, image, imageReview, credits, facts, prices) |
| DATA.CATALOG.06 | P1 | PASS | Executed full SQL verifier and foundation checks: Có vòng đời sản phẩm: khoảng 24 sản phẩm có từ đầu, phần còn lại ra mắt dần; 4 sản phẩm ngừng kinh doanh nhưng giữ lịch sử; model cũ giảm giá vốn theo thời gian. (full, sql) |
| DATA.CATALOG.07 | P1 | PASS | Executed full SQL verifier and foundation checks: Tồn cuối kỳ đa dạng: phần lớn 5–40; ít nhất 8 biến thể sắp hết (≤ 5) và 3 biến thể hết hàng; không có tồn âm. (full, sql) |
| DATA.CUST.01 | P0 | PASS | Executed full SQL verifier and foundation checks: 60 khách hàng cùng tài khoản `CUSTOMER` tương ứng: username viết thường duy nhất, điện thoại duy nhất, email `@example.com` hoặc NULL (không chuỗi rỗng), mật khẩu băm từ biến môi trường, không có dữ liệu cá nhân thật. (full, sql) |
| DATA.CUST.02 | P1 | PASS | Executed full SQL verifier and foundation checks: Ngày tạo tài khoản rải đều 18–24 tháng và tăng dần; 4 tài khoản bị khóa; 2 khách chưa có đơn nào. (full, sql) |
| DATA.CUST.03 | P1 | PASS | Executed full SQL verifier and foundation checks: Số đơn theo khách phân bố lệch (có khách mua lặp 5–10 lần); địa chỉ đa dạng tỉnh/thành, số nhà là hư cấu. (full, sql) |
| DATA.PURCH.01 | P0 | PASS | Executed full SQL verifier and foundation checks: Ít nhất 56 phiếu nhập từ 12 nhà cung cấp, rải đều các quý; đa số đã xác nhận; có phiếu nháp (kể cả 1 phiếu rỗng) và phiếu đã hủy. (full, sql) |
| DATA.PURCH.02 | P0 | PASS | Executed full SQL verifier and foundation checks: Mỗi phiếu đã xác nhận: tổng = Σ dòng; tồn tăng đúng; `Cost` biến thể bằng giá vốn của phiếu xác nhận gần nhất; có người và thời điểm xác nhận. Phiếu nháp và phiếu hủy không ảnh hưởng tồn hay giá. (full, sql) |
| DATA.PURCH.03 | P1 | PASS | Executed full SQL verifier and foundation checks: Nhà cung cấp: 12, trong đó một vài nhà đã ngừng hoạt động nhưng còn lịch sử. (full, sql) |
| DATA.ORDER.01 | P0 | PASS | Executed full SQL verifier and foundation checks: Khoảng 600 đơn (±10%), 1–3 dòng mỗi đơn (khoảng 800 dòng); `Total = Σ(số lượng × giá snapshot)`; giá snapshot bằng giá hiệu lực tại thời điểm đặt. (full, sql) |
| DATA.ORDER.02 | P0 | PASS | Executed full SQL verifier and foundation checks: Tỷ lệ trạng thái hợp lý: `HOAN_TAT` ~85%, `DA_HUY` ~8%, `GIAO_THAT_BAI` ~4% (đa số đã nhận lại hàng); đơn đang xử lý chỉ ở vài ngày gần nhất. (full, sql) |
| DATA.ORDER.03 | P0 | PASS | Executed full SQL verifier and foundation checks: Mỗi đơn đúng 1 dòng thanh toán COD; `DA_THU` khi và chỉ khi `HOAN_TAT`; `Amount = Total` khi đã thu, 0 khi chưa thu; `CollectedAt = CompletedAt`. (full, sql) |
| DATA.ORDER.04 | P0 | PASS | Executed full SQL verifier and foundation checks: Lịch sử đơn tuân theo ma trận trạng thái: chuyển hợp lệ, thời điểm tăng dần, trạng thái cuối khớp trạng thái hiện tại; người xử lý hợp lệ. (full, sql) |
| DATA.ORDER.05 | P0 | PASS | Executed full SQL verifier and foundation checks: Tồn kho khớp nhật ký: `OnHand = Σ ΔOnHand`, `Reserved = Σ ΔReserved`; `Reserved` bằng tổng số lượng các đơn đang ở `CHO_XAC_NHAN`/`DA_XAC_NHAN`; `OnHand ≥ Reserved ≥ 0`. (full, sql) |
| DATA.ORDER.06 | P1 | PASS | Executed full SQL verifier and foundation checks: Có ít nhất 6–10 lần kiểm kê (điều chỉnh tồn) kèm lý do trong 2 năm. (full, sql) |
| DATA.ORDER.07 | P1 | PASS | Executed full SQL verifier and foundation checks: Giờ và ngày đặt hàng phân bố hợp lý; đỉnh ở đầu tháng, 11/11, 12/12, Black Friday và trước Tết. (full, sql) |
| DATA.ORDER.08 | P1 | PASS | Executed full SQL verifier and foundation checks: Khoảng 25 dòng giỏ hàng của khoảng 12 khách (giỏ đang hoạt động, cập nhật gần đây); không có phiên đăng nhập còn hạn. (full, sql) |
| DATA.VERIFY.01 | P0 | PASS | Full SQL verifier PASS; one restored-small-QA stock mutation made verifier exit1, exact reversal passed (sql, negative, cleanup) |
| DATA.VERIFY.02 | P0 | PASS | Existing --audit-db passes after real application reads (audit) |
| DATA.VERIFY.03 | P0 | PASS | Real ADMIN login/paging/nine SQL-matched quarter revenues, bestsellers, stock, invoice; MVC real customer/history/catalog/photos (http, mvc) |
| DATA.VERIFY.04 | P1 | NOT RUN | chờ kiểm tra thủ công bởi người dùng — one STA attempt passed dashboard/reports and historical forms, but comprehensive native navigation/journal/search acceptance is not established; do not retry (sta, mvc) |
| DATA.VERIFY.05 | P1 | PASS | Measured actual HTTP, MVC and STA form/report times; measurements retained, no substituted synthetic API (http, mvc, sta) |
| DATA.VERIFY.06 | P1 | NOT RUN | Await actual CI SUCCESS on final exact PR head; initial842 CI failed on obsolete RL-only assertion, corrected only in test (exception) |
| DATA.DOC.01 | P1 | PASS | Runbook and sanitized summary describe environment names, scale, limitations and evidence scope (docs, credits) |
| DATA.DOC.02 | P1 | PASS | Runbook and sanitized summary describe environment names, scale, limitations and evidence scope (docs, credits) |
| DATA.GATE.01 | P0 | NOT RUN | Draft remains open: manual P0 CLEAN.REPORT.03 and exact latest-head CI acceptance are not complete; no release/tag authorization (exception, sta) |
| DATA.GATE.02 | P1 | PASS | Exact76 IDs and original priority labels; PASS/NOT RUN/exception counted separately (exception) |
| CLEAN.METHOD.01 | P0 | PASS | Fresh tool-owned real full demo, trusted frozen denylist, verified pre/post backups and restored-small-QA negative/cleanup proof (full, negative, backups, exception) |
| CLEAN.METHOD.02 | P0 | PASS | Fresh tool-owned real full demo, trusted frozen denylist, verified pre/post backups and restored-small-QA negative/cleanup proof (full, negative, backups, exception) |
| CLEAN.METHOD.03 | P0 | PASS | Fresh tool-owned real full demo, trusted frozen denylist, verified pre/post backups and restored-small-QA negative/cleanup proof (full, negative, backups, exception) |
| CLEAN.DB.01 | P0 | PASS | All19 tables/all40 string columns scanned with571 frozen hard terms: zero hits; snapshot name/SKU mismatches0; intentional shared /images/demo/ path documented (denyScan, sql) |
| CLEAN.DB.02 | P0 | PASS | All19 tables/all40 string columns scanned with571 frozen hard terms: zero hits; snapshot name/SKU mismatches0; intentional shared /images/demo/ path documented (denyScan, sql) |
| CLEAN.DB.03 | P0 | PASS | All19 tables/all40 string columns scanned with571 frozen hard terms: zero hits; snapshot name/SKU mismatches0; intentional shared /images/demo/ path documented (denyScan, sql) |
| CLEAN.DB.04 | P0 | PASS | All19 tables/all40 string columns scanned with571 frozen hard terms: zero hits; snapshot name/SKU mismatches0; intentional shared /images/demo/ path documented (denyScan, sql) |
| CLEAN.DB.05 | P0 | PASS | All19 tables/all40 string columns scanned with571 frozen hard terms: zero hits; snapshot name/SKU mismatches0; intentional shared /images/demo/ path documented (denyScan, sql) |
| CLEAN.DB.06 | P0 | PASS | All19 tables/all40 string columns scanned with571 frozen hard terms: zero hits; snapshot name/SKU mismatches0; intentional shared /images/demo/ path documented (denyScan, sql) |
| CLEAN.DB.07 | P1 | PASS | All19 tables/all40 string columns scanned with571 frozen hard terms: zero hits; snapshot name/SKU mismatches0; intentional shared /images/demo/ path documented (denyScan, sql) |
| CLEAN.DB.08 | P1 | PASS | All19 tables/all40 string columns scanned with571 frozen hard terms: zero hits; snapshot name/SKU mismatches0; intentional shared /images/demo/ path documented (denyScan, sql) |
| CLEAN.DB.09 | P0 | PASS | All19 tables/all40 string columns scanned with571 frozen hard terms: zero hits; snapshot name/SKU mismatches0; intentional shared /images/demo/ path documented (denyScan, sql) |
| CLEAN.DB.10 | P0 | PASS | All19 tables/all40 string columns scanned with571 frozen hard terms: zero hits; snapshot name/SKU mismatches0; intentional shared /images/demo/ path documented (denyScan, sql) |
| CLEAN.REPORT.01 | P0 | PASS | Nine actual API quarter reports match SQL; real labels in bestsellers/stock; regenerated dataset matches plan (http, sql) |
| CLEAN.REPORT.02 | P0 | PASS | Actual MVC covers all46 active products, photos/descriptions/specifications, cart, GET checkout and customer old orders across six quarters; no order placed (mvc, http) |
| CLEAN.REPORT.03 | P0 | NOT RUN | chờ kiểm tra thủ công bởi người dùng — STA real API proved nine quarter reports, eighteen old orders/invoices, four receipts and low/zero stock; stock-journal navigation was not exercised in the single attempt (sta, mvc) |
| CLEAN.REPORT.04 | P1 | PASS | Single STA attempt: eighteen old invoice forms across nine quarters and actual PrintPreview captured; no fake printer (sta) |
| CLEAN.REPORT.05 | P1 | NOT RUN | chờ kiểm tra thủ công bởi người dùng — MVC positive/negative search passed; native product search was not exercised in the single STA attempt (sta, mvc) |
| CLEAN.FILES.01 | P0 | PASS | Current tracked files inspected; archived screenshots explicitly obsolete, denylist/assertions explained; frozen licensed asset checks and all150 HTTP200 (files, credits, history, http) |
| CLEAN.FILES.02 | P0 | PASS | Current tracked files inspected; archived screenshots explicitly obsolete, denylist/assertions explained; frozen licensed asset checks and all150 HTTP200 (files, credits, history, http) |
| CLEAN.FILES.03 | P1 | PASS | Current tracked files inspected; archived screenshots explicitly obsolete, denylist/assertions explained; frozen licensed asset checks and all150 HTTP200 (files, credits, history, http) |
| CLEAN.FILES.04 | P1 | ACCEPTED_EXCEPTION | User approved preserving unchanged Infrastructure legacy DemoSeeder and demo-phone.svg to honor runtime freeze; separate current demo tool is real-only (exception) |
| CLEAN.FILES.05 | P1 | PASS | Demo/QA backup basenames and workspace .bak inventoried; old backups are historical, not current evidence. New verified licensed backup and restore/cleanup proven; no backups in Git (backups, negative, cleanup) |
| CLEAN.INTEG.01 | P0 | PASS | Full independent SQL/foundation verifier and existing audit pass; hashes unchanged after read-only apps; approved simulation price policy (sql, audit, prices) |
| CLEAN.INTEG.02 | P0 | PASS | Full independent SQL/foundation verifier and existing audit pass; hashes unchanged after read-only apps; approved simulation price policy (sql, audit, prices) |
| CLEAN.INTEG.03 | P0 | PASS | Full independent SQL/foundation verifier and existing audit pass; hashes unchanged after read-only apps; approved simulation price policy (sql, audit, prices) |
| CLEAN.INTEG.04 | P1 | PASS | Full independent SQL/foundation verifier and existing audit pass; hashes unchanged after read-only apps; approved simulation price policy (sql, audit, prices) |
| CLEAN.INTEG.05 | P1 | NOT RUN | Await actual CI SUCCESS on final exact PR head; initial842 CI failed on obsolete RL-only assertion, corrected only in test (exception) |
| CLEAN.REAL.01 | P1 | PASS | Per-model manufacturer sources, original Vietnamese prose, per-image author/license/version/crop and approved dated simulated price bands (facts, credits, imageReview, prices) |
| CLEAN.REAL.02 | P1 | PASS | Per-model manufacturer sources, original Vietnamese prose, per-image author/license/version/crop and approved dated simulated price bands (facts, credits, imageReview, prices) |
| CLEAN.REAL.03 | P1 | PASS | Per-model manufacturer sources, original Vietnamese prose, per-image author/license/version/crop and approved dated simulated price bands (facts, credits, imageReview, prices) |
| CLEAN.REAL.04 | P2 | PASS | Per-model manufacturer sources, original Vietnamese prose, per-image author/license/version/crop and approved dated simulated price bands (facts, credits, imageReview, prices) |
| CLEAN.REAL.05 | P2 | PASS | Official DATA.CATALOG.01–03 explicitly updated for approved real catalog; historical instructions identified as context (exception, facts) |

## Exactly seven manual screen groups

The single STA execution passed its recorded assertions, but it did not exercise the complete native navigation/journal/search scope. Retain the requested native acceptance rows as NOT RUN: **chờ kiểm tra thủ công bởi người dùng**. Do not run another automated attempt.

1. **Tổng quan:** verify totals against the full dataset and charts.
2. **Điện thoại:** search Samsung (results), then Auvora/Lumetrix/DM001 (zero results); inspect real model labels.
3. **Đơn hàng cũ:** page1/page2 and details/history of historical orders across quarters.
4. **Hóa đơn:** historical completed orders across quarters, snapshot names/totals and PrintPreview.
5. **Phiếu nhập cũ:** open confirmed historical receipts; inspect real names/SKU/cost.
6. **Tồn kho / Nhật ký kho:** low and zero variants; open movement history and check real names/actors.
7. **Báo cáo:** revenue for2024Q4 through2026Q4 (nine distinct nonzero totals), bestsellers and stock.

The ordinary STA runner prints its default regression counter after this explicit mode; that counter is not the demo-acceptance assertion count. Use `sta-results.json` (one real-API attempt and recorded observations), not that generic counter.

CLEAN.FILES.04 is ACCEPTED_EXCEPTION, never PASS. No M1/M2, tag, main release or merge is authorized.
