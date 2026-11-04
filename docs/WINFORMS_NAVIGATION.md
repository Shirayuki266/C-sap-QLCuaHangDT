# Điều hướng WinForms N1

Phạm vi: `src/PhoneStore.Admin`, hai dự án test Admin và tài liệu/bằng chứng. Không đổi `PhoneStore.Admin.Client`, backend, Infrastructure, Web, schema hoặc hợp đồng API. Nhánh bắt đầu từ develop `045ffb388e10bce91537124c7191f8c8f3e11122`. Một PR; không merge, không tạo tag. Chỉ đề xuất `admin-v1.1`.

## Thiết kế đã triển khai

- `Navigation/NavigationHistory.cs` là lõi C# không phụ thuộc WinForms. Admin.Client.Checks liên kết đúng tệp nguồn này để chạy cùng test trên Ubuntu, không sao chép thuật toán. History có cursor Back/Forward, breadcrumb theo entry, cắt Forward khi rẽ nhánh, giới hạn tổng cộng 30 entry và Reset/Clear.
- `NavigationService` thuộc từng MainForm/phiên, giữ trang `NavigationPageForm` nhúng bằng `TopLevel=false`, bỏ viền, `Dock=Fill`. Mọi điểm mở trang trong shell, drilldown Tổng quan, khách hàng, sản phẩm, đơn, hóa đơn và nhật ký kho đi qua service. Trang độc lập trong harness cũ vẫn có nhánh modal để giữ kiểm chứng riêng từng form.
- Sidebar được giữ; thanh Quay lại/Tiến tới/Tổng quan và breadcrumb nằm trên vùng nội dung. Lịch sử dài cuộn ngang, tooltip giữ đủ nhãn; không tăng chiều cao vô hạn. Nút Tổng quan xóa cả Back/Forward. Chưa triển khai tab.
- Giữ instance trang nên bộ lọc, trang hiện tại và tab không mất. Khi kích hoạt lại, nạp dữ liệu máy chủ trước, rồi phục hồi dòng theo khóa, cột, cuộn dọc/ngang và focus. Báo cáo giữ trang hiển thị khi tải lại, giới hạn theo số dòng mới. Form nhúng không giữ MinimumSize 1280×800 riêng; minimum này thuộc shell.
- ProductEditor, SupplierEditor, CustomerProfileEditor, PurchaseEditor, StockCountDialog, picker, mật khẩu/cấu hình, xác nhận và PrintPreview vẫn modal. Owner top-level là shell. Nội dung chưa lưu được kiểm tra bằng snapshot request/draft, không chỉ cờ TextChanged. Lưu thất bại hoặc validation lỗi thì ở lại; Bỏ không gửi request. Kiểm kê dùng lệnh ghi hiện có và xác nhận nghiệp vụ; lý do xử lý đơn chưa gắn hành động chỉ có Bỏ/Ở lại.
- Guard áp dụng khi rời trang qua Back/Forward/Home/sidebar/breadcrumb và khi đóng/X/Esc trình soạn. Shell không điều hướng xuyên qua modal; phải đóng trình soạn trước. Ctrl+S/Enter vẫn đi tới lệnh mặc định phù hợp. Alt+Left/Right, Alt+Home/Ctrl+H và XButton1/2 dùng cùng service/guard; mouse filter nhận cả sự kiện trên grid con.
- SessionEnded xóa history và dữ liệu trang trước khi quay về Login. Đăng xuất vẫn khả dụng khi GET đang chờ; không bị khóa theo thời gian tải. Các callback kết thúc phiên cũ được kiểm tra đúng API/shell để không đóng phiên đăng nhập mới. Trang có GET đang chờ bị tháo và ẩn ngay, chỉ dispose sau khi GET kết thúc; kết quả đến muộn không được gắn vào shell mới. Không lưu lại hoặc tự replay lệnh ghi của phiên cũ.
- `UnsavedChanges.DiscardAndClose` chỉ dùng cho kết thúc phiên bắt buộc và teardown fixture; thao tác người dùng đi qua guard. Test cũ đổi cách teardown để không phải trả lời hộp thoại mới sau mỗi assertion.
- Chi tiết sản phẩm dùng hai vùng TableLayoutPanel thay SplitContainer: tránh lỗi GDI+ tái hiện trong STA khi thay vòng đời form, vẫn giữ nội dung và grid biến thể.

## Cách chạy

```powershell
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release -- --navigation-only
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release
dotnet run --project tests/PhoneStore.Admin.Tests -c Release
dotnet run --project tests/PhoneStore.Admin.Tests -c Release --no-build -- --dpi-closeout
dotnet run --project tests/PhoneStore.Admin.Tests -c Release --no-build -- --navigation-print-preview
dotnet run --project tests/PhoneStore.Admin.Client.Checks -c Release --no-build -- --walkthrough-release
```

Client/SQL và walkthrough vẫn yêu cầu các biến QA/guard của WINFORMS_HANDOVER. Chỉ fixture tạo/xóa database `PhoneStore_QA_Admin_<GUID>` của chính nó. Native preview cần driver đã cài; không in ra giấy/tệp. Walkthrough cần desktop tương tác riêng và tự khôi phục cấu hình máy chủ.

## Bằng chứng và giới hạn

