# Phạm vi, truy vết yêu cầu và nghiệm thu

Nguồn: `He_thong_quan_ly_va_ban_dien_thoai_di_dong.md`, chương I–III và phần thiết kế 1–5. Đã đọc nội dung chữ và bảng HTML trong toàn bộ tài liệu (bản trích xuất: docs/reference/document-text.txt), trích xuất 48 hình và xem sáu bản tổng hợp. Activity 12–21 có nhãn nhỏ chưa xác nhận từng chữ ở bản tổng hợp; không coi phần đó là đã đọc chắc chắn. Các quy tắc tương ứng được đối chiếu với đặc tả UC và thiết kế chữ đi kèm. ERD, sequence và mockup đã đối chiếu quan hệ, trường dữ liệu và bố cục chính. Hình gốc và bản tổng hợp ở docs/reference.

## Vai trò và màn hình

| Vai trò | Phạm vi |
|---|---|
| Khách vãng lai | Danh mục, tìm/lọc/chi tiết, giỏ localStorage, đăng ký/login |
| CUSTOMER | Website: hồ sơ, mật khẩu, giỏ máy chủ, đặt COD, đơn của mình, hủy khi chờ |
| ADMIN | WinForms: danh mục/biến thể, hãng/danh mục, khách hàng, NCC, phiếu nhập, tồn, đơn, hóa đơn, báo cáo, tài khoản |

API và CSDL là thành phần nội bộ, không phải tác nhân. Không bổ sung vai trò nhân viên.

| Mã UI | Màn hình đã triển khai |
|---|---|
| UI01 | WinForms LoginForm — đăng nhập quản trị |
| UI02 | WinForms Tổng quan — doanh thu kỳ/hôm nay, đơn chờ, tồn thấp, biểu đồ |
| UI03–UI04 | MVC Catalog/Index và Details — bộ lọc, phân trang, chọn biến thể |
| UI05–UI06 | MVC Cart/Index và Checkout — giỏ, thông tin nhận, COD |
| UI07 | MVC Orders/Index và Details — lọc đơn, snapshot, timeline, hủy |
| UI08 | WinForms Điện thoại và ProductEditor — nhiều biến thể, lượng cần nhập |
| UI09 | WinForms Nhập hàng và PurchaseEditor — phiếu nháp nhiều dòng/giá vốn |
| UI10 | WinForms Tồn kho — lọc, kiểm kê, nhật ký |
| UI11 | WinForms Đơn hàng/OrderDetailForm và Khách hàng |
| UI12 | WinForms Báo cáo/ReportScreen — doanh thu, bán chạy, tồn, bảng/biểu đồ |
| Bổ sung theo UC | Account Register/Login/Profile/Password; WinForms Hãng, Danh mục, NCC, Tài khoản |

## Ma trận chức năng

Bằng chứng viết tắt: **SQL** = docs/verification/sql-checks.txt (20 nhóm); **HTTP** = http-checks.txt (6 nhóm); **Chrome** = browser-checks.txt (10 kiểm tra); **Desktop** = winforms-smoke.txt (11 trang và render các editor). “Có/đã triển khai” nghĩa là có xử lý thực tế; Desktop smoke không thay thế nghiệm thu mọi lần bấm nút.

