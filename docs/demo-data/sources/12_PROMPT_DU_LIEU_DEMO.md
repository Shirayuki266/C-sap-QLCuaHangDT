# Prompt giao việc: tạo dữ liệu demo đầy đủ (khoảng 2 năm bán hàng)

Bộ này gồm một bước khảo sát chỉ đọc (D0) và ba bước làm (D1–D3). Đặc tả và checklist nghiệm thu là `11_DAC_TA_DU_LIEU_DEMO.md` (43 ô: 24 P0 / 19 P1 / 0 P2).

## 1. Cách dùng (dành cho bạn)

1. **Phương án tên hãng/model: đã chọn A** (hãng và model hư cấu), đã điền sẵn trong khối quy tắc chung (mục 3).
2. Đặt cùng thư mục AI đọc hoặc đính kèm: `11_DAC_TA_DU_LIEU_DEMO.md`, `02_MA_TRAN_TRANG_THAI.md`, `03_DEC_DA_CHOT.md`, `API_COMPATIBILITY.md`, `BACKEND_HANDOVER.md`, `MVC_HANDOVER.md`, `WINFORMS_HANDOVER.md`.
3. Gửi **lần lượt** D0 → D1 → D2 → D3. D0 chỉ đọc và đề xuất; bạn duyệt đề xuất rồi mới gửi D1. Mỗi bước một PR, bạn duyệt rồi merge mới gửi bước tiếp.
4. Mỗi prompt tham chiếu mục **"Quy tắc chung"** (mục 3). Nếu dùng phiên chat mới, đính kèm cả file này.
5. Dữ liệu demo **không đưa vào CSDL chính của các test**: các bộ test cũ tự tạo và xóa CSDL QA riêng của chúng. Bạn chỉ chạy công cụ trên một CSDL demo riêng (ví dụ `PhoneStore_Demo_2611`).

## 2. Điều bạn cần biết trước

- **Dữ liệu phải đúng nghiệp vụ.** Tồn kho khớp nhật ký, COD khớp trạng thái, giá khớp công thức, lịch sử đơn đúng ma trận. Đây là phần khó nhất và là lý do có bước kiểm chứng `--verify-demo-data` (mục 8 của file 11).
- **Quy mô theo bản chất từng bảng:** khách hàng, sản phẩm, phiếu nhập cỡ 50–60; hãng và danh mục ít hơn; đơn, dòng đơn, nhật ký kho nhiều hơn (khoảng 600 đơn, 2000 nhật ký). Không ép mọi bảng đều 50 dòng.
- **Trang chủ "bán chạy" chỉ tính tháng hiện tại** (quy tắc DEC.07, không lấy kỳ trước). Vì vậy dữ liệu phải có đơn trong 30 ngày gần nhất; nếu demo vào tháng sau, **chạy lại công cụ trên CSDL demo mới ngay trước buổi demo** (ước tính 1–3 phút; D1/D2 sẽ đo thực tế).
- **Công cụ phải có khóa theo tên CSDL.** Đây cũng là giới hạn "seeder chưa khóa theo tên CSDL" ghi trong tài liệu bàn giao backend; công cụ mới giải quyết nó cho bộ dữ liệu lớn, không động tới seeder cũ.
- **Ảnh là tệp SVG sinh tự động** đặt ở thư mục tĩnh của website (tệp mới, không đổi mã). Không dùng logo hay ảnh thương hiệu thật.

## 2b. Quyết định đã chốt sau D0

