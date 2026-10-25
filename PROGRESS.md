# Tiến độ triển khai — 25/10/2026

## Đã triển khai

- Đọc/đối chiếu nội dung chữ, bảng, trích xuất 48 hình; REQUIREMENTS.md ánh xạ K01–K15, A01–A23, UC01–UC10, UI01–UI12 và phi chức năng.
- SQL Server thật: 16 bảng nghiệp vụ + PHIEN, migration phiên bản/hash, khóa ngoại/unique/CHECK/rowversion, seed. Không còn adapter catalog giả trong runtime.
- Tài khoản/phiên/password/hash, quyền server, CSRF, khóa/mở và thu hồi phiên cũ.
- Website MVC khách hàng: danh mục/lọc/chi tiết/biến thể, giỏ guest và giỏ DB, hợp nhất, checkout COD, hồ sơ/password, lịch sử/chi tiết/timeline/hủy đơn.
- WinForms Admin: 11 trang chức năng; product editor nhiều biến thể/lượng cần nhập, hãng/danh mục, khách hàng/khóa/mở/xem đơn, NCC, phiếu nhập nhiều dòng/giá vốn/nháp/chốt/hủy, tồn/kiểm kê/nhật ký, xử lý đơn/giao/COD/nhận trả, hóa đơn, báo cáo bảng/biểu đồ và tổng quan.
- Giao dịch nguyên tử, conditional SQL stock update, transaction-owned cross-process lock, RequestKey/OperationKey, rowversion, nhật ký và snapshot.
- BackupRestore.ps1: backup, verify và restore vào DB mới không ghi đè.
- README, ARCHITECTURE, TESTING, bằng chứng log và ảnh.

## Đã kiểm tra

- Build solution thành công không warning/error.
- 8 unit checks + 20 SQL groups + 6 HTTP/MVC groups + 10 Chrome checks đạt.
- WinForms tải/render 11 trang, render 3 editor; kiểm tra hãng không bị reset khi bind form sửa.
- Backup+VERIFYONLY và restore thử trên DB tách biệt đã thành công.

Chi tiết và giới hạn bằng chứng trong docs/TESTING.md. Các kiểm thử SQL có DB riêng; HTTP/Chrome để lại dữ liệu giao dịch mẫu thật trong PhoneStore.

## Lỗi còn biết

Không còn lỗi build hoặc lỗi trong các bộ kiểm tra trên. Chưa xác minh mọi đường tương tác desktop nên không suy ra không còn lỗi ngoài các kịch bản đã chạy.

## Phần chưa nghiệm thu / bước tiếp theo

- Bấm kiểm tra đầy đủ dialog/nút WinForms trên máy đích, đặc biệt chuyển tạo sản phẩm sang phiếu nhập và in chứng từ dài.
- Máy in vật lý, thiết bị mobile thật, tải cao, mất kết nối thực tế và môi trường HTTPS production.
- Cấu hình kho khóa bền vững và lịch backup khi triển khai vận hành; hiện script backup có thể chạy theo yêu cầu.
- Nhãn nhỏ activity 12–21 ở ảnh tổng hợp nguồn chưa đọc chắc từng chữ; đã ghi rõ trong REQUIREMENTS, không tự coi là đã đọc hết nhãn hình.

Không có module giả lập được đánh dấu hoàn tất. Chưa tuyên bố nghiệm thu production hoặc máy in khi chưa có bằng chứng.

## Đợt QA mới nhất — thay thế đánh giá kiểm thử phía trên

Đã tái hiện, sửa và xác minh lại 8 lỗi trong BUG_REPORT.md: giỏ sai kiểu bị xóa; giá ngoài bộ lọc; tiền mất phần lẻ; envelope lỗi API; thiếu mã biến thể vẫn thành công; lỗi mạng WinForms; preview giỏ âm; lỗi mạng website chưa Việt hóa.

Build Release 6 project và solution đạt 0 warning/error. Runner cuối 336/336 PASS, gồm 6 E2E. Chrome 12 và WinForms 4 kiểm tra con đã nằm trong tổng runner; 8 unit checks riêng đạt. Inventory có 44 method/route. Đã kiểm tra tranh hàng cuối, hai Admin chốt nhập/COD, hủy đồng thời xác nhận, timeout sau commit/retry, rollback giữa phiếu nhập và đối chiếu SQL cuối phiên. WinForms thực sự bấm tạo sản phẩm → lưu lặp → mở phiếu nhập đúng hàng/lượng → lưu nháp; kho chưa tăng trước xác nhận.

Checklist: 400 PASS, 0 FAIL, 46 NOT RUN, 2 BLOCKED, 8 N/A. Các dòng truy vết dùng lại bằng chứng không phải lần chạy độc lập. Chưa tuyên bố hoàn tất QA. Dữ liệu người dùng không bị thay đổi trong đợt này; runner dùng DB riêng.

