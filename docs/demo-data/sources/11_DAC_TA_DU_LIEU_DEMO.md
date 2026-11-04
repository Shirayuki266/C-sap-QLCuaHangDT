 > **Phương án hiệu lực từ2026-11-04:** người dùng đã đổi sang sản phẩm/ảnh thật. Các phần mô tả phương án hư cấu bên dưới là lịch sử. DATA.CATALOG.01–03 được cập nhật theo quyết định này; ngoại lệ runtime seeder cũ và điều kiện đóng sổ ở [RELEASE_CLOSEOUT.md](../RELEASE_CLOSEOUT.md).

# Đặc tả dữ liệu demo đầy đủ (khoảng 2 năm bán hàng)

Tài liệu này mô tả bộ dữ liệu demo giống một cửa hàng điện thoại đã hoạt động 1–2 năm: nhiều sản phẩm có ảnh và mô tả, khách hàng, nhập hàng, đơn hàng với số lượng bán khác nhau theo từng quý. Đi kèm là checklist `DATA.*` để nghiệm thu.

## 1. Nguyên tắc

- **Dữ liệu phải đúng nghiệp vụ, không chỉ đủ số dòng.** Tồn kho phải khớp nhật ký, tổng đơn khớp dòng, COD khớp trạng thái, giá khớp công thức. Dữ liệu sai bất biến sẽ làm lộ lỗi ngay khi giảng viên mở báo cáo.
- **Quy mô theo bản chất từng bảng.** Không cần mọi bảng đều 50 dòng: bảng thực thể chính (khách hàng, sản phẩm, phiếu nhập) cỡ 50–60; bảng danh mục nhỏ (hãng, danh mục) ít hơn; bảng giao dịch và chi tiết (đơn, dòng đơn, nhật ký kho) nhiều hơn vì chúng tích lũy theo thời gian.
- **Không dùng dữ liệu cá nhân hay thương hiệu thật.** Tên người, địa chỉ nhà, điện thoại và email là hư cấu (email dùng miền `example.com`). Ảnh là SVG sinh tự động, không dùng logo hay ảnh sản phẩm thật. Mô tả viết mới bằng tiếng Việt, không chép từ trang bán hàng.
- **Chỉ chạy trên CSDL demo riêng** (`PhoneStore_Demo_...`), qua lệnh chủ động, mật khẩu lấy từ biến môi trường. Không bao giờ chạy trên `PhoneStore_Rebuild` hay CSDL thật.
- **Tái lập được:** cùng `--seed` và `--as-of` cho cùng dữ liệu; chạy lại ngay trước buổi demo để tháng hiện tại luôn có đơn mới.

**Quyết định về tên hãng/model (bạn chọn một trước khi giao AI):**

| Phương án | Nội dung | Ưu / nhược |
| --- | --- | --- |
| **A (mặc định, khuyến nghị)** | 10 hãng và 50 model hư cấu (ví dụ "Aurora X1 Pro") | Không vướng nhãn hiệu, không sai thông số của máy thật; vẫn trông như cửa hàng thật |
| B | Tên hãng thật, model hư cấu hoặc gần giống, thông số ghi rõ "dữ liệu minh họa" | Quen mắt hơn, nhưng dễ bị hỏi về sự chính xác của thông số |

## 2. Quy mô từng bảng (19 bảng)