| # | Quyết định |
| ---: | --- |
| 1 | **Hướng A:** chạy dịch vụ thật (nhập hàng, đặt đơn, chuyển trạng thái, kiểm kê, giỏ) với đồng hồ giả `TimeProvider`; EF tối thiểu chỉ để gán `CreatedAt` lịch sử cho tài khoản (ADMIN tạo bằng `AdminProvisioner`, khách tạo qua đăng ký). Không sửa dịch vụ, Infrastructure hay schema. |
| 2 | **Khoảng 600 đơn:** các quý cộng lại là 601 (70+62+58+66+84+76+70+80+35), nằm trong khoảng 540–660; mỗi quý ±15%. Không cần nâng lên 630. |
| 3 | **Nhận lại hàng luôn có lý do** trong dữ liệu demo; không sửa `DataAudit`. |
| 4 | **Bản kê tái lập** là bản kê nghiệp vụ chuẩn hóa, loại băm mật khẩu (có salt), rowversion và thời điểm migration; không dùng salt cố định. |
| 5 | **Lịch ra mắt sản phẩm** lưu trong bản kê/kế hoạch của công cụ; chỉ nhập và bán sau mốc ra mắt. |
| 6 | **"Bán chạy"** theo hoàn tất trong tháng hiện tại (UTC+7): có ít nhất 8 sản phẩm có đơn `HOAN_TAT`/`DA_THU` hoàn tất trong tháng hiện tại, kể cả đơn đặt cuối tháng trước. |
| 7 | **Quy mô small** giảm số lượng nhưng giữ mọi bất biến và tình huống nghiệp vụ; quy mô full dùng số lượng mục tiêu. |
| 8 | **Phạm vi diff** cho phép gồm: công cụ mới, test mới, tài liệu, đăng ký dự án vào solution, thêm bước vào CI (không sửa bước cũ), tệp ảnh tĩnh mới. |
| 9 | **Phiên mô phỏng** do công cụ tạo trong lúc mô phỏng được xóa trước khi kết thúc (bảng `PHIEN` = 0). Lệnh `--seed-demo` cũ của Web và `DemoSeeder` giữ nguyên; công cụ mới là dự án riêng. |

## 3. Quy tắc chung (áp dụng cho mọi bước)

```text
QUY TẮC CHUNG - DỮ LIỆU DEMO (đọc kỹ trước khi làm)

NGUỒN: 11_DAC_TA_DU_LIEU_DEMO.md là đặc tả và checklist chính thức (xác nhận đếm đúng 24 P0 / 19 P1 / 0 P2, tổng 43). Đọc thêm 02_MA_TRAN_TRANG_THAI.md (ma trận trạng thái), 03_DEC_DA_CHOT.md, API_COMPATIBILITY.md, BACKEND_HANDOVER.md, MVC_HANDOVER.md, WINFORMS_HANDOVER.md, README và docs/DATABASE.md.

PHƯƠNG ÁN TÊN HÃNG/MODEL: A - hãng và model HƯ CẤU (ví dụ "Aurora X1 Pro"), không dùng tên hãng, logo hay ảnh thương hiệu thật (xem mục 1 của file 11). Mình đã chọn phương án A.

PHẠM VI ĐƯỢC SỬA: chỉ dự án công cụ MỚI tools/PhoneStore.DemoData (net10.0, executable), test mới cho nó, tài liệu, và TỆP ẢNH TĨNH MỚI trong thư mục tĩnh của website (ví dụ src/PhoneStore.Web/wwwroot/images/demo/, chỉ thêm tệp). KHÔNG sửa dịch vụ, Infrastructure, schema, migrations, API quản trị, website MVC, WinForms, DemoSeeder hiện có hay test của các giai đoạn trước. Nếu cần thay đổi mã chạy thật (ví dụ để thay nguồn thời gian), DỪNG, báo cáo và chờ mình quyết.

AN TOÀN (bắt buộc):
- Chỉ chạy khi tên CSDL bắt đầu bằng PhoneStore_Demo_ (hoặc PhoneStore_QA_); từ chối PhoneStore_Rebuild, CSDL thật và mọi tên khác.
- Mật khẩu (admin và khách demo) chỉ từ biến môi trường (có thể dùng lại PHONESTORE_DEMO_ADMIN_PASSWORD và PHONESTORE_DEMO_CUSTOMER_PASSWORD), không mặc định, không in log, không nằm trong repo hay file bằng chứng. Thiếu biến thì dừng.
- Cần cờ chủ động (--seed-demo-large kèm --confirm); không chạy lúc khởi động server hay migrate. Từ chối CSDL đã có dữ liệu nghiệp vụ trừ khi --reset và qua kiểm tra tên.
- Tái lập: cùng --seed và --as-of cho dữ liệu giống hệt. Có --scale small (CI) và --scale full (demo).
- Không dùng dữ liệu cá nhân hay thương hiệu thật: tên, địa chỉ, điện thoại hư cấu, email dùng miền example.com.

DỮ LIỆU: tuân theo file 11 (quy mô từng bảng ở mục 2, kế hoạch quý ở mục 3, bất biến ở mục 8). Giá bán = Round(Cost x (1 + ProfitPercent/100), 2, AwayFromZero). Lịch sử đơn tuân ma trận trạng thái của file 02. DA_THU khi và chỉ khi HOAN_TAT; Amount = Total khi đã thu, 0 khi chưa thu. Tồn kho không bao giờ âm. Mọi mốc UTC, không có mốc tương lai. Phải có đơn trong 30 ngày gần nhất để trang chủ "bán chạy" (DEC.07) không rỗng.

KIỂM THỬ: test công cụ chạy trên CI với --scale small trên CSDL QA riêng (PhoneStore_QA_...), không dùng CSDL demo hay PhoneStore_Rebuild. Chạy full thủ công trên CSDL demo và lưu tóm tắt không chứa bí mật.

QUY TRÌNH: báo git status và tạo branch từ develop mới nhất. Một PR cho mỗi bước, KHÔNG tự merge; chờ CI xanh trên đúng commit mới nhất rồi chờ mình duyệt. Chỉ đẩy đúng tên ref cần thiết (không --all/--mirror/--tags), không xóa hay viết lại ref phục hồi cục bộ, không force-push develop.

BÁO CÁO: theo mã DATA.* và nhãn P0/P1/P2 của file 11, không tự định nghĩa nhóm P0 khác; không đánh PASS cho ô chưa có bằng chứng chạy thật (NOT RUN nếu chưa chạy); kèm bảng đếm PASS/FAIL/BLOCKED/NOT RUN theo nhóm, bằng chứng (đếm dòng, truy vấn SQL, ảnh), ô còn NOT RUN và lý do, điểm tài liệu mâu thuẫn, link PR + CI. Dừng chờ mình duyệt.
```

