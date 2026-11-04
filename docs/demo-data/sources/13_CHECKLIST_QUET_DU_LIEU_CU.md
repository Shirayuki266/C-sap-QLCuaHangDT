# Checklist quét dấu vết dữ liệu hư cấu cũ sau khi chuyển sang sản phẩm thật

Nhóm đã chuyển từ sản phẩm hư cấu sang **sản phẩm, ảnh và thông tin thật**. Checklist này giúp AI kiểm tra mọi nơi có thể còn dữ liệu cũ: đơn hàng cũ, phiếu nhập cũ, báo cáo các quý trước, tệp ảnh, tài liệu. Đây là bộ kiểm tra **chỉ đọc trước, sửa sau**: AI báo cáo kết quả rồi chờ bạn duyệt mới sửa.

## 1. Vì sao dấu vết cũ dễ sót

- **Snapshot trong đơn hàng:** dòng đơn lưu tên và mô tả sản phẩm tại thời điểm đặt (`DON_HANG_CT`). Nếu chỉ sửa tên ở bảng `SAN_PHAM`, các đơn cũ vẫn hiển thị tên hư cấu trên website ("đơn của tôi"), hóa đơn và WinForms. Đây là chỗ dễ sót nhất.
- **Không xóa được dữ liệu có lịch sử:** các khóa ngoại là `NO ACTION`, nên sản phẩm đã có đơn hay phiếu nhập không xóa trực tiếp được; nếu chỉ ngừng kinh doanh thì sản phẩm hư cấu cũ vẫn còn trong báo cáo.
- **Báo cáo đọc qua nhiều bảng:** doanh thu theo quý và bán chạy lấy tên từ dòng đơn, tồn kho lấy từ sản phẩm; mỗi nơi có thể cho tên khác nhau nếu sửa không đồng bộ.
- **Tệp và tài liệu:** ảnh SVG cũ trong thư mục tĩnh, ảnh chụp bằng chứng, `docs/DEMO_DATA.md`, bản sao lưu `.bak` và công cụ sinh dữ liệu vẫn có thể chứa catalog hư cấu.

**Khuyến nghị về cách làm:** vì các đơn hàng và phiếu nhập hiện tại là dữ liệu sinh tự động, cách sạch nhất là **dựng lại CSDL từ đầu bằng catalog thật** (chạy lại công cụ sinh dữ liệu, mất khoảng 1–3 phút), nên không còn dấu vết cũ. Sửa tại chỗ chỉ nên làm khi cần giữ nguyên các đơn cũ, và khi đó phải cập nhật cả snapshot dòng đơn.

## 2. Danh sách dấu vết cũ (denylist)

AI phải lập danh sách **trước khi quét**, từ nguồn đáng tin (không dựa vào trí nhớ):

| Loại | Lấy từ đâu |
| --- | --- |
| Tên hãng, tên danh mục cũ | `SELECT DISTINCT Name` từ `HANG`, `DANH_MUC` của CSDL trước khi sửa (hoặc bản sao lưu) |
| Tên model, mã sản phẩm, SKU | `SAN_PHAM.Name/Code`, `BIEN_THE.Sku`, và danh mục catalog trong mã công cụ sinh dữ liệu (lịch sử git) |
| Tên và mô tả snapshot | `DISTINCT` các cột snapshot của `DON_HANG_CT` |
| Đường dẫn ảnh | `SAN_PHAM.ImageUrl`, `SAN_PHAM_ANH.Url`; mẫu `/images/demo/` và `demo-phone.svg` |
| Cụm từ đặc trưng | Vài câu mở đầu của mô tả và dòng cấu hình mẫu cũ |
| Tên nhà cung cấp gắn thương hiệu | `NHA_CUNG_CAP.Name` nếu mang tên hãng cũ |

Khách hàng giả (tên, địa chỉ hư cấu) **không phải** dấu vết cần xóa: yêu cầu chuyển sang dữ liệu thật chỉ áp dụng cho sản phẩm. Dữ liệu cá nhân vẫn phải là hư cấu.

## 3. Checklist

Nhãn `[P0]`, `[P1]`, `[P2]` giống các checklist trước. Tổng **33 ô: 19 P0 / 12 P1 / 2 P2**. Trạng thái: `PASS`, `FAIL`, `BLOCKED`, `NOT RUN`, `N/A`; không đánh `PASS` khi chưa có bằng chứng chạy thật.

### A. Phương pháp và an toàn (CLEAN.METHOD)

