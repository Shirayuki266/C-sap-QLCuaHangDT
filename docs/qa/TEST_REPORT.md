# Báo cáo kiểm thử QA — 25/10/2026

**Chưa tuyên bố hoàn tất toàn bộ phạm vi QA.** Các trường hợp đã chạy đạt theo bằng chứng bên dưới; vẫn còn NOT RUN/BLOCKED, đặc biệt nghiệm thu mọi nhánh UI, thiết bị in và script backup. Không dùng số lượng test đã PASS để suy ra các trường hợp chưa chạy.

## Mã nguồn và môi trường

Baseline git: ccd7b0e (Initial commit); ứng dụng và thay đổi hiện có đang ở working tree chưa commit. Không reset/stash hoặc ghi đè thay đổi của người dùng. Danh sách SHA-256 các tệp nguồn cuối ở source-hashes.json.

| Thuộc tính | Giá trị |
| --- | --- |
| database | PhoneStore_QA_20261025_083708_26baf7 |
| url | http://localhost:59219 |
| sdk | 10.0.401 |
| os | Microsoft Windows NT 10.0.22631.0 |
| configuration | Release |
| baselineCommit | ccd7b0e + uncommitted workspace; see source hashes |

Không dùng DB PhoneStore hoặc server của người dùng. Runner tạo DB mới PhoneStore_QA_..., chọn cổng loopback riêng, seed credential ngẫu nhiên chỉ trong bộ nhớ, tự dừng process do nó tạo. Để lại DB QA để đối chiếu; không drop dữ liệu.

## Kết quả thực hiện

| Bộ | Kết quả | Bằng chứng |
| --- | --- | --- |
| Restore | Thành công từ cache/môi trường hiện có | restore.txt |
| Build | 6 project riêng + solution Release, 0 warning/error | build*.txt |
| Runner HTTP/SQL/E2E/umbrella UI | 349/349 PASS; FAIL=0 | results.json |
| Chrome kiểm tra con | 22/22 PASS | browser.txt |
| WinForms kiểm tra con | 4/4 PASS | native-results.json |
| Quy tắc miền | 8/8 PASS | unit.txt |
| API inventory | 46 method/route thu thập từ assembly | API_INVENTORY.md |

Kiểm tra con Chrome/WinForms nằm trong UI-013/ENV-014 của runner, **không cộng lại** để tăng tổng độc lập. Các dòng alias trong checklist là truy vết cùng bằng chứng, không phải lần chạy mới.

## Tổng trạng thái checklist

| Trạng thái | Số dòng |
| --- | --- |
| PASS | 451 |
| FAIL | 0 |
| BLOCKED | 2 |
| NOT RUN | 47 |
| N/A | 7 |

## Theo module/use case

