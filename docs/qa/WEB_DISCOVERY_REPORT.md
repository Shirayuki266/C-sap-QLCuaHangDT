# Website khách hàng: trang chủ, tìm kiếm, ảnh và QR

Đã triển khai trên repository MVC hiện có và kích hoạt backend publish tại **http://localhost:65377**, cùng DB **PhoneStore_QA_20261025_061611_2722bd**. Không tạo website demo, không reset sản phẩm/khách/đơn/phiếu nhập. Nguồn yêu cầu: docs/reference/web-update-request.txt.

## Trang kiểm tra

- Trang chủ: http://localhost:65377/
- Danh mục có bộ lọc: http://localhost:65377/Catalog
- Kết quả tìm kiếm: http://localhost:65377/Search?Query=iphone
- Chi tiết: /Catalog/Details/{id}
- Nguồn ảnh và giấy phép: /Home/ImageCredits
- Checkout: /Cart/Checkout, cần giỏ và tài khoản khách.

## Kết quả

Build Release0 warning/error. Runner **349/349 PASS** trên DB QA mới theo environment.json, không chạy ghi vào DB đang dùng. Chrome **22 kiểm tra con** và WinForms4 kiểm tra con nằm trong tổng runner. Sau kích hoạt, **12/12 kiểm tra Chrome chỉ đọc** trên QA đang dùng đạt. Không cộng các dòng nghiệm thu tái sử dụng để tăng số lần chạy độc lập.

| ID | Nghiệm thu | Trạng thái |
| --- | --- | --- |
| WEB-ACCEPT-001 | Trang chủ riêng, không có bộ lọc | PASS |
| WEB-ACCEPT-002 | Header có Trang chủ và Sản phẩm | PASS |
| WEB-ACCEPT-003 | Khách chỉ có một nút Đăng nhập | PASS |
| WEB-ACCEPT-004 | Login có liên kết đăng ký | PASS |
| WEB-ACCEPT-005 | Nhập i gợi ý iPhone từ SQL | PASS |
| WEB-ACCEPT-006 | Response cũ không ghi đè từ khóa mới | PASS |
| WEB-ACCEPT-007 | Enter, phím lên/xuống, Escape và chọn gợi ý đúng trang | PASS |
| WEB-ACCEPT-008 | Rỗng, không kết quả, ký tự đặc biệt an toàn | PASS |
| WEB-ACCEPT-009 | Danh mục và Search đều có bộ lọc | PASS |
| WEB-ACCEPT-010 | Màu/dung lượng/giá trên cùng biến thể | PASS |
| WEB-ACCEPT-011 | Giá card khớp bộ lọc | PASS |
| WEB-ACCEPT-012 | URL giữ điều kiện qua reload và back | PASS |
| WEB-ACCEPT-013 | Bán chạy khớp số lượng và doanh thu tính tay | PASS |
| WEB-ACCEPT-014 | Không đếm hủy, chưa thu hoặc ngoài30 ngày | PASS |
| WEB-ACCEPT-015 | Ảnh HTTPS từ DB, gallery có thứ tự | PASS |
| WEB-ACCEPT-016 | Ảnh lỗi có fallback | PASS |
| WEB-ACCEPT-017 | COD vẫn đặt đơn thành công | PASS |
| WEB-ACCEPT-018 | QR disabled và ghi Sắp ra mắt | PASS |
| WEB-ACCEPT-019 | Request QR không tạo đơn/giữ/thanh toán | PASS |
| WEB-ACCEPT-020 | Giao diện desktop và mobile | PASS |
| WEB-ACCEPT-021 | Build và hồi quy liên quan đạt | PASS |

Ảnh QR checkout đã được xem trực tiếp: COD được chọn, QR mờ/bị vô hiệu hóa, có giải thích. Bằng chứng API QR kiểm tra số đơn/tồn giữ trước/sau; COD vẫn tạo đơn đúng.

## Quy tắc và kiến trúc