- [ ] [P0] CLEAN.METHOD.01 — Ghi rõ phương án đã dùng để chuyển sang sản phẩm thật: (a) dựng lại CSDL từ đầu bằng catalog thật, (b) sửa tại chỗ rồi cập nhật snapshot, hoặc (c) khác. Nêu lý do. Nếu sửa tại chỗ, liệt kê bảng/cột đã `UPDATE` và nêu rõ việc sửa snapshot lịch sử chỉ chấp nhận cho dữ liệu demo, không áp dụng cho dữ liệu thật.
- [ ] [P0] CLEAN.METHOD.02 — Lập **danh sách dấu vết cũ** (denylist) từ nguồn đáng tin: giá trị khác biệt của CSDL trước khi sửa (tên hãng, tên model, mã sản phẩm, SKU, URL ảnh, cụm từ đặc trưng trong mô tả/cấu hình), mã và lịch sử git của công cụ sinh dữ liệu, tệp `demo-phone.svg`. Lưu ở tệp không chứa bí mật; không dựa vào trí nhớ.
- [ ] [P0] CLEAN.METHOD.03 — Có bản sao lưu CSDL trước khi sửa và cách quay lại; mọi thao tác chạy trên CSDL demo/QA có tên hợp lệ (`PhoneStore_Demo_...`/`PhoneStore_QA_...`), không đụng CSDL khác.

### B. Quét CSDL (CLEAN.DB)

Quét **mọi cột chuỗi của cả 19 bảng**, không chỉ các cột đã nghĩ tới. Dùng mẫu SQL ở mục 4.

- [ ] [P0] CLEAN.DB.01 — `HANG.Name`, `DANH_MUC.Name` không còn tên hư cấu cũ (nếu giữ lại danh mục cũ có chủ ý thì nêu lý do).
- [ ] [P0] CLEAN.DB.02 — `SAN_PHAM`: `Code`, `Name`, `Description`, `Specifications`, `ImageUrl` không còn dấu vết cũ; mô tả không dùng lại câu chữ mẫu cũ.
- [ ] [P0] CLEAN.DB.03 — `SAN_PHAM_ANH.Url` không còn đường dẫn `/images/demo/...` hay `demo-phone.svg` (trừ khi giữ có chủ ý); mỗi sản phẩm có ít nhất 2 ảnh thật hợp lệ và có `SortOrder`.
- [ ] [P0] CLEAN.DB.04 — `BIEN_THE.Sku` không còn mã cũ; SKU mới duy nhất; màu và dung lượng khớp sản phẩm thật.
- [ ] [P0] CLEAN.DB.05 — **Snapshot dòng đơn:** `DON_HANG_CT` (tên, mô tả và mọi cột snapshot khác) không còn tên/mô tả hư cấu; tên snapshot khớp sản phẩm thật của biến thể liên kết, hoặc có danh sách ngoại lệ kèm lý do.
- [ ] [P0] CLEAN.DB.06 — Phiếu nhập: `PHIEU_NHAP`, `PHIEU_NHAP_CT` không còn tham chiếu dữ liệu hư cấu (tên, ghi chú, mã); mọi dòng nhập trỏ tới biến thể thuộc sản phẩm thật.
- [ ] [P1] CLEAN.DB.07 — Lịch sử và nhật ký: `LICH_SU_DON.Reason`, `NHAT_KY_KHO.Reason` và các ghi chú khác không chứa tên model cũ.
- [ ] [P1] CLEAN.DB.08 — Nhà cung cấp: tên và mã không gắn với hãng hư cấu cũ (nếu nhà cung cấp mang tên thương hiệu).
- [ ] [P0] CLEAN.DB.09 — Quét tự động toàn bộ cột chuỗi bằng denylist (mục 4): kết quả 0 khớp, hoặc có danh sách ngoại lệ kèm lý do.
- [ ] [P0] CLEAN.DB.10 — Không còn dòng mồ côi hoặc sản phẩm/hãng cũ ở trạng thái ngừng kinh doanh, trừ khi giữ có chủ ý và nêu rõ.

### C. Báo cáo và màn hình (CLEAN.REPORT)

Dùng chính dữ liệu của các quý cũ: đơn hàng, phiếu nhập và hóa đơn của quá khứ là nơi dấu vết cũ dễ sót nhất.

- [ ] [P0] CLEAN.REPORT.01 — API báo cáo (doanh thu từng quý, bán chạy, tồn kho) trả tên sản phẩm thật; số đơn và doanh thu theo quý không đổi so với trước nếu chỉ đổi tên, hoặc khớp kế hoạch nếu dữ liệu được tạo lại.
- [ ] [P0] CLEAN.REPORT.02 — Website: trang chủ (bán chạy), danh mục, chi tiết (ảnh, mô tả, cấu hình), giỏ, đặt hàng và "đơn của tôi" (chi tiết đơn cũ ở nhiều quý) hiển thị tên và ảnh thật; không còn ảnh hỏng.
- [ ] [P0] CLEAN.REPORT.03 — WinForms: tổng quan, danh sách và chi tiết đơn cũ, hóa đơn của một số đơn ở mỗi quý, phiếu nhập cũ, tồn kho, nhật ký kho và báo cáo từng quý hiển thị tên thật.
- [ ] [P1] CLEAN.REPORT.04 — Hóa đơn (form xem và PrintPreview) của đơn cũ hiển thị tên thật.
- [ ] [P1] CLEAN.REPORT.05 — Tìm kiếm theo tên mới ra kết quả; tên cũ không còn tìm thấy ở website và WinForms.