| Module | PASS | FAIL | BLOCKED | NOT RUN | N/A |
| --- | --- | --- | --- | --- | --- |
| ENV | 12 | 0 | 0 | 2 | 2 |
| UC | 0 | 0 | 0 | 11 | 0 |
| UC01 | 11 | 0 | 0 | 0 | 0 |
| UC02 | 9 | 0 | 0 | 1 | 0 |
| UC03 | 16 | 0 | 0 | 0 | 0 |
| UC04 | 9 | 0 | 0 | 0 | 0 |
| UC05 | 6 | 0 | 0 | 0 | 0 |
| UC06 | 8 | 0 | 0 | 2 | 1 |
| UC07 | 6 | 0 | 0 | 1 | 0 |
| UC08 | 13 | 0 | 0 | 0 | 0 |
| UC09 | 12 | 0 | 0 | 0 | 0 |
| UC10 | 9 | 0 | 0 | 1 | 2 |
| API | 13 | 0 | 0 | 7 | 2 |
| DB | 14 | 0 | 0 | 3 | 0 |
| UI | 4 | 0 | 0 | 14 | 0 |
| SEC | 12 | 0 | 0 | 4 | 0 |
| QA | 19 | 0 | 2 | 1 | 0 |
| WEB | 28 | 0 | 0 | 0 | 0 |
| E2E | 6 | 0 | 0 | 0 | 0 |
| EP001 | 3 | 0 | 0 | 0 | 0 |
| EP002 | 7 | 0 | 0 | 0 | 0 |
| EP003 | 3 | 0 | 0 | 0 | 0 |
| EP004 | 7 | 0 | 0 | 0 | 0 |
| EP005 | 7 | 0 | 0 | 0 | 0 |
| EP006 | 3 | 0 | 0 | 0 | 0 |
| EP007 | 3 | 0 | 0 | 0 | 0 |
| EP008 | 4 | 0 | 0 | 0 | 0 |
| EP009 | 7 | 0 | 0 | 0 | 0 |
| EP010 | 3 | 0 | 0 | 0 | 0 |
| EP011 | 7 | 0 | 0 | 0 | 0 |
| EP012 | 4 | 0 | 0 | 0 | 0 |
| EP013 | 7 | 0 | 0 | 0 | 0 |
| EP014 | 3 | 0 | 0 | 0 | 0 |
| EP015 | 7 | 0 | 0 | 0 | 0 |
| EP016 | 4 | 0 | 0 | 0 | 0 |
| EP017 | 7 | 0 | 0 | 0 | 0 |
| EP018 | 7 | 0 | 0 | 0 | 0 |
| EP019 | 7 | 0 | 0 | 0 | 0 |
| EP020 | 3 | 0 | 0 | 0 | 0 |
| EP021 | 3 | 0 | 0 | 0 | 0 |
| EP022 | 7 | 0 | 0 | 0 | 0 |
| EP023 | 3 | 0 | 0 | 0 | 0 |
| EP024 | 7 | 0 | 0 | 0 | 0 |
| EP025 | 7 | 0 | 0 | 0 | 0 |
| EP026 | 5 | 0 | 0 | 0 | 0 |
| EP027 | 1 | 0 | 0 | 0 | 0 |
| EP028 | 6 | 0 | 0 | 0 | 0 |
| EP029 | 5 | 0 | 0 | 0 | 0 |
| EP030 | 1 | 0 | 0 | 0 | 0 |
| EP031 | 3 | 0 | 0 | 0 | 0 |
| EP032 | 7 | 0 | 0 | 0 | 0 |
| EP033 | 7 | 0 | 0 | 0 | 0 |
| EP034 | 7 | 0 | 0 | 0 | 0 |
| EP035 | 1 | 0 | 0 | 0 | 0 |
| EP036 | 2 | 0 | 0 | 0 | 0 |
| EP037 | 5 | 0 | 0 | 0 | 0 |
| EP038 | 1 | 0 | 0 | 0 | 0 |
| EP039 | 3 | 0 | 0 | 0 | 0 |
| EP040 | 7 | 0 | 0 | 0 | 0 |
| EP041 | 4 | 0 | 0 | 0 | 0 |
| EP042 | 7 | 0 | 0 | 0 | 0 |
| EP043 | 2 | 0 | 0 | 0 | 0 |
| EP044 | 7 | 0 | 0 | 0 | 0 |
| EP045 | 1 | 0 | 0 | 0 | 0 |
| EP046 | 1 | 0 | 0 | 0 | 0 |
| NATIVE | 4 | 0 | 0 | 0 | 0 |
| BROWSER | 22 | 0 | 0 | 0 | 0 |
| GUARD | 5 | 0 | 0 | 0 | 0 |

## Kiểu bằng chứng

| Kiểu | Số dòng |
| --- | --- |
| Kiểm tra tĩnh | 14 |
| Restore runtime | 1 |
| Build runtime | 1 |
| SQL runtime | 5 |
| SQL migration runtime | 1 |
| HTTP + SQL | 124 |
| Chưa chạy | 45 |
| WinForms runtime | 1 |
| Truy vết kiểm thử đã thực hiện (không phải chạy thêm) | 42 |
| HTTP timing; không phải load test | 1 |
| SQL trực tiếp trên DB QA | 1 |
| Metadata EF + schema đã tạo | 1 |
| Schema runtime; chưa benchmark execution plan | 1 |
| Chrome runtime | 1 |
| Safety regression; no SQL writes | 1 |
| HTTP contract | 27 |
| HTTP gate | 71 |
| HTTP validation | 115 |
| WinForms controls + HTTP runtime | 4 |
| Chrome/CDP runtime | 22 |
| Process safety regression | 5 |
| SQL read-only runtime | 1 |
| Chưa chạy thao tác xóa | 1 |
| Truy vết nghiệm thu website (không phải chạy thêm) | 21 |