Bước tiếp theo: thực hiện các dòng NOT RUN trong TEST_REPORT.md, ưu tiên mọi nhánh UI, keyboard/navigation, trạng thái từng màn hình, profiling truy vấn và cài trên máy sạch. Script backup bị ExecutionPolicy chặn trong đợt này; không đổi chính sách máy. In vật lý chưa nghiệm thu. Xem README để chạy lại và docs/qa để đối chiếu bằng chứng.

## Cấu hình QA riêng và bỏ fallback của bộ cũ

Đã bổ sung qa/settings.json, appsettings.QA.json, QaTarget guard, công cụ init/serve/test/legacy/cleanup và sáu hồ sơ trong docs/qa. CSDL tương tác PhoneStore_QA_20261025_061611_2722bd tại http://localhost:65377. Mỗi vòng runner vẫn dùng DB/cổng khác. Không sửa connection mặc định hoặc dữ liệu PhoneStore; không commit/push/reset/clean.

Đã chạy init hai lần không nhân đôi, runner340/340, bộ cũ20SQL+6HTTP, guard5/5; dữ liệu fixture và hai rủi ro giỏ/giá đã được kiểm tra lại. Không có lỗi nghiệp vụ mới được xác nhận trong lượt này; 8 lỗi lượt trước vẫn được kiểm tra hồi quy. Các lỗi chuẩn bị fixture được phân biệt trong báo cáo. Trạng thái và bằng chứng cuối xem docs/qa/PROGRESS.md và TEST_REPORT.md; còn NOT RUN/BLOCKED, không tuyên bố toàn bộ QA hoàn tất.

## WinApp Windows và shortcut

Đã bổ sung cấu hình kết nối LocalAppData, màn hình đăng nhập hai trường, launcher health/fingerprint, publish win-x64 self-contained cả Admin/backend và shortcut Desktop. Backend QA đang dùng chung với website; không tự migration/seed/reset và không tắt khi đóng Admin. Packaging10 ca đạt, UI shortcut6 ca và UI lỗi HTTP500 đạt; bằng chứng cuối ở docs/qa/ADMIN_PACKAGING_REPORT.md. Không tự đóng VS Code người dùng; máy sạch không SDK chưa có VM để xác minh.

## Dữ liệu mẫu nhiều kỳ báo cáo

Đã thêm vào DB QA hiện dùng: 24 sản phẩm, 72 biến thể, 40 khách hàng, 4 NCC, 410 đơn và 136 phiếu nhập (102 đã chốt, 34 nháp), trải tháng01/2024–10/2026. Không reset dữ liệu có sẵn hoặc ghi vào PhoneStore. Có305 đơn hoàn tất đã thu,34 hủy,68 giao thất bại (gồm đã nhận trả/chưa nhận),3 đơn đang xử lý.

Build Release đạt0 warning/error; chạy seed lặp không nhân đôi. Đối chiếu tồn/giữ/journal/đơn/phiếu nhập đạt;49 kỳ báo cáo tháng/quý/năm được so service với SQL, tất cả PASS. Xem docs/qa/DEMO_DATA.md và docs/qa/demo-data-run.txt. Công cụ: node scripts/qa.mjs demo. Không thay đổi nghiệp vụ hoặc bổ sung chức năng export báo cáo trong lượt này.

## Website: Home, tìm kiếm gợi ý, ảnh và QR

Đã triển khai Home riêng, danh mục/Search có URL bộ lọc màu cùng biến thể, autocomplete debounce300ms/chống response cũ/keyboard, bán chạy dùng query doanh thu chung với báo cáo. Public DTO bỏ tồn nội bộ/rowversion. Gallery ảnh HTTPS qua migration002 và trường Images tùy chọn; không đổi/xóa cột ảnh đại diện.

Sau rà soát trực tiếp đã xác minh13 URL của8 model, cập nhật9 cover và6 gallery trong QA; các model chưa có nguồn phù hợp dùng fallback. Không ghi đè ảnh người dùng. COD vẫn hoạt động; QR disabled và backend từ chối rõ ràng, chưa có ngân hàng/webhook/QR giả.

Build0 warning/error;349/349 ca trên DB QA mới,22 kiểm tra Chrome con,4 WinForms con và12 kiểm tra Chrome chỉ đọc trên QA đang dùng đều đạt.21 tiêu chí đợt website đạt, tái sử dụng bằng chứng chứ không tăng số lần chạy độc lập. Xem docs/qa/WEB_DISCOVERY_REPORT.md. Chưa benchmark production hoặc kiểm tra mọi thiết bị thật; các mục QA tổng thể chưa nghiệm thu vẫn giữ nguyên.

Đã publish và kích hoạt http://localhost:65377/ (Home), /Catalog, /Search?Query=iphone; cùng database cũ, không reset sản phẩm/đơn/khách/phiếu nhập. Cấu hình launcher Admin trỏ executable backend publish mới. Đã publish lại bản Desktop Admin có trường gallery sau khi đóng riêng màn hình Login và mở lại shortcut. QR tích hợp ngân hàng/đối soát được để lại giai đoạn sau.