- [Client W1–W6](evidence/admin-navigation/client/): 63 nhóm HTTP/SQL, gồm ma trận 48 ô xử lý đơn, cộng test lõi lịch sử.
- [STA navigation](evidence/admin-navigation/results.json): 7 nhóm mới; bộ STA đầy đủ giữ 36 nhóm WIN cũ, tổng 43 nhóm. Fake API được ghi rõ; không gọi đây là bằng chứng SQL.
- [Handles](evidence/admin-navigation/handles.json): đo tại 50 và 100 lần mở, giới hạn 30 trang sống và kiểm tra trang bị loại đã Dispose. Cũng kiểm tra số top-level và clear phiên.
- [Trước](evidence/admin-navigation/interaction-before.json) / [sau](evidence/admin-navigation/interaction-after.json): bản develop gốc được archive/build riêng, harness STA mở 10 OrderListForm thật theo ShowDialog(owner), đo 11 top-level và 10 lần đóng; bản mới 10 lần chuyển trang rồi một lần bấm Tổng quan. Đây là phép đo cơ chế cửa sổ có kiểm soát, **không** khẳng định đồ thị nghiệp vụ cũ có đường đi sâu đúng 10 trang.
- [PrintPreview](evidence/admin-navigation/print-preview.json): API giả, dialog native/driver thật, owner shell, đóng về hóa đơn rồi Back về đơn, không phát sinh lệnh ghi.
- [DPI](evidence/admin-navigation/dpi-results.json): giữ đủ 56 capture kiểm tra cũ. 125% là desktop thực 120 DPI trên máy chạy; 150% là sự kiện native WM_DPICHANGED144, không phải chứng nhận màn hình vật lý 150%. Ảnh thanh điều hướng mới giữ kích thước 1280×800 và ghi DeviceDpi thật trong manifest.
- [Release walkthrough](evidence/admin-navigation/release-manifest.json) và [SQL](evidence/admin-navigation/release-sql-results.json): executable thật, API/SQL QA thật. Luồng mở rộng kiểm tra Back/Home, hai tài khoản quản trị gây conflict, kiểm kê/lọc tồn sau Tổng quan, và mất nháp khi hết phiên. Manifest gắn SHA256 executable/DLL.

Kiểm thử phím dùng message/handler WinForms trong STA và test UI thật bằng Win32 WM_COMMAND/BN_CLICKED và UIAutomation theo AccessibleName; bounds/ảnh dùng DPI PMv2 theo từng lời gọi; không gán nhãn là người dùng thao tác thủ công. Bằng chứng 150% và printer giữ đúng các giới hạn đã chấp nhận trong handover.

## Ledger NAV

Trạng thái trong bảng chỉ được chốt theo bằng chứng chạy thật; cổng CI phải xác minh trên đúng head PR sau commit tài liệu cuối.

| ID | Mức | Trạng thái | Bằng chứng |
|---|---|---|---|
| NAV.BASE.01 | P0 | PASS | Diff chỉ Admin/test/tài liệu; architecture checks W1/STA |
| NAV.BASE.02 | P0 | PASS | 63 Client/SQL + core; 43 STA; 56 DPI, không giảm 36 nhóm WIN cũ |
| NAV.BASE.03 | P1 | PASS | NavigationChecks, cùng lõi nguồn chạy Ubuntu |
| NAV.UI.01 | P0 | PASS | STA embedded-page, drilldown; Release walkthrough |
| NAV.UI.02 | P1 | PASS | Back/Forward/cắt nhánh, trạng thái nút |
| NAV.UI.03 | P1 | PASS | Home click/shortcut xóa history và dispose |
| NAV.UI.04 | P1 | PASS | Click LinkLabel breadcrumb về đúng entry; nhãn khách/đơn |
| NAV.UI.05 | P1 | PASS | STA Alt+Left/Right/Home, Ctrl+H, Esc modal; tooltip |
| NAV.UI.06 | P1 | PASS | Mở non-modal phụ rồi đóng bằng lệnh, giữ shell |
| NAV.UI.07 | P2 | PASS | XButton1/2 từ handle grid con qua message filter |
| NAV.UI.08 | P2 | PASS | AccessibleName/tooltip Việt, STA và ảnh |
| NAV.STATE.01 | P0 | PASS | Back giữ query/status/page2/selection/scroll và tăng GET |
| NAV.STATE.02 | P0 | PASS | Back tải version mới, stale write409 và UI tải lại; SQL race cũ |
| NAV.STATE.03 | P0 | PASS | Stay/Discard/Save lỗi/thành công; Lookup Home; modal X/Esc |
| NAV.STATE.04 | P1 | PASS | Core capacity30; STA100 trang/handles/dispose |
| NAV.STATE.05 | P0 | PASS | Context thật expiry/relogin; Back không phục hồi; GET đến muộn |
| NAV.STATE.06 | P1 | PASS | Ghi thành công rồi Back thấy trạng thái mới |
| NAV.FLOW.01 | P0 | PASS | BaseCloseoutChecks: mọi xác nhận Hủy gửi 0 writes |
| NAV.FLOW.02 | P1 | NOT RUN | Đợt 22 checkpoint đã đạt; đang chốt lại runner trên DLL cuối |
| NAV.FLOW.03 | P1 | PASS | Invoice embedded/Back; PrintPreview native driver thật |
| NAV.FLOW.04 | P1 | PASS | Hai tệp đo trước/sau, phương pháp nêu rõ |
| NAV.FLOW.05 | P1 | PASS | Ảnh 1280×800 baseline120/native144; DPI56 |
| NAV.GATE.01 | P0 | NOT RUN | Chờ Ubuntu/Windows trên đúng head PR cuối |
| NAV.GATE.02 | P1 | PASS | Ledger24 ô, handover điều hướng, chỉ đề xuất admin-v1.1 |

| Mức | Tổng | PASS | FAIL | BLOCKED | NOT RUN | N/A |
|---|---:|---:|---:|---:|---:|---:|
| P0 | 9 | 8 | 0 | 0 | 1 | 0 |
| P1 | 13 | 12 | 0 | 0 | 1 | 0 |
| P2 | 2 | 2 | 0 | 0 | 0 | 0 |
| Tổng | 24 | 22 | 0 | 0 | 2 | 0 |