## Luồng và cạnh tranh đã chạy

E2E-01–06 đều có request thật và assertion SQL: đăng ký/mua/COD/doanh thu; hủy/hoàn giữ; nhập rồi đặt hàng; giao thất bại/nhận trả; sai quyền; tranh hàng cuối. Dữ liệu đầu vào và số dư/kết quả cụ thể: scenarios.json. Các race SEC-013/014 dùng hai tài khoản Admin khác nhau. SEC-015 gây timeout thật ở proxy sau khi server commit, sau đó retry cùng RequestKey và xác minh cùng đơn/giữ một lần.

Rollback được fault-inject ở dòng2 của phiếu nhập trên DB QA, kiểm tra dòng1 không tăng, phiếu còn NHAP và không có journal dở dang. Tình huống này cố ý nhận HTTP500 đã che chi tiết nội bộ; đây không phải FAIL. DBCC CHECKCONSTRAINTS, tổng chứng từ, COD, journal và giữ theo đơn mở được đối chiếu cuối phiên.

WinForms đã thực sự PerformClick Thêm → lưu sản phẩm (bấm lặp) → phiếu tự điền đúng hàng/lượng → lưu nháp, kiểm tra DB kho0 và tổng160.50. Timer chứng minh UI không bị chặn khi tải chậm; kết nối hỏng trả thông báo Việt hóa. Đây là kiểm tra controls/runtime ẩn, không thay thế mọi tương tác bằng chuột/bàn phím trên thiết bị đích.

## Lỗi đã sửa và tồn đọng

- BUG-QA-001: Form số lượng sai kiểu bị xử lý thành lệnh xóa giỏ — Verified
- BUG-QA-002: Giá thẻ và thứ tự giá lấy biến thể nằm ngoài bộ lọc — Verified
- BUG-QA-003: Hiển thị tiền làm tròn mất phần thập phân hợp lệ — Verified
- BUG-QA-004: Lỗi API chưa đăng nhập và CSRF không cùng hợp đồng JSON — Verified
- BUG-QA-005: PUT giỏ thiếu mã biến thể vẫn báo thành công — Verified
- BUG-QA-006: WinForms hiển thị thông báo mạng thô chưa Việt hóa — Verified
- BUG-QA-007: API preview giỏ âm trả 200 và âm thầm bỏ dòng — Verified
- BUG-QA-008: Website hiện lỗi fetch tiếng Anh khi mất mạng — Verified

Không còn FAIL trong các ca runtime cuối nếu kết quả bảng trên là0. Chưa phát hiện Critical/High còn mở trong các ca đã kiểm chứng; không suy ra không có lỗi ngoài mức phủ này. Xem BUG_REPORT.md để tái hiện và phạm vi hồi quy.

## Phần chưa kiểm chứng / bị chặn