| Bảng | Mục tiêu | Khoảng chấp nhận | Ghi chú |
| --- | ---: | --- | --- |
| TAI_KHOAN | 61 | 60–62 | 60 tài khoản CUSTOMER và 1 ADMIN (cấp bằng công cụ cấp quyền hiện có, mật khẩu từ biến môi trường) |
| KHACH_HANG | 60 | 50–70 | 4 khách bị khóa; 2 khách chưa có đơn; email có thể NULL |
| PHIEN | 0 | 0–3 | Không seed phiên còn hạn; phiên sinh ra khi đăng nhập thật |
| HANG | 10 | 8–12 | Hãng hoạt động; 1 hãng có thể ngừng để minh họa |
| DANH_MUC | 6 | 5–8 | Phân khúc: giá rẻ, tầm trung, cao cấp, gaming, màn hình gập, ... |
| SAN_PHAM | 50 | 45–55 | Mỗi sản phẩm có `ImageUrl`, `Description`, `Specifications`; 4 sản phẩm ngừng kinh doanh |
| SAN_PHAM_ANH | 150 | 100–180 | Khoảng 3 ảnh mỗi sản phẩm, có `SortOrder` |
| BIEN_THE | 125 | 110–140 | 2–3 biến thể mỗi sản phẩm (dung lượng và màu) |
| TON_KHO | = số biến thể | — | Mỗi biến thể một dòng tồn |
| GIO_HANG_CT | 25 | 20–30 | Giỏ đang hoạt động của khoảng 12 khách |
| NHA_CUNG_CAP | 12 | 10–15 | Một vài nhà ngừng hoạt động |
| PHIEU_NHAP | 56 | 50–62 | Đa số đã xác nhận; có nháp (1 rỗng) và đã hủy |
| PHIEU_NHAP_CT | 250 | 200–320 | 3–6 dòng mỗi phiếu |
| DON_HANG | 600 | 540–660 | Phân bố theo quý ở mục 3 |
| DON_HANG_CT | 810 | 700–950 | 1–3 dòng mỗi đơn, đa số 1 dòng |
| THANH_TOAN | = số đơn | — | Đúng 1 dòng COD mỗi đơn |
| LICH_SU_DON | 2000 | 1600–2400 | 2–5 mục mỗi đơn theo chuyển trạng thái |
| NHAT_KY_KHO | 1900 | 1500–2600 | Nhập hàng, giữ, nhả, xuất giao, nhận lại, kiểm kê |
| __PhoneStoreMigrations | 1 | 1 | Sổ migration, không do công cụ tạo |

## 3. Dòng thời gian và mùa vụ

Mốc: từ ngày đầu của quý cách quý hiện tại 8 quý, đến ngày `--as-of` (mặc định hôm nay theo UTC+7). Ví dụ với ngày 02/11/2026: từ 01/10/2024 đến 02/11/2026. Số đơn mục tiêu mỗi quý (±15%):

| Quý | Đặc điểm | Đơn mục tiêu |
| --- | --- | ---: |
| Q4/2024 | Khởi đầu, cuối năm | 70 |
| Q1/2025 | Tết, mua sắm đầu năm | 62 |
| Q2/2025 | Thấp điểm | 58 |
| Q3/2025 | Khai giảng, ra mắt model mới | 66 |
| Q4/2025 | Cao điểm 11/11, 12/12, Black Friday | 84 |
| Q1/2026 | Tết, tăng trưởng so với năm trước | 76 |
| Q2/2026 | Thấp điểm | 70 |
| Q3/2026 | Khai giảng, model mới | 80 |
| Q4/2026 (đến ngày as-of) | Quý đang chạy, đến hết `--as-of` | 35 (≥ 20 đơn trong 30 ngày gần nhất) |

Tổng khoảng 600 đơn. Doanh thu mỗi quý chênh nhau rõ rệt (mùa vụ cộng tăng trưởng ~20% năm sau so với năm trước); đơn giá trung bình 8–12 triệu đồng, đa số đơn 1 dòng.

## 4. Danh mục sản phẩm

