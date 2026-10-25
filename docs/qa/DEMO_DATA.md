# Dữ liệu mẫu nhiều kỳ báo cáo

Đã bổ sung vào PhoneStore_QA_20261025_061611_2722bd; không reset/xóa dữ liệu có sẵn, không ghi PhoneStore. Các giao dịch tạo qua StoreService; chỉ dữ liệu do batch tạo được đặt ngày lịch sử để trình diễn. Không phải giao dịch kinh doanh thật hoặc giá/thông số thực tế.

| Đối tượng thêm | Số lượng |
| --- | --- |
| Sản phẩm | 24 |
| Biến thể | 72 |
| Khách hàng | 40 |
| Đơn hàng | 410 |
| Phiếu nhập | 136 |
| Nhà cung cấp | 4 |

Dữ liệu từ01/2024 đến10/2026. Có102 phiếu chốt và34 phiếu nháp; hàng hết không được đặt, giữ/xuất/nhận trả thông qua service. Có nhiều mức giá, màu/dung lượng. Tài khoản khách mẫu dùng mật khẩu ngẫu nhiên không công bố; không thay tài khoản QA hiện có.

| Trạng thái đơn mẫu | Số lượng |
| --- | --- |
| HOAN_TAT | 305 |
| DA_HUY | 34 |
| GIAO_THAT_BAI | 68 |
| CHO_XAC_NHAN | 1 |
| DA_XAC_NHAN | 1 |
| DANG_GIAO | 1 |

## Doanh thu theo năm của batch mẫu

| Năm | Doanh thu |
| --- | --- |
| 2024 | 1.945.560.000 đ |
| 2025 | 1.815.060.000 đ |
| 2026 | 1.550.820.000 đ |

## Doanh thu theo quý của batch mẫu

| Năm | Quý | Doanh thu |
| --- | --- | --- |
| 2024 | 1 | 515.190.000 đ |
| 2024 | 2 | 469.290.000 đ |
| 2024 | 3 | 435.540.000 đ |
| 2024 | 4 | 525.540.000 đ |
| 2025 | 1 | 397.290.000 đ |
| 2025 | 2 | 516.990.000 đ |
| 2025 | 3 | 438.690.000 đ |
| 2025 | 4 | 462.090.000 đ |
| 2026 | 1 | 503.940.000 đ |
| 2026 | 2 | 415.740.000 đ |
| 2026 | 3 | 516.540.000 đ |
| 2026 | 4 | 114.600.000 đ |

## Doanh thu theo tháng của batch mẫu

| Tháng | Đơn hoàn tất đã thu | Doanh thu |
| --- | --- | --- |
| 2024-01 | 9 | 213.280.000 đ |
| 2024-02 | 9 | 167.380.000 đ |
| 2024-03 | 9 | 134.530.000 đ |
| 2024-04 | 9 | 122.380.000 đ |
| 2024-05 | 9 | 149.830.000 đ |
| 2024-06 | 9 | 197.080.000 đ |
| 2024-07 | 9 | 175.480.000 đ |
| 2024-08 | 9 | 140.380.000 đ |
| 2024-09 | 9 | 119.680.000 đ |
| 2024-10 | 9 | 151.630.000 đ |
| 2024-11 | 9 | 191.230.000 đ |
| 2024-12 | 9 | 182.680.000 đ |
| 2025-01 | 9 | 136.780.000 đ |
| 2025-02 | 9 | 125.530.000 đ |
| 2025-03 | 9 | 134.980.000 đ |
| 2025-04 | 9 | 194.830.000 đ |
| 2025-05 | 9 | 177.280.000 đ |
| 2025-06 | 9 | 144.880.000 đ |
| 2025-07 | 9 | 131.380.000 đ |
| 2025-08 | 9 | 132.280.000 đ |
| 2025-09 | 9 | 175.030.000 đ |
| 2025-10 | 9 | 182.230.000 đ |
| 2025-11 | 9 | 152.080.000 đ |
| 2025-12 | 9 | 127.780.000 đ |
| 2026-01 | 9 | 138.130.000 đ |
| 2026-02 | 9 | 179.980.000 đ |
| 2026-03 | 9 | 185.830.000 đ |
| 2026-04 | 9 | 146.680.000 đ |
| 2026-05 | 9 | 135.880.000 đ |
| 2026-06 | 9 | 133.180.000 đ |
| 2026-07 | 9 | 177.280.000 đ |
| 2026-08 | 9 | 187.630.000 đ |
| 2026-09 | 9 | 151.630.000 đ |
| 2026-10 | 8 | 114.600.000 đ |

## Kiểm chứng

Build Release không warning/error. Đã chạy lại seed nhiều lần: vẫn24 sản phẩm/72 biến thể/40 khách/410 đơn/136 phiếu, không nhân đôi. Checkpoint theo DB nằm .artifacts/demo-data; giữ checkpoint nếu muốn chạy lại trên cùng DB. Có khóa liên tiến trình chống hai seed chạy đồng thời.

Đã đối chiếu tồn thực/giữ với toàn bộ nhật ký, giữ với đơn mở; tổng đơn/dòng/thanh toán; tổng phiếu nhập và lượng thực nhận, nháp không cộng tồn. Báo cáo service được so với SQL trong 49 kỳ (34 tháng,12 quý,3 năm): 49 PASS. Bằng chứng demo-data-summary.json, demo-report-verification.json và .artifacts/demo-data/final-run.txt. Số năm/tháng tạo được tính tới ngày chạy, lần này25/10/2026.

Bảng doanh thu phía trên chỉ gồm batch mẫu; báo cáo trong app gồm cả đơn có sẵn nên có thể cao hơn. report-verification.json đối chiếu toàn bộ DB theo đúng điều kiện HOAN_TAT và DA_THU, ngày UTC+7. Không cộng đơn hủy, chưa thu hoặc giao thất bại vào doanh thu.

## Xem trong ứng dụng

Mở Admin hiện có, bấm Tải lại ở Sản phẩm/Khách hàng/Phiếu nhập/Đơn hàng. Tìm DEMO hoặc Khách mẫu. Vào Báo cáo doanh thu và chọn khoảng ngày từng tháng/quý/năm2024–2026. Các phiếu nháp và đơn đang xử lý có thể thao tác tiếp; backend và website đọc cùng SQL.

Chạy lại khi cần: node scripts/qa.mjs demo, sau đó node scripts/demo-data-report.mjs. Cần manifest QA đã init; không fallback DB. Khi batch đã hoàn tất, lệnh chỉ đối chiếu/chuẩn hóa ngày dữ liệu demo, không tạo thêm bộ mới. Chưa bổ sung chức năng xuất Excel/PDF hoặc chọn quý mới vào UI; dùng bộ lọc khoảng ngày hiện có.