| Mã / UC | Yêu cầu và tiêu chí nghiệm thu | Màn hình | API | Bảng | Kết quả / bằng chứng |
|---|---|---|---|---|---|
| K01 UC01 | Đăng ký; username/email/SĐT không trùng; mật khẩu có hash; chỉ CUSTOMER | Register | POST /api/auth/register | TAI_KHOAN,KHACH_HANG | Có; SQL registration/duplicates, HTTP MVC register |
| K02 UC01 | Login đúng mật khẩu, tài khoản hoạt động | Login | POST /api/auth/login | TAI_KHOAN,PHIEN | Có; SQL login roles, HTTP/Chrome login |
| K03 UC01 | Logout kết thúc phiên hiện hành | Menu tài khoản | POST /api/auth/logout | PHIEN | Có; HTTP cookie bị từ chối sau logout |
| K04 UC01 | Hồ sơ chỉ sửa của mình; unique email/SĐT | Profile | GET/PUT /api/profile | KHACH_HANG | Có; SQL profile uniqueness; Chrome mở form |
| K05 UC01 | Đổi mật khẩu cần mật khẩu cũ, thu hồi phiên cũ | Password | POST /api/auth/password | TAI_KHOAN,PHIEN | Có; SQL password change revokes all |
| K06 UC02 | Chỉ công khai hàng/hãng/danh mục hoạt động; phân trang ổn định | UI03 | GET /api/catalog | SAN_PHAM,BIEN_THE,HANG,DANH_MUC,TON_KHO | Có; SQL catalog/soft CRUD; Chrome desktop/mobile |
| K07 UC02 | Tìm tên/hãng/cấu hình/SKU, trạng thái rỗng | UI03 | GET /api/catalog?Query= | SAN_PHAM,BIEN_THE,HANG | Có; SQL/HTTP catalog; giao diện tìm kiếm |
| K08 UC02 | Kết hợp hãng/giá/dung lượng; sắp giá/tên kèm ID; xóa lọc | UI03 | GET /api/catalog?BrandId=&MinPrice=&MaxPrice=&StorageGb=&Sort= | SAN_PHAM,BIEN_THE | Có; SQL filters/pagination/invalid range |
| K09 UC02 | Chi tiết cấu hình/ảnh, màu/dung lượng, giá và lượng bán cập nhật khi chọn | UI04 | GET /api/catalog/{id} | SAN_PHAM,BIEN_THE,TON_KHO | Có; Chrome variant selection updates price |
| K10 UC03 | Giỏ guest và giỏ server; lượng dương/không vượt tồn; không giữ | UI04–05 | POST /api/cart, /api/cart/merge; /api/guest-cart/preview | GIO_HANG_CT,BIEN_THE,TON_KHO | Có; SQL cart/merge, Chrome guest merge |
| K11 UC03 | Sửa/xóa dòng, tính tiền server | UI05 | PUT /api/cart (Quantity=0 để xóa) | GIO_HANG_CT | Có; SQL add/update/remove; HTTP MVC cart |
| K12 UC03 | Địa chỉ/người nhận/SĐT bắt buộc, COD, so giá, giữ kho và tạo đơn nguyên tử, RequestKey duy nhất | UI06 | POST /api/orders | DON_HANG,DON_HANG_CT,THANH_TOAN,TON_KHO,LICH_SU_DON,NHAT_KY_KHO | Có; SQL checkout/retry/rollback/race; HTTP và Chrome checkout |
| K13 UC04 | Lịch sử chính mình; lọc thời gian/trạng thái, phân trang | UI07 | GET /api/orders | DON_HANG | Có; SQL/HTTP orders; MVC form lọc |
| K14 UC04 | Snapshot tên/SKU/biến thể/giá, địa chỉ và timeline | UI07 Details | GET /api/orders/{id} | DON_HANG_CT,THANH_TOAN,LICH_SU_DON | Có; SQL immutable snapshot; HTTP detail |
| K15 UC04 | Chỉ chủ đơn hủy khi chờ; cần lý do, xác nhận; hoàn giữ | UI07 Details | POST /api/orders/{id}/cancel | DON_HANG,TON_KHO,NHAT_KY_KHO,LICH_SU_DON | Có; SQL ownership/cancel/race; Chrome cancellation |
| A01 UC05 | Login WinApp kiểm tra ADMIN, từ chối CUSTOMER | UI01 | POST /api/auth/login?admin=true | TAI_KHOAN,PHIEN | Có; SQL roles, HTTP admin login, Desktop kết nối |
| A02 UC05 | Logout Admin hủy phiên | WinForms Tài khoản/đóng cửa sổ | POST /api/auth/logout | PHIEN | Có; HTTP logout; Desktop form |
| A03 UC05 | Đổi mật khẩu Admin, hủy phiên cũ | WinForms Tài khoản | POST /api/auth/password | TAI_KHOAN,PHIEN | Có; dùng chung dịch vụ password đã SQL test |
| A04 UC06 | Tra cứu nội bộ kể cả hàng đã ngừng | WinForms Điện thoại | GET /api/admin/products | SAN_PHAM,BIEN_THE | Có; SQL soft CRUD, Desktop dữ liệu thật |
| A05 UC06 | Tạo sản phẩm/nhiều biến thể với kho 0; lượng cần nhập chuyển sang phiếu, không tự tăng kho | UI08 → UI09 | POST /api/admin/products; POST /api/admin/purchases | SAN_PHAM,BIEN_THE,TON_KHO,PHIEU_NHAP,PHIEU_NHAP_CT | Có; SQL zero stock và phiếu; render editor; cần kiểm tra tay chuỗi bấm chuyển form |
| A06 UC06 | Sửa thông tin/ảnh/cấu hình/giá bán; version chống form cũ, không đổi giá đơn cũ | ProductEditor | PUT /api/admin/products/{id} | SAN_PHAM,BIEN_THE | Có; SQL price/snapshot/stale version; Desktop giữ đúng BrandId |
| A07 UC06 | Ngừng/mở kinh doanh, bảo toàn lịch sử | WinForms Điện thoại | PUT /api/admin/products/{id} | SAN_PHAM,BIEN_THE | Có; SQL soft CRUD |
| A08 UC06 | Hãng và danh mục: thêm/sửa/ngừng, không xóa quan hệ cũ | WinForms Hãng/Danh mục | GET/POST/PUT /api/admin/lookups/{kind}[/{id}] | HANG,DANH_MUC | Có; SQL lookup CRUD; Desktop |
| A09 UC07 | Tìm khách tên/tài khoản/SĐT/email, phân trang | WinForms Khách hàng | GET /api/admin/customers | KHACH_HANG,TAI_KHOAN | Có; HTTP customer lookup, Desktop |
| A10 UC07 | Sửa hồ sơ hợp lệ, không trả mật khẩu/hash | WinForms Khách hàng | PUT /api/admin/customers/{id} | KHACH_HANG | Có; SQL profile validation/uniqueness |
| A11 UC07 | Khóa/mở; phiên cũ bị từ chối khi khóa; giữ đơn | WinForms Khách hàng | PUT /api/admin/customers/{id} | TAI_KHOAN,PHIEN | Có; SQL sessions, HTTP old cookie after lock |
| A12 UC07 | Admin xem đơn khách được chọn | Lịch sử đơn của khách | GET /api/admin/orders?customerId= | DON_HANG | Có; service quyền ADMIN; Desktop màn hình danh sách |
| A13 UC08 | NCC tìm/thêm/sửa/ngừng; lịch sử giữ nguyên | WinForms Nhà cung cấp | GET/POST/PUT /api/admin/suppliers[/{id}] | NHA_CUNG_CAP | Có; SQL supplier soft CRUD; Desktop |
| A14 UC08 | Phiếu nháp nhiều dòng, thêm/xóa/sửa; tổng = sum(q×cost) | UI09 | GET/POST/PUT /api/admin/purchases[/{id}] | PHIEU_NHAP,PHIEU_NHAP_CT | Có; SQL draft/edit; editor đã render |
| A23 UC08 | Giá vốn >0 lưu theo lần nhập, tách giá bán | UI09 | POST/PUT /api/admin/purchases | PHIEU_NHAP_CT | Có; SQL nonpositive cost, tổng phiếu |
| A15 UC08 | Xác nhận nháp hợp lệ cộng kho 1 lần; hủy nháp không tăng; khóa sửa sau chốt | UI09 danh sách | POST /api/admin/purchases/{id}/confirm hoặc /cancel | PHIEU_NHAP,TON_KHO,NHAT_KY_KHO | Có; SQL confirm-repeat/empty/cancel/locked edit |
| A16 UC08 | Tồn thực/giữ/khả dụng; lọc hãng/tên/SKU và sắp hết | UI10 | GET /api/admin/stock | TON_KHO,BIEN_THE,SAN_PHAM,HANG | Có; SQL balances; HTTP stock; Desktop |
| A17 UC08 | Kiểm kê số thực có lý do, không dưới giữ; nhật ký actor/chứng từ | UI10 | POST /api/admin/stock/{id}/adjust; GET /api/admin/movements | TON_KHO,NHAT_KY_KHO | Có; SQL adjustment/audit reconciliation |
| A18 UC09 | Tìm đơn mã/khách/SĐT/ngày/trạng thái; phân trang | UI11 | GET /api/admin/orders | DON_HANG,KHACH_HANG | Có; HTTP admin orders; Desktop |
| A19 UC09 | Xác nhận/hủy trước giao; trạng thái và version hợp lệ | OrderDetailForm | POST /api/admin/orders/{id}/confirm hoặc /cancel | DON_HANG,TON_KHO,LICH_SU_DON,NHAT_KY_KHO | Có; SQL state/race; HTTP confirm |
| A20 UC09 | Xuất giảm tồn/giữ; hoàn tất+thu COD; thất bại không nhập lại; nhận đủ hàng trả chỉ 1 lần | OrderDetailForm | POST /api/admin/orders/{id}/dispatch, complete, fail, return | DON_HANG,THANH_TOAN,TON_KHO,LICH_SU_DON,NHAT_KY_KHO | Có; SQL failed/return/audit; HTTP complete COD |
| A21 UC09 | In hóa đơn snapshot, không làm đổi đơn | OrderDetailForm PrintPreview/Print | GET /api/admin/orders/{id} | DON_HANG,DON_HANG_CT,THANH_TOAN | Đã có code phân trang bản in; chưa kiểm chứng máy in vật lý |
| A22 UC10 | Báo cáo doanh thu/bán chạy đúng tập đơn; kỳ theo hoàn tất UTC+7; tồn hiện tại bỏ ngày; bảng và chart cùng ReportResult | UI02/UI12 | GET /api/admin/reports?kind=revenue/bestsellers/stock | DON_HANG,DON_HANG_CT,THANH_TOAN,TON_KHO | Có; SQL revenue/empty/date boundary, HTTP report, Desktop chart |