## 4. Các prompt từng bước

### D0 — Khảo sát chỉ đọc và đề xuất

```text
Bước D0, CHỈ ĐỌC VÀ ĐỀ XUẤT. Không viết mã, không tạo branch hay PR, không sửa gì. Áp dụng QUY TẮC CHUNG - DỮ LIỆU DEMO trong file 12_PROMPT_DU_LIEU_DEMO.md (mục 3).

Đọc tài liệu và mã, rồi trả lời bằng báo cáo ngắn kèm bảng:
1. Đếm đúng 43 ô của file 11 (24 P0 / 19 P1 / 0 P2).
2. DemoSeeder hiện có làm gì (bảng nào, bao nhiêu dòng, cách đặt mật khẩu, cách chạy); công cụ hiện có AdminProvisioner và DataAudit (--audit-db) kiểm những gì.
3. Nguồn thời gian: các dịch vụ nghiệp vụ lấy thời gian từ đâu (TimeProvider, IClock, DateTime.UtcNow, SQL SYSUTCDATETIME)? Có thể chạy các dịch vụ thật với thời gian lùi về quá khứ mà KHÔNG sửa mã dịch vụ không? Nếu không, dịch vụ nào cố định thời gian hiện tại?
4. Đề xuất hướng thực hiện: (a) qua dịch vụ thật với đồng hồ giả lập nếu khả thi; (b) ghi trực tiếp bằng SQL/EF theo quy tắc rồi kiểm bằng --verify-demo-data và --audit-db. Nêu ưu/nhược cụ thể theo mã thực tế và khuyến nghị một hướng.
5. Ràng buộc schema ảnh hưởng dữ liệu: độ dài tối đa các cột (Name, Description, Specifications, ImageUrl, Address), unique, check, giá trị Kind của nhật ký kho, quy tắc OperationKey/RequestKey, các cột người tạo/người xác nhận/ActorId.
6. Cơ chế phục vụ ảnh: thư mục tĩnh của website ở đâu, cách seed ảnh SVG hiện có, ảnh /images/ được phục vụ thế nào.
7. Bố cục dự án đề xuất cho tools/PhoneStore.DemoData, danh sách cờ dòng lệnh, ước tính thời gian chạy full, rủi ro và câu hỏi cần mình quyết.
Dừng chờ mình duyệt trước khi làm D1.
```