- **Phân khúc (50 sản phẩm):** giá rẻ 14, tầm trung 16, cao cấp 12, gaming 4, màn hình gập 4. Giá vốn gợi ý: giá rẻ 2,5–5,5 triệu (lợi nhuận 12–18%); tầm trung 5–10 triệu (10–15%); cao cấp 14–30 triệu (8–12%); gaming 12–22 triệu; màn hình gập 20–35 triệu (8–10%).
- **Biến thể:** dung lượng 64/128/256/512 GB, 3–4 màu; mỗi sản phẩm 2–3 biến thể (khoảng 125 biến thể). Giá bán luôn tính theo công thức `Price = Round(Cost × (1 + ProfitPercent/100), 2, AwayFromZero)`.
- **Vòng đời:** khoảng 24 sản phẩm có từ đầu, 26 sản phẩm ra mắt dần theo thời gian; model cũ giảm giá vốn 3–6% mỗi quý; 4 sản phẩm ngừng kinh doanh nhưng vẫn giữ lịch sử đơn và nhập. Schema không có ngày tạo/ra mắt sản phẩm nên lịch ra mắt nằm trong kế hoạch của công cụ; chỉ phát sinh nhập và bán sau mốc đó.
- **Ảnh:** mỗi sản phẩm 3 ảnh SVG sinh tự động (mặt trước, mặt sau, góc nghiêng), màu theo hãng và biến thể, chữ tên model; lưu ở `/images/demo/{mã}-{n}.svg`, `ImageUrl` là ảnh đầu tiên. Ảnh là tệp tĩnh mới của website, cần kiểm tra phản hồi HTTP 200.
- **Mô tả:** `Description` 2–4 câu giới thiệu bằng tiếng Việt; `Specifications` nhiều dòng (màn hình, chip, RAM, bộ nhớ, camera, pin, sạc, hệ điều hành, bảo hành). Viết đa dạng theo phân khúc, không lặp nguyên văn giữa sản phẩm, ghi chú "nội dung minh họa" ở tài liệu chứ không chèn vào từng mô tả.

## 5. Khách hàng và đơn hàng

- **Khách hàng:** 60 người, họ tên Việt phổ biến, địa chỉ ở nhiều tỉnh/thành (TP.HCM, Hà Nội, Đà Nẵng, Cần Thơ, Hải Phòng, Bình Dương, Đồng Nai, Huế, ...) với số nhà hư cấu; điện thoại sinh ngẫu nhiên đúng định dạng nhưng là hư cấu; email `@example.com`. Tạo tài khoản rải đều 18–24 tháng và tăng dần.
- **Hành vi mua:** phân bố lệch (một số khách mua 5–10 lần, nhiều khách mua 1 lần); khoảng 20% đơn giao cho người nhận khác với chủ tài khoản.
- **Trạng thái đơn:** `HOAN_TAT` ~85%, `DA_HUY` ~8% (hủy khi chờ xác nhận hoặc đã xác nhận), `GIAO_THAT_BAI` ~4% (đa số đã nhận lại hàng); đơn đang xử lý (`CHO_XAC_NHAN`, `DA_XAC_NHAN`, `DANG_GIAO`) chỉ ở vài ngày gần nhất. Độ trễ: xác nhận sau 0–1 ngày, xuất giao sau 1–2 ngày, hoàn tất sau 2–4 ngày.
- **Thanh toán:** chỉ COD; `DA_THU` khi và chỉ khi `HOAN_TAT`.
- **Kiểm kê:** 6–10 lần điều chỉnh tồn có lý do trong 2 năm (kiểm kho định kỳ, hàng lỗi).
- **Nhận lại hàng:** mọi lần nhận lại trong dữ liệu demo đều ghi lý do (dịch vụ cho phép bỏ trống nhưng `--audit-db` hiện coi nhận lại không lý do là lỗi; giữ nguyên mã cũ).

## 6. Nhập hàng và tồn kho

- Nhập lô lớn đầu kỳ đủ cho các đơn đầu tiên, sau đó bổ sung theo tốc độ bán, 6–7 phiếu mỗi quý, chọn nhà cung cấp luân phiên.
- Mô phỏng theo trình tự thời gian: tồn không bao giờ âm; mọi đơn chỉ đặt khi còn hàng khả dụng.
- Cuối kỳ: phần lớn biến thể còn 5–40, có biến thể sắp hết (≤ 5) và hết hàng để minh họa báo cáo và cảnh báo.

## 7. Cách tạo: hai hướng và khuyến nghị

