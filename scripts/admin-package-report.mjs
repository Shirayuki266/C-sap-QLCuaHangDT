import fs from 'node:fs';
const root='docs/qa';const read=p=>{const b=fs.readFileSync(p);return (b[0]===255&&b[1]===254?b.subarray(2).toString('utf16le'):b.toString('utf8')).replace(/^\uFEFF/,'');};
const pkg=JSON.parse(read('.artifacts/admin-package/results.json')),ui=JSON.parse(read('.artifacts/admin-package/ui-results.json')),runtime=JSON.parse(read('.artifacts/qa/results.json'));
if(fs.existsSync('.artifacts/admin-package/server-error-ui.json'))fs.writeFileSync(root+'/admin-server-error-ui.json',read('.artifacts/admin-package/server-error-ui.json'));
if(fs.existsSync('.artifacts/admin-package/backend-processes.json'))fs.writeFileSync(root+'/admin-backend-processes.json',read('.artifacts/admin-package/backend-processes.json'));
for(const [from,to] of [['.artifacts/admin-package/results.json','admin-package-results.json'],['.artifacts/admin-package/ui-results.json','admin-ui-results.json'],['.artifacts/admin-package/before-final-fixes.json','admin-package-before-final-fixes.json'],['.artifacts/qa-env/publish-admin.txt','admin-publish.txt'],['.artifacts/qa-env/publish-admin-final.txt','admin-publish-final.txt']])if(fs.existsSync(from))fs.writeFileSync(root+'/'+to,read(from));
const report=`# Bàn giao WinApp Phone Store Admin — 25/10/2026

Đã tạo bản portable Windows x64 self-contained và shortcut Desktop, cấu hình QA đúng CSDL hiện có. Không cần VS Code/terminal/SDK cho mỗi lần sử dụng. Vẫn cần SQL Server; không coi portable là bộ cài SQL hoặc nghiệm thu máy sạch.

## Đường dẫn sử dụng

- Shortcut: C:\\Users\\PC\\OneDrive\\Máy tính\\Phone Store Admin.lnk
- Executable: D:\\C-sap-QLCuaHangDT\\dist\\Phone Store Admin\\PhoneStore.Admin.exe
- Backend publish: thư mục backend bên cạnh executable.
- Cấu hình người dùng: %LOCALAPPDATA%\\PhoneStore\\Admin\\connection.json; backend.json riêng cho kết nối CSDL. Không lưu mật khẩu đăng nhập.
- Môi trường đã chọn: QA; API http://localhost:65377; server localhost; DB PhoneStore_QA_20261025_061611_2722bd. Website và Admin cùng backend/DB này. Không chuyển sang PhoneStore.

Trước sửa, LoginForm mặc định5080, khác manifest QA65377. Không kết luận mật khẩu sai. Bản mới đọc cấu hình theo người dùng, không phụ thuộc cwd; đăng nhập chỉ có username/password, cài đặt URL nằm riêng. Default phát hành không hard-code cổng QA tạm.

## Lệnh thực hiện

node scripts/publish-admin.mjs; dotnet publish Admin -c Release -r win-x64 --self-contained true (các lần chỉnh Admin); node scripts/install-admin.mjs --qa (đã chạy lặp); node scripts/admin-package-checks.mjs; node scripts/admin-package-checks.mjs --retry-failed; node scripts/admin-ui-checks.mjs; dotnet build PhoneStore.slnx -c Release; dotnet run --no-build -c Release --project tests/PhoneStore.Qa -- --children.

## Kết quả

- Packaging: ${pkg.filter(x=>x.status==='PASS').length}/${pkg.length} PASS. Bằng chứng admin-package-results.json. Retry chỉ chạy các ca còn FAIL; các PASS trước đó được giữ và ghi PREVIOUS PASS, không giả chạy lại tất cả.
- UI Automation từ shortcut thật: ${ui.filter(x=>x.status==='PASS').length}/${ui.length} PASS; admin-ui-results.json. Bổ sung UI-SERVER-500-DISTINCT PASS ở admin-server-error-ui.json: form thật gọi server thử500 với credential giả, không đụng tài khoản thật. Sau kiểm tra đã mở lại màn hình đăng nhập QA cho người dùng.
- Hồi quy hệ thống: ${runtime.filter(x=>x.Status==='PASS').length}/${runtime.length} PASS; FAIL=${runtime.filter(x=>x.Status==='FAIL').length}; results.json. Các bộ UI con đã nằm trong tổng runner, không cộng trùng.
- Quét bản phát hành cuối: 848 tệp khoảng254MiB; không có private.json hoặc giá trị credential QA ngẫu nhiên (cả UTF-8/UTF-16). Có coreclr/hostfxr/WindowsDesktop/AspNetCore và includedFrameworks; runtime self-contained.
- Build cuối: xem build.txt. Các công cụ WiX/Inno/NSIS không tìm thấy trên PATH; bàn giao portable và script shortcut, không tuyên bố có MSI.

| Ca nghiệm thu | Trạng thái | Bằng chứng/phạm vi |
| --- | --- | --- |
| Đóng VS Code rồi mở bằng Desktop | NOT RUN | Không tự đóng IDE/công việc người dùng. Shortcut đã mở qua Windows Shell ngoài IDE; VS Code vẫn mở. |
| Backend chưa chạy tự khởi động đúng local | PASS | PKG-START-LOCAL; executable publish, không dotnet run. |
| Backend đã chạy không tạo thêm | PASS | PKG-REUSE-PARALLEL; health/fingerprint đúng đích; admin-backend-processes.json xác minh chỉ1 backend publish sau nhiều lần mở. |
| Nhấp shortcut nhiều lần | PASS | UI-REPEATED-SHORTCUT-ONE-INSTANCE; còn đúng1 process Admin publish. |
| Đăng nhập đúng môi trường | PASS | UI-QA-ADMIN-LOGIN, health/fingerprint + QA identity. |
| Sai mật khẩu | PASS | UI-WRONG-PASSWORD-MESSAGE, label WinForms thật. |
| Timeout khác lỗi mật khẩu | PASS | PKG-TIMEOUT; thông báo riêng từ launcher. |
| Lỗi HTTP500 hiển thị trong WinForms | PASS | UI-SERVER-500-DISTINCT: form publish gọi backend thử500, hiển thị lỗi server khác sai mật khẩu. |
| CSDL không khả dụng | PASS | PKG-DATABASE-UNAVAILABLE: backend publish dùng DB QA không tồn tại, health503, không tạo schema và báo lỗi CSDL. |
| Cổng bị dịch vụ khác chiếm | PASS | PKG-COLLISION; dịch vụ thử vẫn sống, không kill/kết nối nhầm. |
| Đường dẫn khoảng trắng/tiếng Việt | PASS | Executable trong Phone Store Admin; shortcut nằm Máy tính, cwd Thử mở ứng dụng; UI mở/đăng nhập thật. |
| Cấu hình giữ sau đóng/mở | PASS | UI đóng/mở lại, file không đổi qua launcher checks; config nằm LocalAppData. |
| Website/Admin dùng chung dữ liệu | PASS | QA identity đúng DB; catalog SQL và admin login cùng URL, không seed riêng. |
| Đóng Admin giữ backend chung | PASS | UI-CLOSE-KEEPS-SHARED-BACKEND; health còn sẵn sàng sau CloseMainWindow. |
| Không secret QA trong phát hành | PASS | PKG-NO-QA-SECRETS và scan lại trên binary cuối. Không xuất giá trị secret. |
| Máy sạch không có SDK/runtime | NOT RUN | Chỉ kiểm tra PATH không có dotnet + DOTNET_ROOT không tồn tại trên máy hiện tại; không thay cho VM sạch. |

Các trạng thái ở đây riêng cho đóng gói; QA tổng thể vẫn có NOT RUN/BLOCKED trong TEST_REPORT.md.

## Lỗi đã phát hiện và sửa khi triển khai

1. Cổng local trống có thể gây TCP/HTTP timeout trước khi launcher kịp chạy backend. Sửa dùng danh sách listener Windows cho loopback, sau đó health check xác minh đúng dịch vụ. Cold start và mở song song đã đạt.
2. Client health deadline3s bằng deadlineSQL3s khiến lỗi DB bị che thành timeout. Client dùng8s để nhận health503; PKG-DATABASE-UNAVAILABLE chạy lại đạt.
3. Assert fingerprint của bộ thử so sánh phân biệt hoa/thường dù launcher đúng; sửa harness, không coi là lỗi ứng dụng. Assertion chuỗi khóa tài khoản phải parse JSON và giữ Unicode khi truyền qua shell, dùng escape C# để tránh chữ thành dấu hỏi; không bỏ assertion.

## Vòng đời và bảo vệ

App tương tác dùng mutex để chỉ có một cửa sổ; mở lặp đưa cửa sổ cũ lên trước. Launcher khóa liên tiến trình cho cùng URL, kiểm tra ứng dụng/môi trường/fingerprint/schema trước đăng nhập. Không kill dịch vụ khác. Backend tự khởi động chạy ẩn và được dùng chung với website, tiếp tục sống khi Admin đóng; lần sau dùng lại. Không có migration/seed/reset trong đường khởi động này.

QA test DB thiếu là DB tên QA không tồn tại, chỉ health read; không tạo/xóa nó. Test không dừng SQL Server người dùng. Backend lỗi thử do test tạo mới được test tự dừng; backend QA dùng chung được giữ lại.

## Tệp và cấu hình sửa

AdminSettings.cs, BackendLauncher.cs, ConnectionSettingsForm.cs, LoginForm.cs, Program.cs, ApiClient.cs, adminsettings.json, csproj và icon; Web/Program.cs (health/Local); StoreService.Login (thông báo khóa sau xác minh mật khẩu); EndpointCases.cs (assert message khóa). Thêm publish-admin.mjs, install-admin.mjs, admin-package-checks.mjs, admin-ui-checks.mjs, admin-package-report.mjs và docs/ADMIN_WINDOWS.md. .gitignore bỏ qua dist. Không reset/clean/commit/push hoặc thay connection mặc định.
`;
fs.writeFileSync(root+'/ADMIN_PACKAGING_REPORT.md',report);console.log('Published ADMIN_PACKAGING_REPORT.md');
const progressPath=root+'/PROGRESS.md';const marker='\n## WinApp Windows nhấp đúp\n';const progress=read(progressPath).split(marker)[0];fs.writeFileSync(progressPath,progress+marker+'\nĐã publish Admin/backend win-x64 self-contained, tạo shortcut Desktop và cấu hình LocalAppData trỏ đúng QA hiện có. Packaging10 ca, UI shortcut6 ca và UI HTTP500 đạt; hồi quy toàn hệ thống '+runtime.filter(x=>x.Status==='PASS').length+'/'+runtime.length+' đạt. Xem ADMIN_PACKAGING_REPORT.md. Chưa thử đóng VS Code và máy sạch không SDK; không tự đóng công việc người dùng. Các NOT RUN/BLOCKED QA tổng thể ở trên vẫn còn hiệu lực.\n');
