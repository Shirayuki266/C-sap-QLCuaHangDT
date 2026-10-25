# Phone Store — Quản lý và bán điện thoại di động

C# / .NET 10: website khách hàng ASP.NET Core MVC, WinForms quản trị, API ASP.NET Core dùng chung và SQL Server. Các chức năng đọc/ghi CSDL thật; không còn adapter danh mục hard-code trong runtime. Dữ liệu seed là dữ liệu thử, giá và ảnh không đại diện cửa hàng thực tế.

## Yêu cầu môi trường

- Windows và .NET 10 SDK (máy triển khai đang dùng SDK 10.0.401).
- SQL Server cục bộ hoặc máy chủ truy cập được. Mặc định instance `localhost`, Windows Authentication, CSDL `PhoneStore`.
- Tài khoản chạy migration có quyền tạo DB/bảng; tài khoản chạy server cần quyền đọc/ghi DB và `sp_getapplock`. WinForms chỉ kết nối API, không cần quyền SQL.
- Node.js 22+ và Chrome chỉ cần khi chạy bộ kiểm tra trình duyệt.

## Cài đặt và tạo CSDL

Chạy PowerShell từ thư mục chứa `PhoneStore.slnx`:

```powershell
dotnet restore PhoneStore.slnx
dotnet build PhoneStore.slnx --no-restore

# Tùy chọn, nếu dùng instance khác:
$env:ConnectionStrings__Store = 'Server=localhost;Database=PhoneStore;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'

# Tạo CSDL nếu chưa có, áp dụng migration rồi nạp dữ liệu mẫu:
dotnet run --project src/PhoneStore.Web -- --migrate --seed
```

Migration SQL tại `src/PhoneStore.Infrastructure/Migrations/001_Initial.sql`, gồm 16 bảng nghiệp vụ và bảng phiên `PHIEN`. Runner ghi phiên bản và SHA-256 trong `__PhoneStoreMigrations`, khóa transaction khi chạy và bỏ qua bản đã áp dụng. Chạy lại không tạo trùng bảng. Không chỉnh script đã áp dụng; thêm file migration có thứ tự tiếp theo khi thay đổi schema.

Seed chỉ chạy khi bảng tài khoản trống, không ghi đè dữ liệu đang có. Có 2 tài khoản, 3 hãng, 1 danh mục, 3 điện thoại với 4 biến thể, 1 NCC và phiếu nhập đã xác nhận có nhật ký; một biến thể hết hàng để thử lỗi. Tồn ban đầu được ghi kèm phiếu nhập, không sửa từ website. Trên máy hiện tại đã có thêm đơn hoàn tất và đơn hủy từ kiểm thử HTTP/trình duyệt để xem báo cáo.

| Vai trò | Tên đăng nhập | Mật khẩu mẫu |
|---|---|---|
| Admin — WinForms | `admin` | `Admin@12345` |
| Khách hàng — website | `khachhang` | `Khach@12345` |

Có thể đặt `Seed__AdminPassword` và `Seed__CustomerPassword` trước lần seed đầu tiên. Mật khẩu trong CSDL được băm PBKDF2 với salt qua PasswordHasher; các mật khẩu trên chỉ phục vụ chạy thử. Đổi bằng màn hình Tài khoản trước khi sử dụng dữ liệu thật. Ngoài Development, seed yêu cầu cấu hình cả hai mật khẩu thay vì dùng mặc định.

## Chạy hai ứng dụng

Terminal thứ nhất:

```powershell
dotnet run --no-build --project src/PhoneStore.Web --urls http://localhost:5080
```

Mở http://localhost:5080. API nằm tại `/api`, quản trị tại `/api/admin`. Terminal thứ hai:

```powershell
$env:PHONESTORE_API_URL = 'http://localhost:5080'
dotnet run --project src/PhoneStore.Admin
```