| Hướng | Mô tả | Ưu / nhược |
| --- | --- | --- |
| **1. Qua dịch vụ thật** | Công cụ gọi đúng các dịch vụ nghiệp vụ (nhập hàng, đặt đơn, chuyển trạng thái, kiểm kê) với đồng hồ giả lập chạy lùi theo từng ngày | Bất biến đúng tự nhiên; cần xác nhận mã có cơ chế nguồn thời gian có thể thay (ví dụ `TimeProvider`) |
| **2. Ghi trực tiếp bằng SQL/EF** | Công cụ tự tính trạng thái và ghi các bảng theo quy tắc, rồi kiểm bằng `--verify-demo-data` và `--audit-db` | Chủ động mốc thời gian; có nguy cơ lệch bất biến nếu lỗi logic, nên phải kiểm chứng kỹ |

**Đã chốt sau D0: hướng 1.** Các dịch vụ nhập hàng, đặt đơn, chuyển trạng thái, kiểm kê và giỏ nhận `TimeProvider` qua `BusinessKernel`, nên chạy lùi thời gian được bằng đồng hồ giả (đã có tiền lệ `DemoClock` trong `DemoSeeder`). Riêng `AccountRegistration` và `AdminProvisioner` không điều khiển được thời gian: công cụ gán lại `CreatedAt` của tài khoản bằng EF tối thiểu.

## 8. Các bất biến phải đúng (kiểm bằng SQL)

- **Tồn kho:** `OnHand = Σ ΔOnHand` và `Reserved = Σ ΔReserved` theo nhật ký của từng biến thể; `Reserved` = tổng số lượng các đơn đang `CHO_XAC_NHAN`/`DA_XAC_NHAN`; `OnHand ≥ Reserved ≥ 0`.
- **Đơn:** `Total = Σ(Quantity × Price)` của dòng đơn; mỗi đơn đúng một dòng thanh toán COD; `DA_THU` ⇔ `HOAN_TAT`; `Amount = Total` khi đã thu và 0 khi chưa thu; `CompletedAt` chỉ có khi `HOAN_TAT`; `CollectedAt = CompletedAt`.
- **Lịch sử đơn:** chuỗi chuyển trạng thái hợp lệ theo ma trận, thời điểm tăng dần, trạng thái cuối khớp trạng thái hiện tại.
- **Giá:** `Price = Round(Cost × (1 + ProfitPercent/100), 2, AwayFromZero)`; `Cost` của biến thể bằng giá vốn dòng của phiếu xác nhận gần nhất; giá snapshot ở dòng đơn bằng giá hiệu lực lúc đặt.
- **Phiếu nhập:** tổng = Σ dòng; phiếu xác nhận có người xác nhận và thời điểm; phiếu nháp và phiếu hủy không có nhật ký kho; mỗi biến thể trong một phiếu chỉ một dòng.
- **Định danh:** username viết thường duy nhất; điện thoại duy nhất; email NULL hoặc duy nhất; không chuỗi rỗng ở cột không bắt buộc.
- **Thời gian:** UTC; không có mốc tương lai; thứ tự: sản phẩm < nhập hàng < bán; khách < đơn đầu; mốc kiểm kê và hủy hợp lý.
- **Ảnh và mô tả:** mọi `ImageUrl` hợp lệ và có tệp tương ứng; mọi sản phẩm có mô tả và cấu hình không rỗng.

## 9. Checklist nghiệm thu (DATA.*)

Nhãn `[P0]`, `[P1]`, `[P2]` giống các checklist trước. Tổng **43 ô: 24 P0 / 19 P1 / 0 P2**. Trạng thái: `PASS`, `FAIL`, `BLOCKED`, `NOT RUN`, `N/A`; không đánh `PASS` khi chưa có bằng chứng chạy thật.

### A. An toàn và tái lập (DATA.SAFE)

Công cụ ghi dữ liệu lớn nên phải chặn chạy nhầm vào CSDL thật. Đây cũng là lúc hoàn tất giới hạn "seeder chưa khóa theo tên CSDL" ghi trong tài liệu bàn giao backend.

