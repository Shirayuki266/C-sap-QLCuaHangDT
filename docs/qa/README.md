# Môi trường và hồ sơ QA

Dữ liệu mẫu nhiều kỳ đã được bổ sung vào QA hiện có: 24 sản phẩm, 72 biến thể, 40 khách, 410 đơn, 136 phiếu nhập, trải01/2024–10/2026. Xem [DEMO_DATA.md](DEMO_DATA.md). Công cụ chạy lại an toàn: `node scripts/qa.mjs demo`; không reset dữ liệu có sẵn.

Ứng dụng dùng SQL Server qua EF Core. Website khách hàng MVC và API cùng origin; quản trị vẫn là WinForms C#. Không cần cấu hình CORS khác origin; WinForms dùng URL API của backend QA.

## Điều kiện và cấu hình

Windows, .NET SDK 10, SQL Server đang chạy với quyền CREATE DATABASE/migration, Node 22+ và Chrome. `qa/settings.json` xác định server QA, kết nối Windows Authentication, tiền tố DB và môi trường QA. Muốn dùng server khác, sửa server QA rõ ràng và đặt `PHONESTORE_TEST_CONNECTION` ngoài repository nếu cần credential. Server trong connection string phải khớp cấu hình. Không đổi connection string mặc định ứng dụng.

## Khởi tạo và chạy tương tác

```powershell
node scripts/qa.mjs init
node scripts/qa.mjs verify
node scripts/qa.mjs serve
```

Init build, tạo DB `PhoneStore_QA_<timestamp>_<suffix>`, áp dụng migration hiện có và seed dữ liệu QA. Init lặp khi đã có manifest không reset/nhân đôi dữ liệu. Server lấy URL từ `.artifacts/qa-env/environment.json`; init chọn cổng trống. Nếu cổng đã bị chiếm sau init, server dừng báo lỗi; không tự chuyển sang ứng dụng thật.

Môi trường tương tác đã tạo trong đợt này: **http://localhost:65377**, DB **PhoneStore_QA_20261025_061611_2722bd**, server `localhost`. Lệnh serve cần tiếp tục chạy để truy cập được URL. Kiểm tra `/api/qa/identity` để thấy môi trường/server/DB đang dùng. URL và DB mới nhất luôn đọc từ manifest, không hard-code URL vào test. Dừng serve trước khi sửa/build lại mã để tránh khóa apphost trên Windows. `verify` chỉ đọc và so sánh fixture ban đầu; sau khi thao tác tay làm đổi số dư, nó có thể báo khác dự kiến và không tự reset dữ liệu.

Tài khoản `admin`, `khachhang`, `qa-second` và mật khẩu ngẫu nhiên nằm trong **`.artifacts/qa-env/private.json`**, chỉ dùng cục bộ. Mở tệp trong IDE để lấy; không copy tệp/ảnh chụp thông tin này vào báo cáo hoặc commit. `.artifacts/` được gitignore. Không có mật khẩu QA cố định trong tài liệu. WinForms chạy bình thường rồi nhập URL QA vào màn hình kết nối và dùng tài khoản QA.

Fixture gồm sản phẩm QA có ba biến thể 100.000/200.000/300.000 VNĐ; tồn 0/1/30 trước các giao dịch seed; sáu trạng thái đơn hợp lệ, COD đã thu/chưa thu, phiếu nháp và phiếu chốt. Đơn giữ/xuất làm thay đổi tồn cuối đúng nghiệp vụ. Tất cả thông tin cá nhân là dữ liệu thử, không sao chép dữ liệu thật. Đơn giao thất bại chưa nhận lại được để thử luồng nhận trả; không tự cộng lại kho.

## Kiểm thử và xem báo cáo

```powershell
node scripts/qa.mjs test
node scripts/qa-safety-checks.mjs
# Với backend QA tương tác đang chạy, chạy bộ kiểm thử cũ đã bảo vệ:
node scripts/qa.mjs legacy
```

`test` build rồi tạo DB/cổng riêng cho **mỗi lần** chạy, kiểm tra HTTP/SQL/E2E/Chrome/WinForms và sinh báo cáo. Không chạy ghi dữ liệu vào DB tương tác hay PhoneStore. Test con nhận cùng server/DB rõ ràng và kiểm tra identity trước ghi. `legacy` lấy config từ manifest; SQL integration tạo thêm DB QA riêng, HTTP chỉ tới backend đã xác minh. Không có fallback URL5080 hoặc PhoneStore khi thiếu config. Các test trực tiếp đòi đủ `PHONESTORE_API_URL`, `PHONESTORE_TEST_CONNECTION`, `PHONESTORE_QA_SERVER`, `PHONESTORE_QA_DATABASE` và credential thích hợp; ưu tiên script để khỏi cấu hình thủ công.

Xem [QA_CHECKLIST.md](QA_CHECKLIST.md), [REQUIREMENTS_TRACEABILITY.md](REQUIREMENTS_TRACEABILITY.md), [API_INVENTORY.md](API_INVENTORY.md), [BUG_REPORT.md](BUG_REPORT.md), [TEST_REPORT.md](TEST_REPORT.md), [PROGRESS.md](PROGRESS.md). Các bản ở gốc được giữ tương thích. Bằng chứng request không có body/token/cookie/password. Kết quả test FAIL trả exit code khác0. Các mục chưa chạy không được đổi thành PASS nhờ build.

## Dọn dữ liệu khi gọi rõ ràng

Dừng serve trước, rồi chỉ khi muốn xóa DB QA tương tác hiện hành:

```powershell
node scripts/qa.mjs cleanup
```

Cleanup kiểm tra chính xác server và DB trong manifest, tiền tố QA, rồi DROP đúng DB đó, không dùng REPLACE hoặc cưỡng chế đóng kết nối. Nếu DB đang có kết nối, SQL từ chối; dừng ứng dụng QA rồi thử lại. Không tự xóa DB runner lịch sử hoặc PhoneStore. DB runner được giữ để đối chiếu; tên trong bằng chứng, không xóa hàng loạt theo wildcard.

PowerShell ExecutionPolicy không cản script Node/.NET này; không cần đổi policy. Script vận hành `BackupRestore.ps1` không được dùng để reset test và vẫn chưa nghiệm thu trong đợt này. Cài máy sạch và in vật lý còn cần xác minh riêng.

## Quy tắc đối chiếu hai rủi ro

Số lượng0 ở PUT giỏ nghĩa là xóa dòng, ở POST thêm hàng bị từ chối. Null/phân số/âm/overflow phải từ chối mà không đổi SQL. Thêm lặp cộng cùng dòng; tồn được giữ khi checkout. Race hàng cuối và đổi tồn được kiểm tra bằng request thật và đối chiếu SQL.

Tài liệu quy định lọc danh mục theo hãng, dung lượng và giá; không có bộ lọc màu ở danh sách. Màu được chọn ở chi tiết biến thể, nên không tự thêm tham số màu giả. Giả định hiển thị đã ghi nhận: giá danh sách và sắp giá lấy các biến thể phù hợp bộ lọc; chi tiết có đủ biến thể để chọn màu/dung lượng; giỏ và checkout dùng đúng ID/giá từ DB. Giá và snapshot SQL được so sánh trong QA-FILTER-CROSS-FLOW, cùng QA-FILTER và các test browser. Không coi rủi ro là lỗi mới khi chưa tái hiện vi phạm.