### D. Tệp, tài liệu, bằng chứng (CLEAN.FILES)

- [ ] [P0] CLEAN.FILES.01 — Thư mục ảnh tĩnh không còn tệp `/images/demo/*.svg` của sản phẩm cũ (hoặc không còn được tham chiếu); tệp `demo-phone.svg` giữ hay xóa được nêu rõ; mọi URL ảnh trong CSDL trả HTTP 200; không có tệp ảnh mồ côi.
- [ ] [P0] CLEAN.FILES.02 — Quét repo (mã, test, README, `docs/DEMO_DATA.md`, `docs/evidence`, JSON tóm tắt, fixtures) bằng denylist: không còn tên cũ ngoài lịch sử git; mọi chỗ còn lại có lý do.
- [ ] [P1] CLEAN.FILES.03 — Ảnh chụp màn hình và bằng chứng cũ có hiển thị sản phẩm hư cấu được thay mới hoặc đánh dấu lỗi thời; liệt kê các ảnh trong báo cáo Word cần chụp lại.
- [ ] [P1] CLEAN.FILES.04 — Công cụ sinh dữ liệu/seeder: catalog cũ được thay hoặc xóa; không còn tạo sản phẩm hư cấu; test và `--verify-demo-data` dùng bộ catalog mới.
- [ ] [P1] CLEAN.FILES.05 — Các bản sao lưu (`.bak`) cũ chứa dữ liệu hư cấu được ghi nhận và thay bằng bản sao lưu mới sau khi xong.

### E. Toàn vẹn sau khi đổi (CLEAN.INTEG)

- [ ] [P0] CLEAN.INTEG.01 — `--audit-db` và `--verify-demo-data` đạt sau khi đổi; bất biến tồn kho, giá, COD, lịch sử vẫn đúng; số dòng từng bảng không đổi ngoài chủ ý.
- [ ] [P0] CLEAN.INTEG.02 — Giá bán thỏa `Round(Cost × (1 + ProfitPercent/100), 2, AwayFromZero)`; tổng đơn = Σ dòng; không có giá hoặc giá vốn bằng 0; giá thật nằm trong khoảng hợp lý.
- [ ] [P0] CLEAN.INTEG.03 — Mã sản phẩm, SKU, URL ảnh duy nhất và trong giới hạn cột: mã sản phẩm 30, SKU 50, tên 200, mô tả 50.000, cấu hình 20.000, URL ảnh 1.000.
- [ ] [P1] CLEAN.INTEG.04 — Ảnh phụ duy nhất theo (`ProductId`, `UrlHash`); `SortOrder ≥ 0`.
- [ ] [P1] CLEAN.INTEG.05 — Regression và test các giai đoạn trước vẫn đạt; CI xanh; nếu có đổi mã chạy thật thì nêu rõ phần nào và vì sao.

### F. Dữ liệu thật: nguồn và bản quyền (CLEAN.REAL)

Chuyển sang sản phẩm thật, ảnh thật làm phát sinh thêm yêu cầu về nguồn và bản quyền.

- [ ] [P1] CLEAN.REAL.01 — Mỗi sản phẩm có nguồn dữ liệu được ghi lại (nguồn thông số, nguồn ảnh, điều khoản sử dụng) trong tài liệu; ghi rõ ảnh dùng cho mục đích học tập.
- [ ] [P1] CLEAN.REAL.02 — Mô tả không chép nguyên văn từ trang bán hàng; tự viết hoặc trích ngắn có ghi nguồn.
- [ ] [P1] CLEAN.REAL.03 — Giá nhập và giá bán phù hợp thị trường; `ProfitPercent` hợp lệ (0–10.000%).
- [ ] [P2] CLEAN.REAL.04 — Tên hãng và model đúng chính tả, nhất quán giữa `Name`, SKU, màu và ảnh.
- [ ] [P2] CLEAN.REAL.05 — Các tiêu chí `DATA.CATALOG.01–03` trong file 11 (phương án hư cấu, "không dùng logo/ảnh thương hiệu thật") được cập nhật cho phương án dữ liệu thật.