### D1 — Công cụ, danh mục, ảnh, khách hàng

```text
Bước D1. Áp dụng QUY TẮC CHUNG - DỮ LIỆU DEMO (file 12, mục 3). D0 đã được mình duyệt, các quyết định ở mục 2b của file 12 là bắt buộc. Hướng thực hiện: Hướng A (dịch vụ thật với đồng hồ giả TimeProvider; EF tối thiểu để gán CreatedAt tài khoản). Báo git status, tạo branch từ develop mới nhất.

LÀM: (1) Tạo dự án tools/PhoneStore.DemoData và thêm vào solution sao cho các job CI hiện có không bị ảnh hưởng. (2) Khóa an toàn: kiểm tra tên CSDL, cờ chủ động và --confirm, từ chối CSDL không rỗng trừ --reset, mật khẩu từ biến môi trường, --seed, --as-of, --scale. (3) Dữ liệu gốc: 10 hãng, 6 danh mục, 12 nhà cung cấp, 50 sản phẩm (đúng phân khúc ở mục 4 file 11) với khoảng 125 biến thể, mô tả tiếng Việt và cấu hình nhiều dòng, 3 ảnh SVG mỗi sản phẩm sinh tự động ở /images/demo/ (tệp mới), 60 khách hàng và tài khoản CUSTOMER (username viết thường duy nhất, điện thoại duy nhất, email example.com hoặc NULL). (4) Test cho khóa an toàn, tái lập (băm bản kê chuẩn hóa), unique/độ dài cột, giá theo công thức. (5) Cấu trúc: tách lớp ghi CSDL (ví dụ Persistence) khỏi Generation; Verification độc lập, KHÔNG dùng lại logic tính của Generation; đường dẫn xuất ảnh là tham số; commit các tệp SVG vào repo và kiểm tra .gitignore không chặn thư mục ảnh; các khóa RequestKey/OperationKey riêng cho mỗi thao tác.
Ô cần xong: DATA.SAFE.*, DATA.CATALOG.01-05, DATA.CUST.01-03 (phần dữ liệu gốc), và bằng chứng bắt buộc: mọi tệp /images/demo/* trả 200 từ website chạy thật; không dùng logo hay ảnh thương hiệu thật; mật khẩu không xuất hiện trong log hay repo.
```

### D2 — Mô phỏng 2 năm bán hàng và kiểm chứng bất biến

```text
Bước D2. Áp dụng QUY TẮC CHUNG - DỮ LIỆU DEMO (file 12, mục 3). D1 đã merge. Báo git status, tạo branch từ develop mới nhất.

LÀM: mô phỏng theo trình tự thời gian từ đầu quý cách 8 quý đến --as-of: nhập hàng (>= 56 phiếu: xác nhận, nháp kể cả 1 rỗng, hủy; giá vốn giảm dần với model cũ), tạo đơn hàng theo kế hoạch quý ở mục 3 file 11 (mùa vụ, đỉnh 11/11, 12/12, Black Friday, trước Tết, tăng trưởng năm sau), trạng thái và độ trễ theo mục 5, thanh toán COD, lịch sử đơn theo ma trận, nhật ký kho, 6-10 lần kiểm kê, khoảng 25 dòng giỏ hàng hiện tại, sản phẩm ra mắt dần và 4 sản phẩm ngừng kinh doanh, tồn cuối kỳ đa dạng (sắp hết, hết hàng). Thêm lệnh --verify-demo-data chạy MỌI bất biến ở mục 8 file 11 bằng SQL, mã thoát khác 0 khi vi phạm, in tóm tắt theo bảng, theo quý (số đơn, doanh thu) và theo trạng thái.
Ô cần xong: DATA.SCALE.*, DATA.TIME.*, DATA.PURCH.*, DATA.ORDER.*, DATA.VERIFY.01.

BẰNG CHỨNG BẮT BUỘC: đếm dòng từng bảng nằm trong khoảng ở mục 2 file 11; bảng số đơn và doanh thu theo quý khớp mục tiêu (+-15%); kết quả --verify-demo-data trên CSDL demo full; thử cố ý làm hỏng một bất biến (ví dụ sửa tồn hoặc tổng đơn trong CSDL QA) để chứng minh lệnh kiểm chứng phát hiện; ít nhất 20 đơn trong 30 ngày gần nhất và >= 8 sản phẩm bán trong tháng hiện tại; chạy lại cùng --seed và --as-of cho kết quả giống hệt.
```