WinForms mở màn hình đăng nhập có thể sửa địa chỉ API. Tài khoản Customer bị từ chối khi đăng nhập quản trị. Nếu dùng máy chủ từ xa, WinForms yêu cầu HTTPS. Khi build lại, dừng các tiến trình đang chạy để tránh khóa DLL.

## Thử luồng chính

1. Đăng ký hoặc đăng nhập khách; mở điện thoại, chọn màu/dung lượng, thêm giỏ. Giỏ chưa đăng nhập nằm trong localStorage và hợp nhất sau login.
2. Xem giỏ, sửa/xóa; mở checkout, nhập người nhận, SĐT, địa chỉ và đặt COD. Đơn mới giữ hàng, chưa trừ tồn thực.
3. Mở WinForms → Đơn hàng → chọn đơn → Xem/Xử lý → Xác nhận → Xuất giao → Hoàn tất và thu COD.
4. Xuất giao giảm cả tồn thực và giữ. Báo cáo chỉ tính đơn hoàn tất và đã thu, theo ngày hoàn tất UTC+7.
5. Với đơn chờ, khách hoặc Admin hủy để hoàn giữ; Admin được hủy thêm ở trạng thái đã xác nhận. Hủy/giao thất bại cần lý do.
6. Giao thất bại không tăng kho; bấm Nhận lại khi đã thực nhận toàn bộ hàng, chỉ được một lần.
7. Điện thoại → Thêm → nhập biến thể và lượng cần nhập → lưu → phiếu nhập điền sẵn hàng/lượng. Chọn NCC và giá vốn, lưu nháp rồi xác nhận tại danh sách phiếu. Tồn chỉ tăng sau xác nhận.
8. Tồn kho → kiểm kê có lý do; giá nhập quản lý ở phiếu, giá bán ở sản phẩm. Hãng/danh mục/NCC dùng ngừng hoạt động, không xóa lịch sử.

Website có hồ sơ, đổi mật khẩu, lọc lịch sử và chi tiết/timeline đơn. WinForms có hãng/danh mục, quản lý khách hàng và khóa/mở phiên, NCC, phiếu nháp nhiều dòng, nhật ký kho, tìm/lọc đơn, hóa đơn qua PrintPreview/Print và ba loại báo cáo có bảng/biểu đồ.

## Kiểm thử

```powershell
# 8 kiểm tra miền độc lập:
dotnet run --project tests/PhoneStore.Checks

# Thêm 20 nhóm kiểm thử SQL (cần quyền tạo CSDL):
dotnet run --project tests/PhoneStore.Checks -- --integration

# Thêm 6 nhóm HTTP/MVC, cần server đang chạy:
dotnet run --project tests/PhoneStore.Checks -- --api

# Mở ẩn và tải dữ liệu 11 màn hình WinForms, chụp các form:
dotnet run --project src/PhoneStore.Admin -- --smoke

# Chrome headless: 10 kiểm tra website desktop/mobile và luồng giỏ/COD/hủy:
node scripts/browser-checks.mjs
```

SQL checks luôn tạo CSDL riêng `PhoneStore_Checks_<timestamp>_<suffix>` và giữ lại để đối chiếu. Không xóa DB ứng dụng. Có thể đặt `PHONESTORE_TEST_CONNECTION` cho server SQL khác; tên DB thử vẫn tự sinh. HTTP checks tạo tài khoản `verify...` và đơn COD hoàn tất trong DB của server đang chạy; chỉ chạy với dữ liệu thử. Chrome checks dùng tài khoản `khachhang`, tạo rồi hủy một đơn, giữ nhật ký. `PHONESTORE_ADMIN_PASSWORD` cho phép đổi mật khẩu đăng nhập của các bộ kiểm tra; Chrome sử dụng mật khẩu seed mẫu của khách.

Log/ảnh lần chạy mới ở `.artifacts/` (gitignore). Bằng chứng đã lưu tại [docs/verification](docs/verification), [ảnh giao diện](docs/screenshots), và [báo cáo kiểm thử](docs/TESTING.md).