## 4. Mẫu SQL quét mọi cột chuỗi

Chạy trong SSMS trên đúng CSDL demo. Điền `#terms` bằng denylist thật. **Kết quả rỗng nghĩa là không có dấu vết nào.** Mỗi dòng kết quả cho biết bảng, cột và số dòng còn chứa từ khóa.

```sql
IF OBJECT_ID('tempdb..#terms') IS NOT NULL DROP TABLE #terms;
CREATE TABLE #terms (Term NVARCHAR(200) NOT NULL);
INSERT INTO #terms (Term) VALUES (N'Aurora'), (N'/images/demo/'), (N'demo-phone.svg');  -- thay bằng denylist thật

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += CASE WHEN LEN(@sql) > 0 THEN N' UNION ALL ' ELSE N'' END
  + N'SELECT N''' + c.TABLE_NAME + N''' AS TableName, N''' + c.COLUMN_NAME + N''' AS ColumnName, COUNT(*) AS Hits FROM '
  + QUOTENAME(c.TABLE_SCHEMA) + N'.' + QUOTENAME(c.TABLE_NAME) + N' t JOIN #terms x ON t.' + QUOTENAME(c.COLUMN_NAME)
  + N' LIKE N''%'' + x.Term + N''%'' HAVING COUNT(*) > 0'
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.DATA_TYPE IN ('nvarchar', 'varchar', 'nchar', 'char') AND c.TABLE_NAME <> '__PhoneStoreMigrations';

IF LEN(@sql) = 0 SELECT 'Khong co cot chuoi nao' AS Note ELSE EXEC sp_executesql @sql;
```

Ngoài truy vấn trên, AI cần viết thêm truy vấn đối chiếu **tên snapshot của dòng đơn với tên sản phẩm của biến thể liên kết** (`DON_HANG_CT` nối `BIEN_THE` và `SAN_PHAM`) theo tên cột thật của schema, và liệt kê các dòng lệch nhau.

## 5. Prompt giao cho AI (giai đoạn kiểm tra, chưa sửa)

```text
Chỉ KIỂM TRA, KHÔNG SỬA: không sửa dữ liệu, mã, tệp hay tài liệu; không tạo branch hay PR. Mục tiêu: nhóm đã chuyển sang sản phẩm, ảnh và thông tin THẬT; hãy tìm mọi dấu vết của sản phẩm HƯ CẤU cũ.

Đọc 13_CHECKLIST_QUET_DU_LIEU_CU.md (checklist chính thức, xác nhận đếm đúng 19 P0 / 12 P1 / 2 P2, tổng 33). Làm theo thứ tự:
1. Lập denylist theo mục 2 của file 13 từ nguồn đáng tin (CSDL trước khi sửa hoặc bản sao lưu, mã công cụ sinh dữ liệu và lịch sử git); lưu danh sách không chứa bí mật; báo số mục theo từng loại.
2. Chạy mẫu SQL ở mục 4 trên CSDL demo hiện tại (chỉ đọc), và truy vấn đối chiếu tên snapshot dòng đơn với tên sản phẩm của biến thể; báo từng bảng, cột, số dòng còn chứa dấu vết.
3. Kiểm chứng ở lớp ứng dụng: gọi API báo cáo (doanh thu từng quý, bán chạy, tồn kho), mở website (trang chủ, danh mục, chi tiết, "đơn của tôi" với đơn ở nhiều quý) và WinForms (tổng quan, đơn cũ, hóa đơn, phiếu nhập cũ, báo cáo từng quý); kiểm tra mọi URL ảnh trả HTTP 200. Ghi bằng chứng (ảnh chụp, kết quả truy vấn).
4. Quét tệp và tài liệu: thư mục ảnh tĩnh, mã, test, README, docs, docs/evidence, JSON tóm tắt, bản sao lưu .bak; báo các chỗ còn chứa dấu vết.
5. Báo cáo theo mã CLEAN.* và nhãn P0/P1/P2: bảng đếm PASS/FAIL/BLOCKED/NOT RUN; danh sách phát hiện (bảng/cột/tệp, số lượng, ví dụ giá trị đã che bớt nếu nhạy cảm); phương án sửa đề xuất cho từng nhóm phát hiện. Dừng chờ mình duyệt trước khi sửa.

QUY TẮC: chỉ đọc; không dùng CSDL ngoài tên PhoneStore_Demo_ hoặc PhoneStore_QA_; không in mật khẩu, chuỗi kết nối hay bí mật; không đánh PASS cho ô chưa có bằng chứng chạy thật.
```

## 6. Bảng đếm mong đợi

| P0 | P1 | P2 | Tổng |
| ---: | ---: | ---: | ---: |
| 19 | 12 | 2 | 33 |