## Thực thể dữ liệu

| Bảng | Thực thể / dữ liệu chính |
|---|---|
| TAI_KHOAN | Account: username, hash, role, active, created UTC |
| KHACH_HANG | Customer: AccountId/Id, FullName, Phone, Email, Address |
| HANG / DANH_MUC | Brand / Category: tên và hoạt động |
| SAN_PHAM | Product: mã, tên, hãng/danh mục, cấu hình, mô tả, ảnh, hoạt động |
| BIEN_THE | Variant: SKU, màu, dung lượng, giá bán, hoạt động, rowversion |
| TON_KHO | Inventory: VariantId, OnHand, Reserved, rowversion; Available tính khi đọc |
| GIO_HANG_CT | CartLine: CustomerId + VariantId, quantity, updated UTC |
| DON_HANG | SalesOrder: mã, chủ đơn, thông tin nhận snapshot, trạng thái, tổng, RequestKey, thời điểm, rowversion |
| DON_HANG_CT | SalesLine: tên/SKU/biến thể/giá snapshot, số lượng |
| THANH_TOAN | Payment: một COD mỗi đơn, CHUA_THU/DA_THU, amount, collected UTC, actor |
| LICH_SU_DON | OrderHistory: trước/sau, actor, UTC, lý do |
| NHA_CUNG_CAP | Supplier: mã, tên, SĐT/địa chỉ, hoạt động |
| PHIEU_NHAP | Purchase: NCC, người lập/xác nhận, trạng thái nháp/chốt/hủy, tổng, UTC, rowversion |
| PHIEU_NHAP_CT | PurchaseLine: variant, quantity, cost |
| NHAT_KY_KHO | Movement: variant, actor, order/purchase, loại, delta tồn/giữ, lý do, UTC, OperationKey |
| PHIEN (bổ sung kỹ thuật) | LoginSession: ID ngẫu nhiên, account, hạn 8 giờ để thu hồi phiên |