### D3 — Kiểm chứng đầu-cuối, hiệu năng, tài liệu

```text
Bước D3 (cuối). Áp dụng QUY TẮC CHUNG - DỮ LIỆU DEMO (file 12, mục 3). D2 đã merge. Báo git status, tạo branch từ develop mới nhất, liệt kê toàn bộ ID còn NOT RUN kèm lý do.

LÀM: (1) Chạy --audit-db hiện có trên CSDL demo full. (2) Smoke đầu-cuối trên CSDL demo bằng ứng dụng thật: ADMIN đăng nhập, danh sách đơn phân trang, báo cáo doanh thu của từng quý (có số liệu khác 0 và khác nhau), bán chạy, tồn kho (có biến thể sắp hết và hết hàng), hóa đơn của một đơn hoàn tất; website MVC: trang chủ có bán chạy, danh mục hiển thị đủ sản phẩm đang bán kèm ảnh và mô tả, một khách demo đăng nhập và xem lịch sử đơn; WinForms mở với CSDL demo: tổng quan và báo cáo hiển thị đúng. Ảnh chụp các màn hình chính. (3) Đo hiệu năng (thời gian mở danh sách và báo cáo) và ghi số đo. (4) Chạy lại toàn bộ regression và test các giai đoạn trước, chứng minh không đổi mã chạy thật. (5) Tài liệu docs/DEMO_DATA.md (cách tạo và đặt lại CSDL demo, biến môi trường không kèm giá trị, quy mô, giả định, giới hạn) và docs/evidence/demo-data/summary.json không chứa bí mật. (6) Chốt sổ 43 ô theo nhãn; DATA.GATE.01 chỉ PASS khi mọi P0 PASS và diff chỉ gồm tệp ảnh tĩnh mới và công cụ mới.
Ô cần xong: DATA.VERIFY.02-06, DATA.DOC.*, DATA.GATE.*.
```

## 5. Việc bạn tự kiểm tra

**Sau D0:** đọc kỹ hướng đề xuất (qua dịch vụ thật hay ghi trực tiếp). Nếu AI nói phải sửa mã dịch vụ để thay nguồn thời gian, hãy hỏi lại và ưu tiên hướng ghi trực tiếp kèm kiểm chứng bất biến.

**Sau D1:** chạy công cụ với `--scale small` trên một CSDL `PhoneStore_Demo_...` mới; mở website và xem danh mục có ảnh và mô tả; thử chạy công cụ trên tên CSDL khác (ví dụ `PhoneStore_Rebuild`) để thấy nó từ chối.

**Sau D2:** mở bảng tóm tắt theo quý (số đơn, doanh thu) và xem có mùa vụ rõ không (quý 4 cao hơn quý 2); chạy `--verify-demo-data` và tự thử một truy vấn SQL kiểm tồn kho cho vài biến thể.

**Sau D3:** mở WinForms và website trên CSDL demo, vào báo cáo doanh thu từng quý, tổng quan, tồn kho sắp hết, và trang chủ bán chạy. Đảm bảo thư mục kho mã không chứa mật khẩu và các tệp bằng chứng không có bí mật.

**Trước buổi demo:** chạy lại công cụ trên CSDL demo mới với `--as-of` là hôm nay để tháng hiện tại luôn có đơn; không dùng `--reset` trên CSDL có dữ liệu thật.