| ID | Trạng thái | Nội dung | Lý do/bằng chứng hiện có |
| --- | --- | --- | --- |
| ENV-013 | NOT RUN | Website khởi động, truy cập được các route. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| ENV-016 | NOT RUN | README đủ để người khác chạy lại trên môi trường sạch. | Đã rà soát hướng dẫn và tạo DB mới; chưa có máy/VM sạch để xác minh độc lập. |
| UC-001 | NOT RUN | Điều kiện trước. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC-002 | NOT RUN | Luồng chính. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC-003 | NOT RUN | Tất cả luồng thay thế trong tài liệu. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC-004 | NOT RUN | Tất cả ngoại lệ trong tài liệu. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC-005 | NOT RUN | Điều kiện sau và dữ liệu được thay đổi. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC-006 | NOT RUN | Người dùng đúng quyền, sai quyền, chưa đăng nhập. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC-007 | NOT RUN | Dữ liệu hợp lệ, rỗng, sai định dạng, giá trị biên. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC-008 | NOT RUN | Đối tượng không tồn tại hoặc đã bị thay đổi. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC-009 | NOT RUN | Bấm nhiều lần/gửi lại request. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC-010 | NOT RUN | Mất kết nối hoặc server trả lỗi. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC-011 | NOT RUN | Giao diện và backend áp dụng cùng quy tắc. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC02-003 | NOT RUN | Giá, ảnh và tồn khả dụng đúng biến thể. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC06-006 | NOT RUN | Cập nhật giá được hiển thị đúng trên Website. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC06-009 | NOT RUN | Xóa sản phẩm đã được tham chiếu không làm mất lịch sử. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC07-005 | NOT RUN | Không làm mất lịch sử đơn hàng. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UC10-007 | NOT RUN | Bảng, thẻ tổng hợp và biểu đồ khớp nhau. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| API-002 | NOT RUN | Request hợp lệ trả kết quả đúng hợp đồng. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| API-006 | NOT RUN | ID không hợp lệ/không tồn tại. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| API-008 | NOT RUN | Đã đăng nhập nhưng sai quyền. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| API-011 | NOT RUN | HTTP status phù hợp hợp đồng; không trả 200 che lỗi. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| API-012 | NOT RUN | Response schema và thông báo lỗi nhất quán. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| API-013 | NOT RUN | Không lộ stack trace, SQL, secret hoặc password hash. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| API-018 | NOT RUN | Client xử lý đúng 400/401/403/404/409/500 hoặc mã theo hợp đồng. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| DB-003 | NOT RUN | Ràng buộc định danh/SKU chống trùng cả khi ghi đồng thời. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| DB-005 | NOT RUN | Độ chính xác và quy tắc làm tròn nhất quán. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| DB-013 | NOT RUN | Không truy vấn lặp N+1 gây chậm rõ rệt. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-001 | NOT RUN | Tất cả màn hình trong tài liệu đã tồn tại và truy cập được. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-002 | NOT RUN | Menu, liên kết, nút và điều hướng hoạt động. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-003 | NOT RUN | Không có nút chỉ hiển thị nhưng chưa xử lý. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-004 | NOT RUN | Không dùng dữ liệu giả thay cho chức năng đã tuyên bố hoàn thành. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-005 | NOT RUN | Form kiểm tra dữ liệu và chỉ rõ lỗi từng trường. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-006 | NOT RUN | Có trạng thái đang tải, dữ liệu rỗng, lỗi và thành công phù hợp. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-007 | NOT RUN | Bấm liên tục không tạo thao tác trùng. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-008 | NOT RUN | Thông báo lỗi dễ hiểu, không lộ chi tiết nội bộ. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-009 | NOT RUN | Tìm kiếm/lọc/phân trang/cập nhật làm mới dữ liệu đúng. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-011 | NOT RUN | Back/forward và phiên hết hạn được xử lý. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-012 | NOT RUN | Số tiền VNĐ, ngày và trạng thái được hiển thị thống nhất. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-014 | NOT RUN | Không tràn màn hình, che nút hoặc cắt nội dung quan trọng. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-015 | NOT RUN | Label, focus bàn phím và thông báo lỗi cơ bản dễ sử dụng. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| UI-016 | NOT RUN | Không có lỗi console, request thất bại hoặc tài nguyên 404 không được xử lý. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| SEC-005 | NOT RUN | Kiểm tra quyền tại backend, không chỉ ẩn nút. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| SEC-006 | NOT RUN | Token/cookie được lưu và cấu hình phù hợp cơ chế xác thực. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| SEC-007 | NOT RUN | Không lộ secret trong source, log và response. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| SEC-010 | NOT RUN | Log đủ để truy vết lỗi nhưng không lộ dữ liệu nhạy cảm. | Chưa có kiểm tra trực tiếp đủ phạm vi trong đợt này. |
| QA-BACKUP-SCRIPT | BLOCKED | Chạy script backup theo tài liệu | PowerShell từ chối chạy .ps1 theo ExecutionPolicy hiện tại. Không thay đổi chính sách bảo mật máy. |
| QA-PHYSICAL-PRINT | BLOCKED | In bằng thiết bị vật lý | Chưa có phiên kiểm tra thiết bị in vật lý; không gửi lệnh tới máy in người dùng. |
| QA-CLEANUP | NOT RUN | Dọn DB QA khi gọi rõ ràng | Đã cài guard và script; chưa thực thi DROP để giữ môi trường nghiệm thu. |