FK NO ACTION và soft deactivate/lock giữ lịch sử; chỉ các dòng của giỏ và dòng phiếu nháp được xóa theo nghiệp vụ. `__PhoneStoreMigrations` là bảng kỹ thuật quản lý migration.

## Quy tắc và luồng nhất quán

| Mã | Quy tắc / tiêu chí | Cài đặt và bằng chứng |
|---|---|---|
| B01 | Khả dụng = thực - giữ, 0 <= giữ <= thực | SQL CHECK và update có điều kiện; SQL balances/audit |
| B02 | Giỏ không giữ; checkout giữ và tạo đơn/payment/history/journal trong 1 tx | PlaceOrder; SQL multi-line rollback, HTTP/Chrome |
| B03 | RequestKey cùng chủ trả đơn cũ; không cho chủ khác dùng lại | UNIQUE RequestKey + kiểm tra owner; SQL retry/ownership |
| B04 | Hủy chờ của chủ hoặc Admin trước xuất giải phóng giữ | Transition/cancel; SQL/Chrome cancel |
| B05 | Xuất giao trừ thực và giữ cùng tx | Transition/dispatch; HTTP/SQL balance |
| B06 | Fail không cộng; nhận lại toàn đơn đúng một lần | Unique journal + rowversion + transition; SQL return once |
| B07 | Xác nhận nhập chỉ nháp; giá vốn/lượng dương; tăng kho 1 lần | ConfirmPurchase; SQL repeated/empty/cancel receipt |
| B08 | Kiểm kê có lý do và không dưới giữ | AdjustStock; SQL adjustment, audit |
| B09 | Giá nhập riêng giá bán; snapshot độc lập thay đổi danh mục | PurchaseLine/Variant/SalesLine; SQL price change/snapshot |
| B10 | Hoàn tất+đã thu mới tính doanh thu/bán chạy; ngày hoàn tất UTC+7 | Report; SQL period boundary + empty/exclusion |
| B11 | Ghi đồng thời không bán vượt tồn; stale version chỉ 1 lệnh thắng | SQL transaction + applock + condition + rowversion; SQL two races |
| B12 | Bảng và biểu đồ cùng dữ liệu, tổng dòng bằng tổng hiển thị | ReportResult, ReportScreen; SQL totals + Desktop render |