- [ ] [P0] DATA.SAFE.01 — Công cụ chỉ chạy khi tên CSDL bắt đầu bằng `PhoneStore_Demo_` (hoặc `PhoneStore_QA_`); từ chối `PhoneStore_Rebuild`, CSDL thật và mọi tên khác. Có test cho cả trường hợp được phép và bị từ chối.
- [ ] [P0] DATA.SAFE.02 — Mật khẩu (admin và khách demo) chỉ lấy từ biến môi trường, không có mặc định, không in ra log, không nằm trong mã hay file kết quả; thiếu biến thì công cụ dừng với thông báo rõ.
- [ ] [P0] DATA.SAFE.03 — Cần cờ chủ động (`--seed-demo-large` kèm `--confirm`); không bao giờ chạy lúc khởi động server hoặc khi migrate.
- [ ] [P0] DATA.SAFE.04 — Từ chối CSDL đã có dữ liệu nghiệp vụ, trừ khi có `--reset` và tên CSDL qua được kiểm tra ở DATA.SAFE.01.
- [ ] [P1] DATA.SAFE.05 — Tái lập được: cùng `--seed` và `--as-of` cho dữ liệu giống hệt (so sánh băm của bản kê chuẩn hóa).
- [ ] [P1] DATA.SAFE.06 — Có chế độ `--scale small` (vài chục dòng, dùng cho CI) và `--scale full` (dùng cho demo).

### B. Quy mô và dòng thời gian (DATA.SCALE, DATA.TIME)

Số dòng từng bảng xem mục 2; kế hoạch theo quý xem mục 3.

- [ ] [P0] DATA.SCALE.01 — Số dòng của từng bảng nằm trong khoảng mục tiêu ở mục 2 của tài liệu này.
- [ ] [P1] DATA.SCALE.02 — Cả 19 bảng đều có dữ liệu hợp lý (bảng `PHIEN` có thể 0; sổ migration 1 dòng).
- [ ] [P0] DATA.TIME.01 — Có đủ 8 quý đầy đủ cộng quý hiện tại đến ngày `--as-of`; số đơn mỗi quý đúng mục tiêu ±15%.
- [ ] [P1] DATA.TIME.02 — Có mùa vụ: quý 4 > quý 2; tháng 11–12 và đợt trước Tết nổi bật; năm sau tăng trưởng so với năm trước.
- [ ] [P0] DATA.TIME.03 — Có ít nhất 20 đơn đặt trong 30 ngày gần nhất và ít nhất 8 sản phẩm khác nhau có đơn `HOAN_TAT`/`DA_THU` với thời điểm hoàn tất trong tháng hiện tại (UTC+7, kể cả đơn đặt cuối tháng trước), để mục "bán chạy" ở trang chủ (DEC.07, tính theo hoàn tất, không lấy kỳ trước) không rỗng. Nếu `--as-of` rơi vào vài ngày đầu tháng, công cụ điều chỉnh thời điểm hoàn tất của các đơn cuối tháng trước để điều kiện vẫn đạt.
- [ ] [P0] DATA.TIME.04 — Mọi mốc thời gian lưu UTC, không có mốc tương lai, thứ tự hợp lý: sản phẩm tạo trước nhập hàng, nhập hàng trước bán; khách tạo tài khoản trước đơn đầu tiên; tài khoản ADMIN tạo trước phiếu nhập đầu tiên.

### C. Danh mục sản phẩm, ảnh, mô tả (DATA.CATALOG)