Header dùng chung có tìm kiếm, Home/Catalog/giỏ, một link Login; đăng nhập có menu hồ sơ/đơn/mật khẩu/logout. Home không chứa filters, chỉ banner và tối đa8 card. Home rỗng giao dịch hiển thị Khám phá sản phẩm, không gắn nhãn bán chạy tùy ý.

Bán chạy dùng chung **RevenueOrders** và **Bounds** với báo cáo: HOAN_TAT + DA_THU, 30 ngày lịch gần nhất gồm hôm nay theo UTC+7 (từ00:00 ngày thứ30 đến00:00 ngày mai, cận trên loại trừ). Gộp SalesLine theo ProductId; xếp lượng giảm, doanh thu snapshot giảm, ProductId tăng. Không trả doanh thu/số bán nội bộ cho khách. Giá card từ các biến thể active hiện tại; sản phẩm/hãng/danh mục phải active. Có thể hiện Hết hàng theo hành vi danh mục hiện có.

WEB-BESTSELLERS tính tay B100 sản phẩm/300 triệu trước A100 sản phẩm/160 triệu, A gồm40+60 ở hai biến thể. Đơn hủy150, chờ80 và đơn hoàn tất200 ngoài kỳ không làm sai top. Thay giá hiện tại sau giao dịch không dùng lại đơn giá snapshot để bán.

Gợi ý /api/suggestions lấy tối đa6 sản phẩm +2 hãng từ SQL, không query khi rỗng; xếp exact/prefix/contains và tiêu chí ổn định, loại trùng theo loại+nhãn. SQL collation Latin1_General_100_CI_AI và ánh xạ đ/d hỗ trợ không dấu. Debounce300ms, AbortController + sequence bỏ response cũ. Có loading/empty/error, ArrowUp/Down, Enter/Escape, click ngoài và touch. Search và Catalog dùng cùng view/query; form/chip/pagination giữ Query/BrandId/MinPrice/MaxPrice/StorageGb/Color/Sort/Page trên URL; thay bộ lọc/keyword về Page1.

Filter SQL Any có màu+dung lượng+giá trên cùng biến thể; sort/min giá và Variants trả về cùng điều kiện. PublicProductDto bỏ OnHand/Reserved/Version; giữ Available cần cho giỏ. Browser khách không gọi API báo cáo Admin.

## Ảnh và migration

Giữ Product.ImageUrl làm ảnh đại diện; migration002 chỉ thêm SAN_PHAM_ANH cho ảnh HTTPS bổ sung có SortOrder, FK và index ProductId/SortOrder, không drop/đổi bảng cũ. Legacy client gửi Images=null giữ gallery. Admin source có trường URL ảnh bổ sung từng dòng; validation HTTPS/tối đa12/thứ tự không âm/không trùng. API không tải URL tùy ý. Gallery có ảnh đại diện và ảnh phụ theo thứ tự, alt/lazy/object-fit/fallback.

Curator đã tìm25 model qua Wikimedia Commons, kiểm tra license/author/title và HTTP image200. Sau rà soát loại hai ảnh so sánh model/biểu đồ không phù hợp. Có **8 model / 13 URL được xác minh**, 9 sản phẩm hiện có coverHTTPS và 6 gallery. Các mẫu còn lại giữ placeholder, không gán nhầm ảnh hoặc ghi đè ảnh người dùng. Nguồn và lý do fallback từng mẫu: qa/product-images.json; attribution công khai ở /Home/ImageCredits. Thumbnail lazy có thể chưa tải trong ảnh chụp toàn trang; khi URL lỗi dùng SVG dự phòng.

Migration được chạy rõ ràng bằng --product-images, kiểm tra ledger002 và số sản phẩm/đơn/phiếu trước/sau; chạy lại không ghi đè custom cover. Xem discovery-image-migration.json.

## Hiệu năng, lỗi đã sửa và giới hạn