Luồng chuẩn: khách tra cứu → biến thể → giỏ → login/hợp nhất → địa chỉ/COD → giữ và ghi đơn → WinForms xác nhận → xuất giao → hoàn tất+thu COD → báo cáo. Nhánh hủy giải phóng giữ trước giao. Nhánh giao thất bại chỉ hoàn kho sau nhận thực tế. Nhánh nhập: sản phẩm/biến thể kho 0 → phiếu nháp → xác nhận mới tăng kho.

## Phi chức năng và vận hành

| Mã | Yêu cầu | Trạng thái và bằng chứng |
|---|---|---|
| NFR01 | Atomic nhiều bảng, đồng thời, chống lặp | Đạt các kịch bản SQL đã liệt kê; chưa stress tải cao |
| NFR02 | Quyền server, hash, owner, khóa chặn phiên cũ, CSRF, lỗi không lộ SQL | SQL/HTTP đã kiểm tra; DTO không trả hash; API lỗi tiếng Việt |
| NFR03 | Responsive; loading/empty/error/success; chống gửi lặp; xác nhận nhạy cảm; form giữ lỗi; phân trang | Chrome desktop + 390px đạt; Desktop tải/render đạt. Chưa bấm tự động toàn bộ nút WinForms/máy in |
| NFR04 | Migration/seed, sao lưu/khôi phục, giữ lịch sử | Migration apply lặp đạt; backup+VERIFYONLY+restore DB mới đã chạy (docs/TESTING.md) |
| NFR05 | Hiệu năng quy mô cửa hàng, ngưỡng cần đo | Chưa benchmark; transaction khóa writer toàn DB ưu tiên đúng, xem ARCHITECTURE |

