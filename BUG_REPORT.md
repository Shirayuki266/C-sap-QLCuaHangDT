# Bug report

Chỉ liệt kê lỗi đã tái hiện; không coi lỗi môi trường/harness là lỗi ứng dụng. Môi trường chung: Windows/.NET10, SQL Server QA riêng, MVC/API trên loopback, Chrome headless và WinForms ẩn. Không có thông tin đăng nhập/token trong bằng chứng.

## BUG-QA-001 — Form số lượng sai kiểu bị xử lý thành lệnh xóa giỏ

| Thuộc tính | Nội dung |
| --- | --- |
| Use case/module | UC03 |
| Mức độ | High — Đầu vào không hợp lệ làm mất dòng giỏ đã lưu, chặn luồng mua hàng. |
| Điều kiện | Đăng nhập CUSTOMER; giỏ QA có một dòng Quantity=1. |
| Bước tái hiện | POST /Cart/Update cùng VariantId với Quantity=abc, 1.5 hoặc vượt Int32; đọc lại /api/cart. |
| Mong đợi | Báo lỗi nhập số nguyên; giỏ giữ nguyên. |
| Thực tế trước sửa | Dòng bị xóa vì model binding thất bại trả Quantity mặc định 0. |
| Bằng chứng trước sửa | docs/qa/before-fixes.json |
| Nguyên nhân gốc | Controller không kiểm tra ModelState trước SetCart; Quantity=0 mang nghĩa xóa. |
| File/phần sửa | src/PhoneStore.Web/Controllers/CartController.cs — Add/Update |
| Sửa đổi | Dừng xử lý khi ModelState không hợp lệ, hiển thị lỗi và không gọi dịch vụ ghi. |
| Test chứng minh | QA-MVC-CART, UC03-004, UC03-005 |
| Hồi quy | Thêm/sửa/xóa hợp lệ; âm/0/không nguyên; checkout MVC/Chrome. |
| Trạng thái | Verified |
| Sau sửa | QA-MVC-CART: PASS; UC03-004: PASS; UC03-005: PASS |

## BUG-QA-002 — Giá thẻ và thứ tự giá lấy biến thể nằm ngoài bộ lọc

| Thuộc tính | Nội dung |
| --- | --- |
| Use case/module | UC02 |
| Mức độ | Medium — Khách lọc dung lượng/giá nhưng thấy mức giá không áp dụng cho lựa chọn đó. |
| Điều kiện | Sản phẩm QA có 128GB giá 1 triệu và 256GB giá 2 triệu. |
| Bước tái hiện | GET catalog lọc StorageGb=256, MinPrice=1500000, MaxPrice=2500000 và mã sản phẩm. |
| Mong đợi | Giá từ và sort dùng biến thể khớp lọc: 2 triệu. |
| Thực tế trước sửa | Sản phẩm khớp nhưng Variants trả cả 128GB; min giá hiển thị còn 1 triệu. |
| Bằng chứng trước sửa | docs/qa/before-fixes.json |
| Nguyên nhân gốc | Lọc sản phẩm bằng Any, sau đó Min/map trên toàn bộ biến thể đang bán. |
| File/phần sửa | src/PhoneStore.Infrastructure/Services/CatalogCartService.cs — Catalog |
| Sửa đổi | Áp dụng cùng điều kiện giá/dung lượng cho sort và tập biến thể public trả về. |
| Test chứng minh | QA-FILTER, UC02-007, UC02-008 |
| Hồi quy | Kết hợp bộ lọc, giá biên, sort/phân trang và danh mục quản trị. |
| Trạng thái | Verified |
| Sau sửa | QA-FILTER: PASS; UC02-007: PASS; UC02-008: PASS |

## BUG-QA-003 — Hiển thị tiền làm tròn mất phần thập phân hợp lệ