Ranking GROUP BY/TOP8 ở SQL, không tải toàn bộ danh mục về browser. ProductQuery dùng split query cho ảnh/biến thể, tránh N+1 và nhân hàng giữa collections; suggestions hai query bounded. Input query100/color50, page/pageSize clamp như cũ. Chưa benchmark tải lớn/query plan trên production; không tự thêm index báo cáo khi chưa có số đo. Chỉ thêm index cho quan hệ ảnh mới.

Lần đầu tìm không dấu với Vietnamese collation không khớp đủ nguyên âm; đã sửa Latin collation + đ/d, WEB-ACCENTS đạt. Test HTML phải decode entity trước so text. Test phím Enter CDP cần text carriage return để phát đúng hành vi browser. Ngưỡng timer700ms của native bị nhiễu tải máy; đổi thành barrier I/O còn chờ và UI timer thật, không bỏ assertion. Các lỗi harness không bị tính là lỗi nghiệp vụ.

CSS cuối được kiểm tra ảnh desktop/mobile, sửa tương phản nav và menu tài khoản; QR disabled đã xem trực tiếp. Không tuyên bố đã thử mọi điện thoại thật hay tải production. Những NOT RUN/BLOCKED QA tổng thể trong TEST_REPORT.md vẫn còn hiệu lực.

## QR giai đoạn sau

CheckoutRequest.PaymentMethod mặc định COD, mọi giá trị khác COD bị từ chối INVALID_INPUT trước tạo/giữ đơn. UI có QR — Sắp ra mắt bị disabled. Chưa có mã QR, ngân hàng, webhook, đối soát, hoàn tiền hoặc nút tự đánh dấu đã trả. Giai đoạn sau cần chọn nhà cung cấp, quy tắc payment state, xác thực callback/idempotency, đối soát và kiểm thử thanh toán trước khi bật lựa chọn.

## Chạy lại và bàn giao

```powershell
node scripts/curate-product-images.mjs
dotnet build PhoneStore.slnx -c Release
dotnet run --no-build -c Release --project tests/PhoneStore.Qa -- --product-images
dotnet run --no-build -c Release --project tests/PhoneStore.Qa -- --children
node scripts/discovery-live-checks.mjs
node scripts/discovery-report.mjs
node scripts/qa-static.mjs
node scripts/qa-report.mjs
```

Không cần chạy curator lại để sử dụng app; manifest đã kiểm tra được giữ trong repo. Cần kiểm tra thủ công ảnh mới nếu đổi nguồn. Deploy script publish kiểm tra cổng trống trước; activate chỉ dừng đúng process QA launcher cũ khi fingerprint/path/port khớp. Đã chuyển bản QA hiện hành, giữ cùng65377/SQL; cấu hình launcher Admin đã trỏ backend publish mới. Không chạy migration/seed mỗi khi khởi động; cookie QA cũ có thể cần đăng nhập lại. Không dừng backend khác.

Executable backend hiện tại: D:\C-sap-QLCuaHangDT\dist\Phone Store Customer Web\PhoneStore.Web.exe. Đã đóng riêng màn hình Login (không có phiên làm việc), publish lại Admin và backend trong portable, mở lại shortcut. Bản Admin Desktop hiện có trường ảnh bổ sung; backend bundled cùng phiên bản website, launcher dùng lại server đang chạy tại65377, không tạo instance thứ hai.

## Rà soát ảnh trực tiếp cuối

Hai ảnh ghi tên S24 từ nguồn webtekno thực tế là Ultra. Đã xem trực tiếp và thay riêng cover seed DEMO-P008 bằng ảnh có nhãn Galaxy S24 rõ ràng, loại các ảnh Ultra khỏi gallery. Chỉ sửa khi URL hiện tại còn đúng URL seed sai; ảnh người dùng đổi được giữ. Danh sách sửa có điều kiện: qa/image-corrections.json. Nguồn thay: https://commons.wikimedia.org/wiki/File:Samsung_Galaxy_S24_01.jpg — Ganesh Mohan T, CC BY-SA4.0; HTTP200 đã xác minh. Manifest source/attribution được cập nhật cả website đang chạy và portable.