## Giả định và lựa chọn triển khai

- Host ASP.NET Core đồng thời chứa MVC và API, cùng gọi IStoreService trong tiến trình; WinForms gọi API. EF DbContext làm repository/unit of work, không tạo thêm các repository interface nhỏ minh họa trong sơ đồ. Domain và DTO ở Core; writer SQL tự kiểm tra bất biến trong transaction.
- Mặc định SQL Server localhost, Windows Auth, DB PhoneStore. Không thay DB khác của người dùng. Script restore chỉ tạo DB mới có tiền tố PhoneStore_Restored_, không REPLACE.
- Mật khẩu 10–128 ký tự gồm chữ và số; username chữ thường, email optional duy nhất khi có, SĐT 9–15 chữ số duy nhất. Logout hủy một phiên; khóa/đổi mật khẩu hủy tất cả phiên.
- Sắp hết: khả dụng <=3. Giỏ tối đa 100 biến thể, mỗi lượng tối đa 10.000. Tiền 2 chữ số thập phân; chưa tính thuế/phí giao/giảm giá.
- Guest merge cộng lượng, không vượt tồn; lỗi không xóa giỏ guest. Nhận trả toàn đơn thất bại một lần; không giao lại/hoàn từng phần.
- Hóa đơn bán hàng cho đơn đã xác nhận/đang giao/hoàn tất; là chứng từ nội bộ, không phải hóa đơn thuế.
- Thu tiền COD gắn thao tác hoàn tất, trạng thái thanh toán vẫn tách bảng. Không tự suy ra lợi nhuận hoặc tự điều chỉnh giá bán từ giá vốn.

Không triển khai chức năng ngoài phạm vi: cổng thanh toán online, vận chuyển tích hợp, IMEI, khuyến mãi, bảo hành, hóa đơn điện tử thuế. Không có màn hình hoặc API giả báo thành công thay cho ghi CSDL.

## Cập nhật phạm vi nghiệm thu QA

Xem REQUIREMENTS_TRACEABILITY.md và TEST_REPORT.md cho test ID và kết quả mới nhất; trạng thái triển khai trong tài liệu này không thay thế nghiệm thu. Đã xem lại hình gốc 12–21: đọc được các nút chính; hình 13 có nhãn cuối chồng nhau; hình 14, 15, 18, 20 bị cắt tại biên ảnh (hình 20 cắt khung hóa đơn bên trái). Không coi pixel thiếu là đã đọc. Kiểm kho dựa theo UI10 chi tiết khi activity19 mô tả khác về số chênh lệch.

## Yêu cầu bổ sung website khách hàng 25/10/2026

Nguồn: docs/reference/web-update-request.txt. Home riêng không có bộ lọc; Catalog/Search chia sẻ filters, Color + StorageGb + Min/MaxPrice khớp cùng một biến thể, URL có đầy đủ điều kiện. Gợi ý dữ liệu SQL công khai, debounce300ms, chống stale response và hỗ trợ bàn phím. BestSellers dùng RevenueOrders/Bounds chung với báo cáo,30 ngày lịch UTC+7 gồm hôm nay; gộp ProductId, xếp Quantity/Revenue/Id, tối đa8 sản phẩm active, giá bán hiện tại, không lộ doanh thu nội bộ.

Thêm SAN_PHAM_ANH bằng migration002 cho gallery HTTPS có thứ tự; ImageUrl vẫn là cover. Chỉ seed placeholder sản phẩm mẫu với nguồn đã xác minh, giữ custom URL. CheckoutRequest.PaymentMethod mặc định COD để tương thích; QR bị từ chối INVALID_INPUT, UI “Sắp ra mắt” disabled. Không triển khai thanh toán ngân hàng.21 tiêu chí và bằng chứng ở docs/qa/WEB_DISCOVERY_REPORT.md và QA_CHECKLIST.md.