Không có máy/VM sạch cho cài đặt độc lập; chưa benchmark lớn/profiling N+1; chưa bấm mọi menu/dialog, keyboard focus và back/forward ở mọi màn hình. Các mục này không bị đổi sang PASS bằng suy luận từ code. Giới hạn hình nguồn xem REQUIREMENTS_TRACEABILITY.md.

## Chạy lại

```powershell
dotnet restore PhoneStore.slnx
dotnet build PhoneStore.slnx -c Release --no-restore
dotnet run --no-build -c Release --project tests/PhoneStore.Qa -- --children
node scripts/qa-static.mjs
node scripts/qa-report.mjs
```

Cần Windows/.NET10, SQL Server có quyền tạo DB QA, Chrome và Node22+. PHONESTORE_TEST_CONNECTION chỉ định server SQL kiểm thử; InitialCatalog luôn được thay bằng tên DB QA mới. Không cần nhập hay ghi credential vào báo cáo. Không chạy bộ HTTP cũ mặc định ghi vào PhoneStore để thay thế runner cách ly. Runner có khoảng chờ cho cửa sổ rate limit, không tắt/rút assertion kiểm tra bảo mật.

## Thời gian quan sát

| Mẫu request | Median ms | P95 ms | Max ms |
| --- | --- | --- | --- |
| 1174 | 3.54 | 54.10 | 4842.39 |

Đo tuần tự và các race nhỏ trên máy phát triển với DB QA; có cả fixture/CSRF/fault injection. Không phải SLA hay benchmark tải.


## Cấu hình QA và kiểm thử bộ cũ

Cấu hình riêng: qa/settings.json và appsettings.QA.json; startup xác minh connection/server/DB trước migration hoặc request. Môi trường QA không dùng connection mặc định của ứng dụng. Quy tắc màu/giá, fixture và cách lấy credential cục bộ: README.md trong docs/qa.

Backend tương tác: http://localhost:65377, server localhost, DB PhoneStore_QA_20261025_061611_2722bd. Có thể chạy lại bằng node scripts/qa.mjs serve. Init đã chạy lặp không seed lại.

Năm kiểm tra bảo vệ đích: 5/5 PASS (safety-results.json). Bộ cũ: 20 SQL PASS; 6 HTTP PASS; 8 unit chạy trong cùng lệnh legacy. Đây là lần chạy riêng, không cộng trùng 8 unit vào tổng runner. Bằng chứng legacy.txt.

Các lệnh đã chạy thêm: dotnet run --no-build -c Release --project tests/PhoneStore.Qa -- --init (hai lần); node scripts/qa.mjs serve; node scripts/qa.mjs legacy; node scripts/qa-safety-checks.mjs.

Cleanup đã được cài guard server/DB và chỉ thực hiện khi gọi cleanup rõ ràng. Chưa thực thi DROP DB tương tác; giữ lại để nghiệm thu. Không ghi là PASS cho cleanup destructive. Trong hai lần chuẩn bị fixture đầu, seed QA bị số điện thoại trùng và EF context giữ số tồn cũ giữa các thao tác; đã sửa fixture riêng và chạy init lại thành công trên DB QA mới. Đây là lỗi thiết lập test, không kết luận lỗi nghiệp vụ ứng dụng. Các DB QA thử thất bại được giữ lại, không xóa tự động.

Giới hạn ExecutionPolicy và in: environment-limitations.txt. Ảnh cuối: screens/README.md.
