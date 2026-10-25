# Phone Store Admin cho Windows

Nhấp đúp **Phone Store Admin** trên Desktop, nhập tên đăng nhập và mật khẩu. Kết nối được lấy từ cấu hình đã thiết lập, không nhập URL ở màn hình đăng nhập. Thay đổi khi cần trong **Cài đặt kết nối**. Không lưu mật khẩu đăng nhập.

## Bản portable và điều kiện

Thư mục phát hành: `dist/Phone Store Admin`. File mở trực tiếp: `PhoneStore.Admin.exe`. Có icon điện thoại và backend publish trong thư mục `backend`. Bản win-x64 self-contained gồm runtime .NET, không cần VS Code/SDK để chạy. Vẫn cần SQL Server đang hoạt động, schema và kết nối hợp lệ; self-contained không kèm SQL Server.

Khởi tạo CSDL/migration là bước riêng theo README chính hoặc môi trường QA theo docs/qa/README.md. Launcher không chạy migration, seed, reset hoặc xóa dữ liệu. Tài khoản phải thuộc đúng CSDL/môi trường; không dùng tài khoản QA cho backend thông thường.

## Thiết lập ban đầu trên máy hiện tại

Người thiết lập có Node và SDK chạy một lần từ repository:

```powershell
node scripts/publish-admin.mjs
node scripts/install-admin.mjs --qa
```

Thao tác này dùng manifest QA cục bộ để cấu hình đúng backend/CSDL hiện có, tạo shortcut Desktop. Không sao chép tài khoản, mật khẩu hoặc private.json vào bản publish. Các lần sử dụng sau chỉ nhấp đúp shortcut, không dùng terminal. Script tạo shortcut có thể chạy lại. Bản portable có bản sao install-admin.mjs; dùng `--qa-file <đường dẫn manifest QA>` nếu chạy ngoài repository.

Cấu hình người dùng nằm ở `%LOCALAPPDATA%\PhoneStore\Admin\connection.json`; kết nối CSDL cho backend local nằm riêng ở `backend.json` cùng thư mục, ngoài release, không ghi vào báo cáo. Mở từ thư mục khác vẫn dùng đường dẫn tuyệt đối. Không chép các tệp cấu hình có bí mật cho người khác.

## Máy chủ từ xa hoặc môi trường thông thường

Chuẩn bị connection-file theo cấu trúc AdminSettings: ApiBaseUrl, Environment, TargetFingerprint, StartLocalBackend=false, BackendExecutable="", BackendConfiguration=""; chạy install-admin.mjs --connection-file <file>. Backend từ xa phải dùng HTTPS, đúng host máy chủ (localhost chỉ trỏ máy hiện tại). TargetFingerprint lấy từ health check đã xác minh của backend đích. Không khởi động backend local trong chế độ này.

Với local thông thường, cấu hình StartLocalBackend=true, BackendExecutable trỏ backend publish, BackendConfiguration là đường dẫn tuyệt đối tới JSON có ConnectionStrings__Store. Environment=Local, URL loopback cổng ổn định do người thiết lập chọn. Không có địa chỉ QA tạm hard-code trong cấu hình phát hành mặc định. Schema/tài khoản được thiết lập riêng, không tự tạo khi mở app.

## Vòng đời và lỗi kết nối

Launcher kiểm tra `/health/ready`: ứng dụng, môi trường và fingerprint server/CSDL phải khớp; health check kiểm tra truy vấn schema thật. Backend đúng đã chạy được dùng lại. Nếu chưa chạy, khởi động executable publish ẩn, chờ sẵn sàng rồi đăng nhập. App chỉ cho một phiên tương tác; khóa liên tiến trình tránh khởi động trùng backend. Cổng bị chiếm bởi dịch vụ không đúng sẽ báo lỗi, không kill/chuyển sang dịch vụ đó.

Backend được chia sẻ với website nên tiếp tục chạy khi đóng Admin; các lần mở tiếp theo dùng lại. Không tạo một process mới mỗi lần. Khi nâng cấp backend, người vận hành dừng đúng backend của mình trong khoảng bảo trì; launcher không dừng bất kỳ server đang dùng chung nào.

Lỗi mất mạng, timeout, sai môi trường, CSDL chưa sẵn sàng, sai tài khoản, tài khoản bị khóa và lỗi server có thông báo riêng. Nếu CSDL chưa có schema hoặc SQL Server tắt, kiểm tra dịch vụ và bước thiết lập; không đổi sang PhoneStore để né lỗi QA.

## Bằng chứng nghiệm thu

Xem `docs/qa/ADMIN_PACKAGING_REPORT.md` trong repository cho ca đã chạy và giới hạn. Chưa coi chạy trên máy hiện tại có SDK là bằng chứng máy sạch không SDK; không tự đóng VS Code của người dùng để giả lập kiểm thử đó.
