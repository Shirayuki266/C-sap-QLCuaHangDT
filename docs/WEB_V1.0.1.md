# Hoàn thiện Web — web-v1.0.1

Đợt này hoàn thiện giao diện Monophone theo phản hồi trực tiếp của người dùng: footer bốn cột, logo/icon, thêm giỏ từ thẻ sản phẩm, phân trang số, ảnh đại diện mới, nền xanh nhạt và khung thống nhất cho các thao tác chữ xanh.

## Hành vi

- Tài khoản, đổi mật khẩu, đăng xuất, quay lại giỏ/danh sách, tiếp tục xem máy, xóa dòng giỏ, xóa bộ lọc, chi tiết đơn, breadcrumb và liên kết footer đều có khung. Trạng thái tài khoản đang chọn dùng nền xanh; bàn phím vẫn có viền focus.
- Nút giỏ nhỏ cạnh “Xem chi tiết” mở phần chọn phiên bản ngay trên thẻ. Chỉ dùng phiên bản do máy chủ trả về, số lượng một máy; hết hàng bị chặn. Giỏ khách vẫn Quote rồi lưu localStorage; giỏ tài khoản dùng POST/antiforgery hiện có. Lỗi tải cho phép thử lại.
- Ô phân trang giữ toàn bộ điều kiện lọc/sắp xếp, đánh dấu trang hiện tại và rút gọn bằng dấu ba chấm khi cần.
- Footer giới thiệu nhóm sinh viên Đại học Sài Gòn. Số tổng đài là số minh họa; các mục chính sách/dịch vụ chỉ hiển thị. Năm icon mạng xã hội cùng trỏ về Facebook người dùng chỉ định; không tích hợp dịch vụ mới.
- Bộ ảnh Web gồm một ảnh chọn riêng cho mỗi model trong 50 model demo; 46 model đang công khai tại thời điểm kiểm tra. Không đổi trạng thái bốn model còn lại. Nguồn và giới hạn ghi tại [STOREFRONT_PRODUCT_IMAGES.md](STOREFRONT_PRODUCT_IMAGES.md).

Chỉ sửa Web, test và tài liệu Web. Không sửa Core, Infrastructure, Admin, schema/migration, API nghiệp vụ hoặc dữ liệu demo. Không đổi tag/release cũ.

## Kiểm chứng

[Bằng chứng và dấu vân tay nguồn](evidence/web-v1.0.1/validation.json) cùng ảnh chụp QA nằm trong thư mục `docs/evidence/web-v1.0.1/`. Đây là kiểm tra tự động trên dữ liệu QA, không phải ảnh tài khoản cá nhân của người dùng.

- Build Release Web/checks: không warning/error.
- Chromium tài khoản: đăng ký/đăng nhập, chống gửi trùng, đổi mật khẩu, đăng xuất, bàn phím, đối chiếu SQL; tám ảnh desktop/mobile.
- Chromium FLOW01–05: luồng giỏ khách → hợp nhất → đặt hàng → hủy, thay đổi giá/trạng thái và hết phiên; 51 ảnh/trạng thái, 16 mục kiểm tra và đối chiếu SQL theo từng bước.
- Chromium catalog và cart: hồi quy responsive 3/2/1 cột, chọn phiên bản, ảnh lỗi/no-JS, bộ lọc/phân trang, giỏ khách, retry/merge, cập nhật/xóa; có kiểm tra thêm giỏ ngay trên thẻ và chặn phiên bản hết hàng.
- Rà khung thao tác trên 15 tổ hợp trang công khai/kích thước 1440, 390, 320px: không tràn ngang hoặc thao tác chữ xanh thiếu khung. Các capture tài khoản/FLOW cũng kiểm tra khung.
- Kiểm tra ảnh riêng: đủ 50 ảnh tải được; toàn bộ 46 model công khai và trang chi tiết dùng đúng ánh xạ. Test catalog đối chiếu tên model, SHA256 nguồn và SVG, đồng thời bảo vệ ảnh tùy chỉnh/ảnh thiếu/model không khớp.

Lệnh hồi quy SQL tổng thể: `dotnet run --project tests/PhoneStore.Checks -c Release -- --integration`. Bốn bộ browser dùng các runner/script hiện có, chỉ tạo/xóa CSDL QA do runner sở hữu. Các lỗi giả lập trong log là một phần kiểm tra trạng thái lỗi.

CI Ubuntu và Windows trên đúng HEAD của PR phải thành công trước khi tạo tag annotated `web-v1.0.1`. Kết quả CI và commit đích ghi trong PR; tag Web đánh dấu commit của PR và không thay cho thao tác merge. PR để mở cho người dùng duyệt.
