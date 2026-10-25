# Kết quả kiểm thử ngày 25/10/2026

## Đã chạy

| Nhóm | Kết quả | Bằng chứng |
|---|---|---|
| Build solution (Core/Infrastructure/Web/Admin/Checks) | Thành công, 0 warning, 0 error | Lệnh dotnet build PhoneStore.slnx --no-restore |
| Quy tắc miền | 8/8 đạt | verification/sql-checks.txt, 8 dòng PASS đầu |
| Tích hợp SQL Server nhiều DbContext/kết nối riêng | 20/20 nhóm đạt | verification/sql-checks.txt |
| HTTP/API và form MVC | 6/6 nhóm đạt | verification/http-checks.txt |
| Chrome headless desktop 1440px/mobile 390px | 10/10 đạt | verification/browser-checks.txt |
| WinForms kết nối API, tải/render 11 trang | Đạt; có kiểm tra ràng buộc BrandId khi sửa | verification/winforms-smoke.txt |
| Editor sản phẩm, phiếu nhập, khách hàng | Đã mở ẩn, render và xem ảnh | .artifacts/winforms, screenshots/admin-product.png |
| SQL backup COPY_ONLY/CHECKSUM + RESTORE VERIFYONLY | Đạt | scripts/BackupRestore.ps1 đã chạy trên DB kiểm thử |
| SQL restore vào DB mới | Đạt, không REPLACE | DB PhoneStore_Restored_Acceptance20261025 |

SQL run cuối dùng DB `PhoneStore_Checks_20261025_045140_96764`; các DB kiểm thử được giữ lại, không xóa hoặc dùng chung DB PhoneStore. Backup/restore đã chạy từ DB `PhoneStore_Checks_20261025_043901_0318d`. File backup tại thư mục mặc định của SQL Server, tên có timestamp 20261025_114652.

HTTP chạy vào PhoneStore, tạo tài khoản `verify1025045137` và đơn hoàn tất `DH66617f3e87124958a95e1ba0`. Chrome dùng khách mẫu, hợp nhất giỏ browser rồi tạo/hủy đơn để giải phóng giữ. Đây là dữ liệu thử có lưu thật; các bảng/biểu đồ trong ảnh đọc từ DB.

## Nội dung đã xác minh

SQL: unique username/SĐT, hash, login sai/role sai; lọc/phân trang danh mục; tạo biến thể kho 0; phiếu nháp/sửa/chốt/chốt lặp/hủy/rỗng/giá vốn sai; giỏ thêm/sửa/xóa/merge không giữ; checkout và RequestKey retry; owner; hủy; COD; failure/return một lần; đổi giá và snapshot; hai khách đặt món cuối; rollback nhiều dòng sau khi đã giữ dòng đầu; xác nhận và hủy tranh chấp cùng rowversion; kiểm kê có lý do/tồn không dưới giữ; khóa/mở/đổi password hủy phiên; báo cáo loại đơn đúng, kỳ ngày UTC+7, rỗng; soft CRUD hãng/NCC/sản phẩm; stale form sản phẩm; đối chiếu toàn bộ nhật ký với số dư thực/giữ và số tiền thu với tổng đơn.

HTTP: catalog public và trang Razor trả tiếng Việt; không login không vào Admin; CSRF thiếu token bị chặn; đăng ký/login bằng form MVC; khách bị 403 với API Admin; thêm/sửa giỏ, đọc hidden price và gửi checkout theo culture vi-VN; chi tiết đơn; Admin login/xác nhận/xuất/hoàn tất; giảm kho/giữ đúng; doanh thu có đơn; dữ liệu kiểm kê sai bị từ chối; cookie cũ của tài khoản khóa bị 401; logout mất quyền.

Chrome: JS chọn biến thể đổi giá; localStorage guest cart đọc giá/tồn DB; login thật hợp nhất rồi xóa guest storage; checkout thật và hủy thật; responsive không tràn ngang tại catalog/checkout 390px; form hồ sơ; không có unhandled JS exception. Ảnh đã xem: catalog desktop, checkout mobile.

WinForms: tải dữ liệu từng trang và biểu đồ từ API; render editor và xem ảnh. Phát hiện/correct kết buộc ComboBox hãng trước khi chụp lần cuối; assert hãng trên form trùng BrandId của sản phẩm. Không coi ảnh đang tải là bằng chứng dữ liệu đã tải: smoke đợi loader hoàn tất trước chụp.

## Chưa kiểm chứng và giới hạn

- Chưa chạy máy in vật lý/print spooler. Có code PrintDocument phân trang và PrintPreview, nhưng không báo là đã in thành công.
- Chưa tự động thao tác tất cả nút/dialog native của WinForms. Dịch vụ/API CRUD đã kiểm tra; cần kiểm tra tay luồng thêm sản phẩm → chuyển phiếu, chỉnh dòng phiếu, hộp thoại lý do và xác nhận trên máy đích.
- Chưa benchmark tải lớn, kiểm tra mất mạng thực, audit bảo mật độc lập, hay triển khai HTTPS production. Khóa toàn writer bảo vệ đúng nhưng giới hạn thông lượng.
- Seed chứa ảnh SVG minh họa. Trường URL ảnh lưu/sửa thật; chưa thử máy chủ ảnh HTTPS bên ngoài cụ thể.
- Script backup không phải lịch backup tự động. Chưa thử sự cố mất ổ đĩa/khôi phục sang phiên bản SQL khác.
- Các nhãn nhỏ activity 12–21 trong bản tổng hợp tài liệu chưa xác nhận từng chữ; dùng đặc tả UC/thiết kế chữ đi kèm để triển khai và truy vết.

## Kiểm tra tay trước vận hành

1. Mở WinForms ở 1280×800/DPI máy đích, login Admin, thử toàn bộ thêm/sửa/ngừng và form nhiều biến thể.
2. Thêm sản phẩm với lượng cần nhập, chọn NCC/giá vốn ở phiếu mở sẵn, lưu/chốt; thử chốt lại và sửa phiếu đã chốt.
3. Kiểm tra in hóa đơn dài nhiều trang bằng máy in đích; đối chiếu snapshot và tổng.
4. Đặt đơn từ điện thoại thật, thao tác quản trị, thử mất mạng rồi tải lại trước khi bấm lặp.
5. Thiết lập chứng chỉ HTTPS/kho khóa Data Protection bền vững/tài khoản DB runtime riêng; đổi mật khẩu mẫu và cấu hình lịch backup.

## Kết quả QA mới thay thế đánh giá lịch sử

Xem ../TEST_REPORT.md, ../QA_CHECKLIST.md và ../BUG_REPORT.md: 8 lỗi đã sửa và kiểm tra lại trên DB QA mới. Không dùng kết quả lịch sử phía trên để đánh dấu PASS cho ca chưa chạy trong checklist mới. Script backup hiện bị ExecutionPolicy chặn; in vật lý chưa kiểm chứng.