| Thuộc tính | Nội dung |
| --- | --- |
| Use case/module | UC02/03/08/09/10 |
| Mức độ | Medium — Tiền được lưu đúng decimal nhưng giá/tổng nhìn thấy không khớp dữ liệu đã chốt. |
| Điều kiện | Biến thể QA có giá 100.50, nằm trong quy tắc decimal(18,2). |
| Bước tái hiện | Mở trang chi tiết, đọc phần giá hiển thị; đối chiếu giá DB/API. |
| Mong đợi | Hiện 100,50; không làm tròn thành số nguyên. |
| Thực tế trước sửa | N0 và Intl VND mặc định làm mất phần lẻ trên giao diện. |
| Bằng chứng trước sửa | docs/qa/before-fixes.json |
| Nguyên nhân gốc | Định dạng số nguyên được dùng cho mọi decimal. |
| File/phần sửa | src/PhoneStore.Core/Contracts/MoneyFormatting.cs; Views/*.cshtml; Admin/Ui.cs, Editors.cs, OrderReportScreens.cs; wwwroot/js/site.js |
| Sửa đổi | Formatter dùng N0 cho tiền nguyên, N2 cho phần lẻ; dùng chung Razor/WinForms/invoice/chart và cấu hình Intl tương ứng. |
| Test chứng minh | QA-MONEY, E2E-01, NATIVE-ADD-PURCHASE |
| Hồi quy | Giá có phần lẻ, tổng COD, phiếu nhập 160.50, số liệu nguyên và báo cáo. |
| Trạng thái | Verified |
| Sau sửa | QA-MONEY: PASS; E2E-01: PASS; NATIVE-ADD-PURCHASE: PASS |

## BUG-QA-004 — Lỗi API chưa đăng nhập và CSRF không cùng hợp đồng JSON

| Thuộc tính | Nội dung |
| --- | --- |
| Use case/module | API/xác thực |
| Mức độ | Medium — Client đọc JSON gặp response rỗng/ProblemDetails khác cấu trúc thay vì thông báo lỗi rõ ràng. |
| Điều kiện | Chưa login hoặc gửi mutation cookie không có CSRF. |
| Bước tái hiện | GET /api/profile anonymous; POST /api/cart thiếu CSRF; kiểm tra status và code/message. |
| Mong đợi | Giữ HTTP401/400, JSON code/message tiếng Việt. |
| Thực tế trước sửa | 401 rỗng; CSRF không có mã lỗi dùng chung. |
| Bằng chứng trước sửa | docs/qa/before-fixes.json |
| Nguyên nhân gốc | Cookie event chỉ gán status; MVC client-error mapping chạy trước filter định dạng CSRF. |
| File/phần sửa | src/PhoneStore.Web/Security/SessionEvents.cs; ApiAntiforgeryResultFilter.cs; Program.cs |
| Sửa đổi | Ghi JSON cho 401/403; ordered result filter trước mapping; chuẩn hóa lỗi MVC và status page API, giữ nguyên mã HTTP. |
| Test chứng minh | QA-ERR401, QA-CSRF |
| Hồi quy | Toàn bộ lỗi API có kiểm tra code/message; JSON sai, media type, quyền, thiếu trường, ID và method. |
| Trạng thái | Verified |
| Sau sửa | QA-ERR401: PASS; QA-CSRF: PASS |

## BUG-QA-005 — PUT giỏ thiếu mã biến thể vẫn báo thành công

| Thuộc tính | Nội dung |
| --- | --- |
| Use case/module | UC03/API |
| Mức độ | Low — Request thiếu trường định danh trả thành công sai, dù không ghi dữ liệu. |
| Điều kiện | CUSTOMER đã đăng nhập. |
| Bước tái hiện | PUT /api/cart với body chỉ Quantity=0. |
| Mong đợi | HTTP400 INVALID_INPUT. |
| Thực tế trước sửa | HTTP200 vì VariantId mặc định 0 được coi như xóa dòng không tồn tại. |
| Bằng chứng trước sửa | docs/qa/before-fixes.json |
| Nguyên nhân gốc | Nhánh Quantity=0 đi trước kiểm tra định danh. |
| File/phần sửa | src/PhoneStore.Infrastructure/Services/CatalogCartService.cs — SetCart |
| Sửa đổi | Kiểm tra VariantId dương trước mọi nhánh sửa/xóa. |
| Test chứng minh | QA-CART-ID, EP033-EMPTY |
| Hồi quy | Xóa hợp lệ và validation các endpoint giỏ. |
| Trạng thái | Verified |
| Sau sửa | QA-CART-ID: PASS; EP033-EMPTY: PASS |

## BUG-QA-006 — WinForms hiển thị thông báo mạng thô chưa Việt hóa

| Thuộc tính | Nội dung |
| --- | --- |
| Use case/module | WinForms/API client |
| Mức độ | Low — Người dùng nhận lỗi kỹ thuật thay vì hướng dẫn kết nối/timeout dễ hiểu. |
| Điều kiện | WinForms QA ẩn gọi địa chỉ loopback không có server. |
| Bước tái hiện | Tải ListScreen qua ApiClient tới cổng đóng; kiểm tra LastError và UI vẫn hoạt động. |
| Mong đợi | Thông báo Không thể kết nối, không treo/crash. |
| Thực tế trước sửa | Assertion Việt hóa thất bại; HttpRequestException truyền trực tiếp lên UI. |
| Bằng chứng trước sửa | docs/qa/native-before-fix.json |
| Nguyên nhân gốc | ApiClient không chuyển lỗi transport/timeout thành lỗi người dùng. |
| File/phần sửa | src/PhoneStore.Admin/ApiClient.cs |
| Sửa đổi | Thông báo mạng/timeout tiếng Việt, hướng dẫn tải lại trạng thái trước gửi lại; giữ inner exception cho chẩn đoán. |
| Test chứng minh | NATIVE-NETWORK, NATIVE-ASYNC |
| Hồi quy | WinForms tải dữ liệu, tạo sản phẩm/phiếu, kết nối hỏng và đóng form. |
| Trạng thái | Verified |
| Sau sửa | NATIVE-NETWORK: PASS; NATIVE-ASYNC: PASS |

## BUG-QA-007 — API preview giỏ âm trả 200 và âm thầm bỏ dòng

| Thuộc tính | Nội dung |
| --- | --- |
| Use case/module | UC03/API |
| Mức độ | Low — Validation preview và giỏ máy chủ không thống nhất; request lỗi bị che bằng kết quả rỗng. |
| Điều kiện | Guest gửi preview có quantity=-1. |
| Bước tái hiện | POST /api/guest-cart/preview với danh sách một dòng số lượng âm. |
| Mong đợi | HTTP400 cho đầu vào không hợp lệ. |
| Thực tế trước sửa | HTTP200 do Where lọc bỏ dòng âm. |
| Bằng chứng trước sửa | docs/qa/round-boundary-before.json |
| Nguyên nhân gốc | Dùng lọc dữ liệu thay vì validation request. |
| File/phần sửa | src/PhoneStore.Infrastructure/Services/CatalogCartService.cs — PreviewCart |
| Sửa đổi | Kiểm tra mã biến thể và Quantity cho từng dòng; không âm thầm bỏ dữ liệu sai. |
| Test chứng minh | EP037-BOUND, QA-MERGE |
| Hồi quy | Preview hợp lệ, giỏ guest, hợp nhất và lỗi JSON/null/media type. |
| Trạng thái | Verified |
| Sau sửa | EP037-BOUND: PASS; QA-MERGE: PASS |

## BUG-QA-008 — Website hiện lỗi fetch tiếng Anh khi mất mạng

| Thuộc tính | Nội dung |
| --- | --- |
| Use case/module | UC03/Website |
| Mức độ | Low — Thông báo không thống nhất tiếng Việt trong nhánh mất kết nối. |
| Điều kiện | Giỏ guest đã có một dòng; Chrome chuyển offline bằng CDP. |
| Bước tái hiện | Bấm Cập nhật giỏ khi offline; kiểm tra thông báo và localStorage. |
| Mong đợi | Thông báo mạng tiếng Việt và giữ nguyên giỏ tạm. |
| Thực tế trước sửa | Assertion thông báo Việt hóa/giữ giỏ không đạt; nhánh fetch truyền trực tiếp thông báo TypeError. |
| Bằng chứng trước sửa | docs/qa/browser-offline-before.txt |
| Nguyên nhân gốc | api helper chỉ xử lý HTTP response, không chuyển lỗi fetch transport. |
| File/phần sửa | src/PhoneStore.Web/wwwroot/js/site.js — api |
| Sửa đổi | Bắt lỗi fetch và trả hướng dẫn mạng tiếng Việt; không xóa localStorage. |
| Test chứng minh | BROWSER-005 |
| Hồi quy | Guest offline → online → login/merge → checkout → hủy; XSS và JS exceptions. |
| Trạng thái | Verified |
| Sau sửa | BROWSER-005: PASS |

## Ghi nhận harness/môi trường

- Proxy timeout ở lần đầu ném lỗi khi đóng kết nối client đã timeout. Đây là cleanup của bộ thử, không phải lỗi đơn hàng. Chỉ bỏ qua IOException/HttpListenerException sau khi đã xác minh upstream commit; assertion timeout, cùng ID và giữ kho một lần vẫn giữ nguyên. SEC-015 đã chạy lại.
- Runner mới trả exit code khác0 khi có FAIL; log request gắn testId, không ghi body/cookie/token. Chrome dùng profile và cổng debug riêng, không chạm phiên trình duyệt người dùng.
- PowerShell ExecutionPolicy chặn script .ps1; không thay đổi policy. Thiết bị in vật lý chưa được nghiệm thu. Đây là BLOCKED, không phải PASS.