## Sao lưu và khôi phục

Chạy trên máy SQL Server bằng tài khoản có quyền backup/restore. Đường dẫn là đường dẫn phía SQL Server, tài khoản dịch vụ SQL cần đọc/ghi được.

```powershell
# Mặc định ghi file mới theo timestamp trong thư mục backup của SQL Server:
./scripts/BackupRestore.ps1 -Mode Backup -Database PhoneStore

./scripts/BackupRestore.ps1 -Mode Verify -BackupFile 'C:\SqlBackups\PhoneStore_20261025.bak'

# Chỉ khôi phục vào CSDL mới, từ chối DB đích đã có; không dùng REPLACE:
./scripts/BackupRestore.ps1 -Mode Restore -BackupFile 'C:\SqlBackups\PhoneStore_20261025.bak' -RestoreDatabase PhoneStore_Restored_Test
```

Thêm `-Server` nếu không dùng localhost. Đã thử backup+VERIFYONLY và restore trên DB kiểm thử riêng. Sau khôi phục, đối chiếu dữ liệu trước khi tự đổi cấu hình ứng dụng sang DB mới. Script không chuyển cấu hình hoặc ghi đè DB hiện hành.

## Kiến trúc, phạm vi và giới hạn xác minh

Xem [REQUIREMENTS.md](REQUIREMENTS.md), [PROGRESS.md](PROGRESS.md), [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Không có quản trị website thay thế WinForms.

Các luồng SQL/HTTP và Chrome đã chạy; WinForms đã kiểm tra tải dữ liệu, kết buộc dữ liệu và render form. Chưa tự động bấm mọi thao tác WinForms hay kiểm tra máy in vật lý; cần nghiệm thu tương tác desktop và in trên thiết bị đích. Chưa benchmark tải lớn hoặc triển khai HTTPS production. Development dùng khóa Data Protection tạm, nên restart server sẽ hủy cookie/token cũ; production cần kho khóa bền vững được bảo vệ, chứng chỉ hợp lệ và cấu hình kết nối riêng (không dùng TrustServerCertificate với máy chủ chưa được tin cậy).

## QA mới nhất — 25/10/2026

Đợt QA này sửa và kiểm tra lại 8 lỗi. Runner cách ly đạt 336/336 ca, gồm 6 E2E; Chrome 12 kiểm tra con, WinForms 4 kiểm tra con (đã nằm trong tổng runner), cùng 8 unit checks riêng. Build Release từng project và solution: 0 warning/error.

Kết quả mới nhất: [TEST_REPORT.md](TEST_REPORT.md), [BUG_REPORT.md](BUG_REPORT.md), [QA_CHECKLIST.md](QA_CHECKLIST.md), [REQUIREMENTS_TRACEABILITY.md](REQUIREMENTS_TRACEABILITY.md), [API_INVENTORY.md](API_INVENTORY.md). Các nhận xét kiểm thử phía trên là lịch sử. Chưa nghiệm thu toàn bộ: 46 NOT RUN và 2 BLOCKED, chi tiết trong TEST_REPORT.

Chạy QA trên Windows có .NET 10, SQL Server với quyền tạo CSDL, Node 22+ và Chrome:

```powershell
dotnet restore PhoneStore.slnx
dotnet build PhoneStore.slnx -c Release --no-restore
dotnet run --no-build -c Release --project tests/PhoneStore.Qa -- --children
node scripts/qa-static.mjs
node scripts/qa-report.mjs
```

Runner tạo CSDL PhoneStore_QA_..., credential ngẫu nhiên trong bộ nhớ và server trên cổng riêng; tự dừng process do nó tạo. Giữ DB QA để đối chiếu, không sửa PhoneStore. Có thể đặt PHONESTORE_TEST_CONNECTION cho server SQL kiểm thử; tên DB luôn được thay bằng tên QA mới. Chrome dùng profile/cổng debug riêng. Log an toàn ở .artifacts/qa và docs/qa; exit code khác 0 nếu FAIL. Có khoảng chờ để kiểm tra rate limit thực tế.

Các lệnh API/HTTP/browser cũ có thể mặc định dùng PhoneStore, nên dùng runner mới cho hồi quy cách ly. Đợt này script backup bị ExecutionPolicy chặn; không thay đổi chính sách máy. In thiết bị vật lý chưa kiểm chứng.

## Môi trường QA cấu hình riêng

Hướng dẫn chuẩn và sáu hồ sơ mới ở [docs/qa/README.md](docs/qa/README.md). Dùng `node scripts/qa.mjs init`, `node scripts/qa.mjs serve`, `node scripts/qa.mjs test`; bộ cũ chạy qua `node scripts/qa.mjs legacy`. Các fallback test về PhoneStore/5080 đã bị loại bỏ; thiếu hoặc sai cấu hình QA thì dừng trước ghi. URL tương tác trong đợt này http://localhost:65377; đọc manifest để lấy URL mới nhất. Tài khoản ngẫu nhiên chỉ lưu cục bộ trong .artifacts/qa-env/private.json, không đưa vào báo cáo. Cleanup chỉ chạy khi gọi rõ ràng `node scripts/qa.mjs cleanup`, sau khi dừng server QA.

Số liệu QA mới nhất nằm trong docs/qa/TEST_REPORT.md; các số liệu cũ ở phần trên là lịch sử.

## Bản Admin Windows nhấp đúp

Đã publish win-x64 self-contained tại `dist/Phone Store Admin/PhoneStore.Admin.exe` và tạo shortcut **Phone Store Admin** trên Desktop. Chỉ cần nhấp shortcut và đăng nhập; URL được lưu trong LocalAppData, không nhập mỗi lần. Backend local được tự mở từ bản publish, kiểm tra đúng môi trường/CSDL trước khi đăng nhập và được giữ khi đóng Admin để website tiếp tục dùng chung.

Xem [hướng dẫn Windows](docs/ADMIN_WINDOWS.md) và [báo cáo đóng gói](docs/qa/ADMIN_PACKAGING_REPORT.md). Vẫn cần SQL Server và schema đã thiết lập; không tự seed/reset khi mở app. Script publish/thiết lập shortcut: `scripts/publish-admin.mjs`, `scripts/install-admin.mjs`. Không có tài khoản/mật khẩu QA trong thư mục phát hành. Các lệnh dotnet run ở trên chỉ dành cho phát triển.

## Website khách hàng cập nhật

Trang chủ: http://localhost:65377/ · Sản phẩm: http://localhost:65377/Catalog · Tìm kiếm: http://localhost:65377/Search?Query=iphone. Header chỉ có một link đăng nhập cho khách, có gợi ý từ SQL; trang chủ tách bộ lọc sang danh mục. Bán chạy tính theo đơn hoàn tất đã thu trong30 ngày lịch gần nhất (UTC+7), không hiển thị doanh thu nội bộ.

Migration002 bổ sung SAN_PHAM_ANH, giữ cột ImageUrl và dữ liệu cũ. Ảnh mẫu HTTPS có nguồn/giấy phép tại /Home/ImageCredits; không gán ảnh sai cho model chưa xác minh được. COD giữ nguyên; QR chỉ là lựa chọn disabled “Sắp ra mắt”, request QR bị backend từ chối.

Báo cáo đầy đủ và21 tiêu chí: [WEB_DISCOVERY_REPORT.md](docs/qa/WEB_DISCOVERY_REPORT.md). Đã chạy349/349 ca hồi quy,22 kiểm tra Chrome con và12 kiểm tra Chrome chỉ đọc trên website đã kích hoạt. Backend publish hiện tại nằm trong dist/Phone Store Customer Web; launcher Admin đã cấu hình dùng backend đó trên cùng DB QA. Không cần chạy seed/migrate mỗi lần mở ứng dụng.