- [ ] [P0] DATA.CATALOG.01 — 50 sản phẩm, 10 hãng, 6 danh mục, khoảng 125 biến thể; mã sản phẩm và SKU duy nhất; tên hãng/model thật nhất quán với nguồn đã ghi; dữ liệu cá nhân/đơn hàng vẫn minh họa.
- [ ] [P0] DATA.CATALOG.02 — Mỗi sản phẩm có `ImageUrl` hợp lệ (`https://` hoặc `/images/...`) và ít nhất 2 ảnh trong `SAN_PHAM_ANH` có `SortOrder`. Mọi tệp `/images/demo/...` phản hồi HTTP 200 từ website. Dùng ảnh chụp/ảnh sản phẩm thật; mỗi ảnh có nguồn, tác giả, giấy phép/điều khoản phù hợp và ghi công. Không dùng nhãn hãng thay cho ảnh thiết bị.
- [ ] [P0] DATA.CATALOG.03 — Mỗi sản phẩm có `Description` tiếng Việt (2–4 câu) và `Specifications` nhiều dòng; không rỗng, không trùng nguyên văn giữa các sản phẩm, nằm trong giới hạn độ dài cột; viết lại từ thông số thật có nguồn.
- [ ] [P0] DATA.CATALOG.04 — Mọi biến thể thỏa `Price = Round(Cost × (1 + ProfitPercent/100), 2, AwayFromZero)`, `Cost > 0`, `ProfitPercent` trong 0–10.000.
- [ ] [P1] DATA.CATALOG.05 — Phân bổ phân khúc: giá rẻ 14, tầm trung 16, cao cấp 12, gaming 4, màn hình gập 4; khoảng giá hợp lý theo phân khúc.
- [ ] [P1] DATA.CATALOG.06 — Có vòng đời sản phẩm: khoảng 24 sản phẩm có từ đầu, phần còn lại ra mắt dần; 4 sản phẩm ngừng kinh doanh nhưng giữ lịch sử; model cũ giảm giá vốn theo thời gian.
- [ ] [P1] DATA.CATALOG.07 — Tồn cuối kỳ đa dạng: phần lớn 5–40; ít nhất 8 biến thể sắp hết (≤ 5) và 3 biến thể hết hàng; không có tồn âm.

### D. Khách hàng và tài khoản (DATA.CUST)

- [ ] [P0] DATA.CUST.01 — 60 khách hàng cùng tài khoản `CUSTOMER` tương ứng: username viết thường duy nhất, điện thoại duy nhất, email `@example.com` hoặc NULL (không chuỗi rỗng), mật khẩu băm từ biến môi trường, không có dữ liệu cá nhân thật.
- [ ] [P1] DATA.CUST.02 — Ngày tạo tài khoản rải đều 18–24 tháng và tăng dần; 4 tài khoản bị khóa; 2 khách chưa có đơn nào.
- [ ] [P1] DATA.CUST.03 — Số đơn theo khách phân bố lệch (có khách mua lặp 5–10 lần); địa chỉ đa dạng tỉnh/thành, số nhà là hư cấu.

### E. Nhập hàng (DATA.PURCH)

- [ ] [P0] DATA.PURCH.01 — Ít nhất 56 phiếu nhập từ 12 nhà cung cấp, rải đều các quý; đa số đã xác nhận; có phiếu nháp (kể cả 1 phiếu rỗng) và phiếu đã hủy.
- [ ] [P0] DATA.PURCH.02 — Mỗi phiếu đã xác nhận: tổng = Σ dòng; tồn tăng đúng; `Cost` biến thể bằng giá vốn của phiếu xác nhận gần nhất; có người và thời điểm xác nhận. Phiếu nháp và phiếu hủy không ảnh hưởng tồn hay giá.
- [ ] [P1] DATA.PURCH.03 — Nhà cung cấp: 12, trong đó một vài nhà đã ngừng hoạt động nhưng còn lịch sử.

### F. Đơn hàng, thanh toán, kho (DATA.ORDER)

Dùng ma trận trạng thái ở `02_MA_TRAN_TRANG_THAI.md` làm chuẩn cho lịch sử đơn.

- [ ] [P0] DATA.ORDER.01 — Khoảng 600 đơn (±10%), 1–3 dòng mỗi đơn (khoảng 800 dòng); `Total = Σ(số lượng × giá snapshot)`; giá snapshot bằng giá hiệu lực tại thời điểm đặt.
- [ ] [P0] DATA.ORDER.02 — Tỷ lệ trạng thái hợp lý: `HOAN_TAT` ~85%, `DA_HUY` ~8%, `GIAO_THAT_BAI` ~4% (đa số đã nhận lại hàng); đơn đang xử lý chỉ ở vài ngày gần nhất.
- [ ] [P0] DATA.ORDER.03 — Mỗi đơn đúng 1 dòng thanh toán COD; `DA_THU` khi và chỉ khi `HOAN_TAT`; `Amount = Total` khi đã thu, 0 khi chưa thu; `CollectedAt = CompletedAt`.
- [ ] [P0] DATA.ORDER.04 — Lịch sử đơn tuân theo ma trận trạng thái: chuyển hợp lệ, thời điểm tăng dần, trạng thái cuối khớp trạng thái hiện tại; người xử lý hợp lệ.
- [ ] [P0] DATA.ORDER.05 — Tồn kho khớp nhật ký: `OnHand = Σ ΔOnHand`, `Reserved = Σ ΔReserved`; `Reserved` bằng tổng số lượng các đơn đang ở `CHO_XAC_NHAN`/`DA_XAC_NHAN`; `OnHand ≥ Reserved ≥ 0`.
- [ ] [P1] DATA.ORDER.06 — Có ít nhất 6–10 lần kiểm kê (điều chỉnh tồn) kèm lý do trong 2 năm.
- [ ] [P1] DATA.ORDER.07 — Giờ và ngày đặt hàng phân bố hợp lý; đỉnh ở đầu tháng, 11/11, 12/12, Black Friday và trước Tết.
- [ ] [P1] DATA.ORDER.08 — Khoảng 25 dòng giỏ hàng của khoảng 12 khách (giỏ đang hoạt động, cập nhật gần đây); không có phiên đăng nhập còn hạn.

### G. Kiểm chứng (DATA.VERIFY)

- [ ] [P0] DATA.VERIFY.01 — Lệnh `--verify-demo-data` chạy mọi bất biến SQL ở mục 8 và trả mã thoát khác 0 khi vi phạm; in tóm tắt đếm theo bảng, theo quý (số đơn, doanh thu) và theo trạng thái.
- [ ] [P0] DATA.VERIFY.02 — Công cụ `--audit-db` hiện có chạy thành công trên CSDL demo.
- [ ] [P0] DATA.VERIFY.03 — Smoke đầu-cuối trên CSDL demo: ADMIN đăng nhập, danh sách đơn phân trang, báo cáo doanh thu của từng quý có số liệu khác 0 và khác nhau, bán chạy, tồn kho, hóa đơn của một đơn hoàn tất; website: trang chủ có "bán chạy", danh mục hiển thị đủ sản phẩm đang bán kèm ảnh và mô tả, một khách demo đăng nhập xem được lịch sử đơn.
- [ ] [P1] DATA.VERIFY.04 — WinForms mở với CSDL demo: tổng quan và báo cáo hiển thị đúng; có ảnh chụp.
- [ ] [P1] DATA.VERIFY.05 — Hiệu năng chấp nhận được với dữ liệu đầy đủ: danh sách và báo cáo mở nhanh; ghi số đo.
- [ ] [P1] DATA.VERIFY.06 — Toàn bộ regression và test các giai đoạn trước vẫn đạt; không đổi mã chạy thật.

### H. Tài liệu và cổng bàn giao (DATA.DOC, DATA.GATE)

- [ ] [P1] DATA.DOC.01 — Có `docs/DEMO_DATA.md`: cách tạo và đặt lại CSDL demo, các biến môi trường (không kèm giá trị), quy mô, giả định, giới hạn.
- [ ] [P1] DATA.DOC.02 — Bằng chứng tóm tắt không chứa bí mật: `docs/evidence/demo-data/summary.json` và ảnh chụp.
- [ ] [P0] DATA.GATE.01 — Mọi ô P0 là `PASS`; CI xanh trên đúng commit mới nhất; diff chứng minh không đổi dịch vụ, schema, migrations, API, website MVC hay WinForms. Phạm vi cho phép chỉ gồm: công cụ mới, test mới, tài liệu, đăng ký dự án vào solution, thêm bước vào CI (không sửa bước cũ) và tệp ảnh tĩnh mới.
- [ ] [P1] DATA.GATE.02 — Báo cáo theo nhãn P0/P1/P2 của tài liệu này; ô chưa chạy giữ `NOT RUN` kèm lý do.

## 10. Bảng đếm mong đợi

| P0 | P1 | P2 | Tổng |
| ---: | ---: | ---: | ---: |
| 24 | 19 | 0 | 43 |

