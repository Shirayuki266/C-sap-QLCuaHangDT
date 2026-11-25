# CODE MAP: TRACE TOÀN BỘ 28 USE CASE & 10 SEQUENCE DIAGRAM — PHONESTORE (MONOPHONE)

Tài liệu này ánh xạ **1-1** toàn bộ **28 Use Case (`UC01.01`–`UC10.01`)** và **10 Sequence Diagram (`2.6.1`–`2.6.10`)** trong báo cáo sang **mã nguồn thực tế** trong thư mục `src/` (đã được chú thích tiếng Việt trực tiếp). Mọi tham chiếu `tệp:dòng` đều là **số dòng thực tế sau khi đã thêm chú thích tiếng Việt**.

---

## Mục lục

1. [Tổng quan kiến trúc & bản đồ các tầng](#1-tổng-quan-kiến-trúc--bản-đồ-các-tầng)
2. [Bảng tổng hợp nhanh 28 Use Case](#2-bảng-tổng-hợp-nhanh-28-use-case)
3. [Code Map chi tiết 28 Use Case (Basic, Alternative & Exception)](#3-code-map-chi-tiết-28-use-case-basic-alternative--exception)
   - [UC01: Tài khoản khách hàng (UC01.01 – UC01.04)](#uc01-tài-khoản-khách-hàng)
   - [UC02: Trang chủ & Danh mục sản phẩm (UC02.01 – UC02.02)](#uc02-trang-chủ--danh-mục-sản-phẩm)
   - [UC03: Giỏ hàng & Đặt hàng COD (UC03.01 – UC03.02)](#uc03-giỏ-hàng--đặt-hàng-cod)
   - [UC04: Đơn hàng của khách hàng (UC04.01 – UC04.02)](#uc04-đơn-hàng-của-khách-hàng)
   - [UC05: Phiên đăng nhập & bảo mật quản trị (UC05.01 – UC05.02)](#uc05-phiên-đăng-nhập--bảo-mật-quản-trị)
   - [UC06: Quản lý điện thoại, biến thể, hãng & danh mục (UC06.01 – UC06.04)](#uc06-quản-lý-điện-thoại-biến-thể-hãng--danh-mục)
   - [UC07: Quản lý khách hàng (UC07.01 – UC07.03)](#uc07-quản-lý-khách-hàng)
   - [UC08: Nhà cung cấp, Nhập hàng & Tồn kho (UC08.01 – UC08.05)](#uc08-nhà-cung-cấp-nhập-hàng--tồn-kho)
   - [UC09: Xử lý đơn hàng & Hóa đơn quản trị (UC09.01 – UC09.03)](#uc09-xử-lý-đơn-hàng--hóa-đơn-quản-trị)
   - [UC10: Tổng quan & Báo cáo (UC10.01)](#uc10-tổng-quan--báo-cáo)
4. [Trace luồng dữ liệu 10 Sequence Diagram (2.6.1 – 2.6.10)](#4-trace-luồng-dữ-liệu-10-sequence-diagram-261--2610)
   - [2.6.1 Sequence UC01 — Đăng nhập khách hàng](#261-sequence-uc01--đăng-nhập-khách-hàng)
   - [2.6.2 Sequence UC02 — Tìm kiếm, lọc & xem chi tiết điện thoại](#262-sequence-uc02--tìm-kiếm-lọc--xem-chi-tiết-điện-thoại)
   - [2.6.3 Sequence UC03 — Đặt hàng COD](#263-sequence-uc03--đặt-hàng-cod)
   - [2.6.4 Sequence UC04 — Khách hàng hủy đơn chờ xác nhận](#264-sequence-uc04--khách-hàng-hủy-đơn-chờ-xác-nhận)
   - [2.6.5 Sequence UC05 — Đăng nhập quản trị WinForms](#265-sequence-uc05--đăng-nhập-quản-trị-winforms)
   - [2.6.6 Sequence UC06 — Thêm mới / chỉnh sửa sản phẩm & biến thể](#266-sequence-uc06--thêm-mới--chỉnh-sửa-sản-phẩm--biến-thể)
   - [2.6.7 Sequence UC07 — Quản lý hồ sơ & khóa/mở tài khoản khách hàng](#267-sequence-uc07--quản-lý-hồ-sơ--khóamở-tài-khoản-khách-hàng)
   - [2.6.8 Sequence UC08 — Xác nhận phiếu nhập kho](#268-sequence-uc08--xác-nhận-phiếu-nhập-kho)
   - [2.6.9 Sequence UC09 — Xử lý vòng đời đơn hàng quản trị](#269-sequence-uc09--xử-lý-vòng-đời-đơn-hàng-quản-trị)
   - [2.6.10 Sequence UC10 — Xem báo cáo doanh thu, bán chạy, tồn kho](#2610-sequence-uc10--xem-báo-cáo-doanh-thu-bán-chạy-tồn-kho)
5. [Danh mục tệp mã nguồn đã chú thích tiếng Việt](#5-danh-mục-tệp-mã-nguồn-đã-chú-thích-tiếng-việt)

---

## 1. Tổng quan kiến trúc & bản đồ các tầng

Hệ thống vận hành theo **2 kênh giao diện** đi qua cùng một tầng nghiệp vụ & khóa giao dịch duy nhất:

```text
[Trình duyệt Khách] ──(HTTP MVC + Cookie PhoneStore.Session + Antiforgery)──┐
                                                                           ▼
    Controllers (Account/Home/Catalog/Cart/Orders) -> MvcStoreAdapter (IStoreService)
                                                                           │
[WinForms Quản trị] ──(HTTP/HTTPS + Cookie RAM + X-CSRF-TOKEN)──┐          │
                                                                 ▼          ▼
    PhoneStore.Admin (Forms) -> Admin.Client (ApiClient + Workspaces) -> Endpoints / Program.cs
                                                                           │
                                                                           ▼
    PhoneStore.Infrastructure (Accounts/*, Commerce/*, BusinessKernel + Rules)
                                                                           │
                                                                           ▼
    StoreDbContext (EF Core + Serializable + sp_getapplock 'PhoneStore:write') ──► SQL Server (18 bảng)
```

### Các thành phần hạ tầng dùng chung (Cross-cutting)
- **Kiểm soát phiên & Cookie**:
  - Thời hạn phiên cố định 8 giờ (không sliding renewal): `src/PhoneStore.Core/Accounts/Sessions.cs:8-12`, cấu hình cookie `PhoneStore.Session` tại `src/PhoneStore.Web/Program.cs:54-64`.
  - Xác thực cookie mọi request qua bảng `PHIEN` và `TAI_KHOAN.Active`: `src/PhoneStore.Web/Security/SessionAuthentication.cs:16-29` (`ValidatePrincipal`), trích xuất phiên hiện tại tại `SessionAuthentication.Current` (`L41-42`).
  - Phân nhánh MVC redirect `/Account/Login` vs API trả JSON 401/403: `src/PhoneStore.Web/Storefront/StorefrontCookieEvents.cs:9-27`.
- **Hạt nhân giao dịch & phân quyền (`BusinessKernel`)**:
  - `Require(actor, role)` (`src/PhoneStore.Infrastructure/Commerce/BusinessKernel.cs:15-25`): join `PHIEN` + `TAI_KHOAN`, ném `UNAUTHORIZED` (`L22`) hoặc `FORBIDDEN` (`L23`).
  - `Read<T>` (`L27-46`): mở giao dịch `IsolationLevel.Serializable` + khóa `sys.sp_getapplock @Resource=N'PhoneStore:write', @LockMode='Shared'`; lỗi 51000/1205 ném `CONCURRENT_UPDATE` (`L42-45`).
  - `Write<T>` (`L49-78`): mở giao dịch `IsolationLevel.Serializable` + khóa `Exclusive`; tự `SaveChangesAsync` + `CommitAsync` (`L63`), rollback và ánh xạ lỗi: `DbUpdateConcurrencyException` → `CONCURRENT_UPDATE` (`L72`), SQL 2601/2627 → `INVALID_INPUT` (`L74`), SQL 547/8115/`OverflowException` → `INVALID_INPUT` (`L75`), SQL 51000/1205 → `CONCURRENT_UPDATE` (`L76`).
- **Xử lý lỗi & bảo vệ CSRF**:
  - Website MVC: middleware `StorefrontErrors` (`src/PhoneStore.Web/Storefront/StorefrontErrors.cs:16-52`), `FormErrors.AddSummary` (`src/PhoneStore.Web/Storefront/FormErrors.cs:7-10`), `WebsiteServiceErrors` (`src/PhoneStore.Web/Storefront/WebsiteServiceErrors.cs:7-22`), `AutoValidateAntiforgeryTokenAttribute` (`src/PhoneStore.Web/Storefront/StorefrontHosting.cs:18`).
  - Admin/JSON API: middleware bắt ngoại lệ toàn cục (`src/PhoneStore.Web/Program.cs:97-109`), chuyển sang `ApiError.From` & `ApiError.Status` (`src/PhoneStore.Web/ApiError.cs:9-30`), bộ lọc CSRF `CommerceCsrfFilter` (`src/PhoneStore.Web/Security/CommerceEndpoints.cs:15-23`).
  - WinForms Admin Client: `ApiClient` (`src/PhoneStore.Admin.Client/ApiClient.cs:8-260`) với `ServerAddress.Validate` (`src/PhoneStore.Admin.Client/ApiErrors.cs:39-48`), ánh xạ thông báo `AdminApiException` (`L4-36`), khóa tuần tự `writes` (`SemaphoreSlim(1,1)`), tự lấy `/api/security/token`, cờ `NeedsReconciliation` khi kết quả không chắc chắn (`L221-223`).

---

## 2. Bảng tổng hợp nhanh 28 Use Case

| Mã UC | Tên Use Case | Actor | Điểm vào UI / Route | Service xử lý chính | Bảng SQL tác động |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `UC01.01` | Đăng ký tài khoản khách hàng | Khách vãng lai | `AccountController.Register` (`/Account/Register`) | `AccountRegistration.RegisterAsync` | `TAI_KHOAN`, `KHACH_HANG` |
| `UC01.02` | Đăng nhập khách hàng | Khách hàng | `AccountController.Login` (`/Account/Login`) | `AccountSessions.LoginAsync` | `TAI_KHOAN`, `PHIEN` |
| `UC01.03` | Cập nhật hồ sơ & đổi mật khẩu khách | Khách hàng | `AccountController.Profile`, `Password` | `AccountProfiles`, `AccountPasswords` | `KHACH_HANG`, `TAI_KHOAN`, `PHIEN` |
| `UC01.04` | Đăng xuất khách hàng | Khách hàng | `AccountController.Logout` (`/Account/Logout`) | `AccountSessions.RevokeAsync` | `PHIEN` |
| `UC02.01` | Xem danh sách, tìm kiếm, lọc, sắp xếp ĐT | Khách / Khách hàng | `HomeController.Index`, `CatalogController.Index` | `PublicBestsellerService`, `LookupService`, `CatalogService.List` | `SAN_PHAM`, `HANG`, `DANH_MUC`, `BIEN_THE`, `TON_KHO`, `DON_HANG*` |
| `UC02.02` | Xem chi tiết điện thoại & biến thể | Khách / Khách hàng | `CatalogController.Details` (`/Catalog/Details/{id}`) | `CatalogService.Read` | `SAN_PHAM`, `HANG`, `DANH_MUC`, `BIEN_THE`, `TON_KHO`, `SAN_PHAM_ANH` |
| `UC03.01` | Quản lý giỏ hàng (thêm/sửa/xóa/hợp nhất) | Khách / Khách hàng | `CartController.Index/Add/Update/Remove/Quote/Merge` | `CartService.Preview/Read/Set/Merge` | `GIO_HANG_CT`, `BIEN_THE`, `TON_KHO` |
| `UC03.02` | Đặt hàng thanh toán khi nhận hàng (COD) | Khách hàng | `CartController.Checkout` (`/Cart/Checkout`) | `CheckoutService.Place` / `Resolve` | `DON_HANG`, `DON_HANG_CT`, `THANH_TOAN`, `TON_KHO`, `NHAT_KY_KHO`, `LICH_SU_DON`, `GIO_HANG_CT` |
| `UC04.01` | Xem lịch sử và chi tiết đơn hàng của khách | Khách hàng | `OrdersController.Index`, `Details` (`/Orders/*`) | `OrderQueryService.List/Read`, `OrderReadModel.Load` | `DON_HANG`, `DON_HANG_CT`, `THANH_TOAN`, `LICH_SU_DON`, `NHAT_KY_KHO` |
| `UC04.02` | Khách hàng hủy đơn chờ xác nhận | Khách hàng | `OrdersController.Cancel` (`/Orders/Cancel`) | `OrderWorkflow.Cancel(..., admin: false)` | `DON_HANG`, `TON_KHO`, `NHAT_KY_KHO`, `LICH_SU_DON`, `THANH_TOAN` |
| `UC05.01` | Đăng nhập quản trị trên WinForms | Quản trị viên | `LoginForm`, `ServerSettingsForm` → `ApiClient.LoginAsync` | `AccountSessions.LoginAsync`, `DatabaseMigrator.IsCurrentAsync` | `TAI_KHOAN`, `PHIEN`, `__PhoneStoreMigrations` |
| `UC05.02` | Đổi mật khẩu và đăng xuất quản trị | Quản trị viên | `PasswordForm`, `MainForm.LogoutButton` | `AccountPasswords.ChangeAsync`, `AccountSessions.RevokeAsync` | `TAI_KHOAN`, `PHIEN` |
| `UC06.01` | Tra cứu điện thoại nội bộ | Quản trị viên | `ProductListForm`, `ProductDetailsForm` | `CatalogService.Internal`, `ProductService.Read` | `SAN_PHAM`, `HANG`, `DANH_MUC`, `BIEN_THE`, `TON_KHO`, `SAN_PHAM_ANH` |
| `UC06.02` | Thêm mới, sửa sản phẩm, biến thể & giá bán | Quản trị viên | `ProductEditor` → `ProductEndpoints` | `ProductService.Save`, `ProductImageStorage.SaveAsync` | `SAN_PHAM`, `BIEN_THE`, `TON_KHO`, `SAN_PHAM_ANH` |
| `UC06.03` | Ngừng hoặc mở lại kinh doanh sản phẩm | Quản trị viên | `ProductListForm.ChangeStateAsync`, `ProductEditor` | `ProductService.Save` | `SAN_PHAM`, `BIEN_THE` |
| `UC06.04` | Quản lý hãng và danh mục | Quản trị viên | `LookupForm` → `LookupEndpoints` | `LookupService.List/Save` | `HANG`, `DANH_MUC` |
| `UC07.01` | Tra cứu khách hàng & xem đơn của khách | Quản trị viên | `CustomerListForm`, `CustomerDetailsForm`, `CustomerOrdersForm` | `CustomerAdminService.List/Read`, `OrderQueryService.List` | `KHACH_HANG`, `TAI_KHOAN`, `DON_HANG` |
| `UC07.02` | Cập nhật hồ sơ khách hàng | Quản trị viên | `CustomerProfileEditor` → `CustomerAdminEndpoints` | `CustomerAdminService.Update` | `KHACH_HANG` |
| `UC07.03` | Khóa hoặc mở khóa tài khoản khách hàng | Quản trị viên | `CustomerDetailsForm.ChangeStateAsync` | `CustomerAdminService.SetActive` | `TAI_KHOAN`, `PHIEN` |
| `UC08.01` | Quản lý nhà cung cấp | Quản trị viên | `SupplierListForm`, `SupplierEditor` → `SupplierEndpoints` | `SupplierService.List/Save` | `NHA_CUNG_CAP` |
| `UC08.02` | Lập, sửa và hủy phiếu nhập nháp | Quản trị viên | `PurchaseListForm`, `PurchaseEditor`, `SupplyVariantPicker` | `PurchaseService.List/Read/Save/Cancel` | `PHIEU_NHAP`, `PHIEU_NHAP_CT` |
| `UC08.03` | Xác nhận phiếu nhập kho | Quản trị viên | `PurchaseEditor.ActAsync("confirm")` | `PurchaseService.Confirm` | `PHIEU_NHAP`, `TON_KHO`, `BIEN_THE`, `NHAT_KY_KHO` |
| `UC08.04` | Tra cứu tồn kho và nhật ký biến động kho | Quản trị viên | `StockListForm`, `StockJournalForm` → `InventoryEndpoints` | `InventoryService.List/Movements` | `TON_KHO`, `BIEN_THE`, `SAN_PHAM`, `HANG`, `NHAT_KY_KHO` |
| `UC08.05` | Kiểm kê và điều chỉnh tồn kho | Quản trị viên | `StockCountDialog` → `POST /api/admin/stock/{id}/adjust` | `InventoryService.Adjust` | `TON_KHO`, `NHAT_KY_KHO` |
| `UC09.01` | Tra cứu đơn quản trị & in hóa đơn bán hàng | Quản trị viên | `OrderListForm`, `OrderDetailForm`, `InvoiceForm` | `OrderQueryService`, `InvoiceService.Read` | `DON_HANG`, `DON_HANG_CT`, `THANH_TOAN`, `LICH_SU_DON` |
| `UC09.02` | Xác nhận hoặc hủy đơn hàng (Quản trị) | Quản trị viên | `OrderDetailForm.ActAsync` (`confirm`/`cancel`) | `OrderWorkflow.Confirm`, `OrderWorkflow.Cancel` | `DON_HANG`, `TON_KHO`, `NHAT_KY_KHO`, `LICH_SU_DON` |
| `UC09.03` | Xuất giao, hoàn tất thu COD, thất bại, nhận lại | Quản trị viên | `OrderDetailForm.ActAsync` (`dispatch`/`complete`/`fail`/`receive-return`) | `OrderWorkflow.Dispatch/Complete/Fail/ReceiveReturn` | `DON_HANG`, `THANH_TOAN`, `TON_KHO`, `NHAT_KY_KHO`, `LICH_SU_DON` |
| `UC10.01` | Tổng quan & xem báo cáo doanh thu, bán chạy, tồn | Quản trị viên | `DashboardView`, `ReportScreen` → `ReportEndpoints` | `ReportService.Read`, `OrdersWorkspace`, `SupplyWorkspace` | `TON_KHO`, `BIEN_THE`, `SAN_PHAM`, `DON_HANG`, `DON_HANG_CT`, `THANH_TOAN` |

---

## 3. Code Map chi tiết 28 Use Case (Basic, Alternative & Exception)

### UC01: Tài khoản khách hàng

#### UC01.01 — Đăng ký tài khoản khách hàng
- **Luồng chính (Basic):**
  1. Mở form đăng ký (`GET /Account/Register`): `src/PhoneStore.Web/Controllers/AccountController.cs:33-34` (nếu đã đăng nhập thì chuyển về `/Home/Index`). ViewModel: `RegisterViewModel` (`src/PhoneStore.Web/Models/AccountViewModels.cs:33-38`).
  2. Gửi form (`POST /Account/Register`): `AccountController.Register` (`src/PhoneStore.Web/Controllers/AccountController.cs:37-52`) → `MvcStoreAdapter.RegisterAsync` (`src/PhoneStore.Web/Storefront/MvcStoreAdapter.cs:29-30`) → `AccountRegistration.RegisterAsync` (`src/PhoneStore.Infrastructure/Accounts/AccountRegistration.cs:16-43`). (API tương đương: `POST /api/auth/register` tại `src/PhoneStore.Web/Program.cs:212-220`).
  3. Chuẩn hóa username về chữ thường & kiểm tra định dạng (`\A[a-z0-9_.@-]{3,100}\z`): `AccountRegistration.cs:18-21`; kiểm tra mật khẩu (10–128 ký tự có chữ và số): `PasswordInput.Validate` (`src/PhoneStore.Infrastructure/Accounts/PasswordInput.cs:8-13`); kiểm tra hồ sơ (SĐT `\A\+?[0-9]{9,15}\z`, email optional): `ProfileInput.Validate` (`src/PhoneStore.Infrastructure/Accounts/ProfileInput.cs:13-23`).
  4. Mở transaction `IsolationLevel.Serializable` + khóa `sp_getapplock` (`AccountRegistration.cs:28-35`), băm mật khẩu qua `IPasswordHasher<Account>` (`L27`), thêm `Account` (`Role="CUSTOMER"`, `Active=true`, `L26, L36-37`) vào `TAI_KHOAN`, thêm `Customer` (`L38`) vào `KHACH_HANG`, `CommitAsync` (`L40`).
  5. Gán `TempData["Notice"]` và redirect sang `Login` (`AccountController.cs:46-47`).
- **Luồng thay thế (Alternative):**
  - `A1` (bỏ trống Email hoặc Địa chỉ): `ProfileFields` cho phép `Email`/`Address` rỗng (`AccountViewModels.cs:21-25`); `ProfileInput.Validate` gọi `Text(..., optional: true)` và trả `email.Length == 0 ? null : email` (`ProfileInput.cs:19-22`) để lưu `Email = NULL` vào `KHACH_HANG`.
- **Luồng ngoại lệ (Exception):**
  - `E1` (sai định dạng / mật khẩu yếu):
    - Kiểm tra & ném lỗi: DataAnnotations trên `RegisterViewModel` (`AccountViewModels.cs:9-37`) hoặc `AccountRegistration.cs:20-23, 66-74` (`INVALID_INPUT`), `PasswordInput.cs:10-12`, `ProfileInput.cs:18-21, 28`.
    - Bắt & hiển thị: `AccountController.Register` kiểm tra `ModelState.IsValid` (`L41`), bắt `BusinessException` qua `IsFormError` (`L25-26, L49`) đưa vào `ModelState`, xóa trắng mật khẩu qua `model.Password = null; ForgetPasswords(...)` (`L27-31, L51`) để giữ lại thông tin hợp lệ khác trên form.
  - `E2` (trùng username / SĐT / email):
    - Nơi bắt SQL: `AccountRegistration.cs:45-50` bắt `SqlException` mã `2601` hoặc `2627`, gọi `RollbackAsync`, `db.ChangeTracker.Clear()` và ném `INVALID_INPUT` ("Tên đăng nhập, số điện thoại hoặc email đã tồn tại...").
    - Bắt khóa bận SQL `51000`: `AccountRegistration.cs:52-57` rollback và ném `CONCURRENT_UPDATE`.
    - Controller bắt ở `AccountController.cs:49` → `FormErrors.AddSummary` hiển thị đầu biểu mẫu.

#### UC01.02 — Đăng nhập khách hàng
- **Luồng chính (Basic):**
  1. Hiển thị form `GET /Account/Login` (`src/PhoneStore.Web/Controllers/AccountController.cs:55-57`), chuẩn hóa `ReturnUrl` an toàn cục bộ bằng `LocalDestination` (`L24`).
  2. Gửi `POST /Account/Login` (`AccountController.cs:60-85`) → `MvcStoreAdapter.LoginAsync` (`src/PhoneStore.Web/Storefront/MvcStoreAdapter.cs:31-32`, ép `Surface = "customer"`) → `AccountSessions.LoginAsync` (`src/PhoneStore.Infrastructure/Accounts/AccountSessions.cs:18-52`).
  3. Mở transaction `Serializable` + `sp_getapplock` (`L30-35`), đọc `TAI_KHOAN` theo username chữ thường (`L37`), đối chiếu hash bằng `VerifyHashedPassword` (`L38`, dùng `DummyHash` `L15` chống timing attack), kiểm tra `account.Active && account.Role == "CUSTOMER"` (`L39`).
  4. Tạo `LoginSession` (`ExpiresAt = clock.GetUtcNow() + SessionPolicy.Lifetime`, `L46-48`), lưu bảng `PHIEN` và `CommitAsync` (`L49-50`).
  5. Controller gọi `HttpContext.SignInAsync` cấp cookie `PhoneStore.Session` với `IsPersistent = false`, `AllowRefresh = false`, `ExpiresUtc = session.ExpiresAt` (`AccountController.cs:71-75`), redirect về `model.ReturnUrl` (`L77`).
- **Luồng thay thế (Alternative):**
  - `A1` (`PasswordVerificationResult.SuccessRehashNeeded`): tại `AccountSessions.cs:42-43`, hệ thống tự động băm lại `account.PasswordHash` bằng cấu hình hash mới trước khi `SaveChangesAsync` (`L49`).
- **Luồng ngoại lệ (Exception):**
  - `E1` (sai username/mật khẩu, tài khoản bị khóa `Active = false`, hoặc dùng tài khoản `ADMIN`):
    - Kiểm tra & ném lỗi: `AccountSessions.cs:26-28, 39-40, 83` ném `BusinessException("UNAUTHORIZED", ...)` (`Denied()`).
    - Bắt & hiển thị: `AccountController.cs:79-80` gom `"UNAUTHORIZED" | "INVALID_CREDENTIALS" | "ACCOUNT_LOCKED"` thành 1 thông báo chung `LoginError` (`L18`) vào `ModelState`; `L83` xóa sạch mật khẩu (`ForgetPasswords`).

#### UC01.03 — Cập nhật hồ sơ và đổi mật khẩu khách hàng
- **Luồng chính (Basic):**
  - *Nhánh Hồ sơ (`/Account/Profile`):*
    1. `GET /Account/Profile`: `AccountController.Profile` (`src/PhoneStore.Web/Controllers/AccountController.cs:96-103`) → `MvcStoreAdapter.ReadProfileAsync` (`MvcStoreAdapter.cs:35-36`) → `AccountProfiles.ReadAsync` (`src/PhoneStore.Infrastructure/Accounts/AccountProfiles.cs:26-32`).
    2. `POST /Account/Profile`: `AccountController.Profile` (`L105-121`) → `AccountProfiles.UpdateAsync` (`AccountProfiles.cs:35-62`) validate `ProfileInput.Validate` (`L37`), khóa `Serializable` + `sp_getapplock` (`L38-45`), xác thực phiên `Require` (`L14-24, L46`), cập nhật `KHACH_HANG` (`L49`) và `CommitAsync` (`L51`).
  - *Nhánh Đổi mật khẩu (`/Account/Password`):*
    1. `GET /Account/Password`: `AccountController.Password` (`L123-125`).
    2. `POST /Account/Password`: `AccountController.Password` (`L127-147`) → `AccountPasswords.ChangeAsync` (`src/PhoneStore.Infrastructure/Accounts/AccountPasswords.cs:14-45`) kiểm tra mật khẩu mới qua `PasswordInput.Validate` (`L18`), khóa `Serializable` + `sp_getapplock` (`L19-27`), xác minh mật khẩu cũ (`L32`), gán `PasswordHash` mới (`L35`), **xóa toàn bộ bản ghi `PHIEN` của tài khoản** (`ExecuteDeleteAsync` tại `L36`), `CommitAsync` (`L38`).
    3. Controller gọi `SignOutAsync` (`AccountController.cs:135`), đặt thông báo và chuyển về `Login` (`L136-137`).
- **Luồng thay thế (Alternative):** Không có luồng thay thế riêng (bảo vệ luôn giữ `Username` chỉ đọc từ `SessionAuthentication.Current(HttpContext)` khi render lại form hồ sơ tại `AccountController.cs:119-120`).
- **Luồng ngoại lệ (Exception):**
  - `E1` (mật khẩu hiện tại sai hoặc dữ liệu mới sai định dạng):
    - Kiểm tra định dạng: `ChangePasswordViewModel` / `ProfileViewModel` (`AccountViewModels.cs:16-54`), `PasswordInput.Validate` (`PasswordInput.cs:10-12`), `ProfileInput.Validate` (`ProfileInput.cs:13-29`).
    - Kiểm tra mật khẩu cũ sai: `AccountPasswords.cs:16-17, 32-33` ném `INVALID_INPUT`.
    - Bắt & hiển thị: `AccountController.cs:142` gắn `INVALID_INPUT` trực tiếp vào trường `CurrentPassword` (`ModelState.AddModelError(nameof(model.CurrentPassword), ...)`), xóa sạch `CurrentPassword` & `NewPassword` (`L145-146`).
  - `E2` (SĐT hoặc Email mới trùng khách hàng khác):
    - `AccountProfiles.cs:53-57` bắt SQL `2601/2627`, rollback, clear tracker và ném `INVALID_INPUT`; `AccountController.cs:117` hiển thị qua `FormErrors.AddSummary`.

#### UC01.04 — Đăng xuất khách hàng
- **Luồng chính (Basic):**
  1. Gửi `POST /Account/Logout` (tự động kiểm tra Antiforgery bởi `AutoValidateAntiforgeryTokenAttribute`, `StorefrontHosting.cs:18`).
  2. `AccountController.Logout` (`src/PhoneStore.Web/Controllers/AccountController.cs:87-94`) → `MvcStoreAdapter.LogoutAsync` (`MvcStoreAdapter.cs:33-34`) → `AccountSessions.RevokeAsync` (`src/PhoneStore.Infrastructure/Accounts/AccountSessions.cs:68-81`) xóa đúng 1 bản ghi `PHIEN` theo `(AccountId, SessionId)` (`L78`) trong transaction `Serializable` + `sp_getapplock`.
  3. `HttpContext.SignOutAsync` xóa cookie `PhoneStore.Session` (`AccountController.cs:91`), giữ nguyên `GIO_HANG_CT` và `DON_HANG`, chuyển về `/Home/Index` (`L93`).
- **Luồng thay thế (Alternative):**
  - `A1` (phiên đã hết hạn hoặc bị thu hồi trước đó khi bấm Đăng xuất): `SessionAuthentication.ValidatePrincipal` (`SessionAuthentication.cs:23-28`) hoặc `StorefrontErrors` (`StorefrontErrors.cs:30-36`) từ chối phiên cũ, xóa cookie qua `SignOutAsync` và đưa người dùng về trạng thái khách vãng lai/đăng nhập; trường hợp gọi qua API `POST /api/auth/logout` (`Program.cs:165-173`) mà `RevokeAsync` ném `UNAUTHORIZED` (`AccountSessions.cs:78-79`) thì tầng HTTP vẫn không giữ phiên.
- **Luồng ngoại lệ (Exception):** Không có ngoại lệ nghiệp vụ hiển thị ở UI khách.

---

### UC02: Trang chủ & Danh mục sản phẩm

#### UC02.01 — Xem danh sách, tìm kiếm, lọc và sắp xếp điện thoại
- **Luồng chính (Basic):**
  1. Trang chủ (`GET /`, `GET /Home/Index`): `HomeController.Index` (`src/PhoneStore.Web/Controllers/HomeController.cs:11-13`) → `MvcStoreAdapter.ReadBestsellersAsync` (`MvcStoreAdapter.cs:23`) → `PublicBestsellerService.Read` (`src/PhoneStore.Infrastructure/Commerce/PublicBestsellerService.cs:8-25`): tính cửa sổ tháng hiện tại UTC+7 (`L9-10`), lọc đơn `HOAN_TAT` + `COD/DA_THU`, gom nhóm theo `ProductId`, chỉ lấy sản phẩm/hãng/danh mục/biến thể `Active`, lấy `.Take(8)` (`L23`).
  2. Trang Danh mục (`GET /Catalog/Index`): `CatalogController.Index` (`src/PhoneStore.Web/Controllers/CatalogController.cs:13-27`) đọc danh sách hãng hoạt động qua `LookupService.Public("brands")` (`src/PhoneStore.Infrastructure/Commerce/LookupService.cs:10-11`), gọi `CatalogService.List(query.ToFilter())` (`src/PhoneStore.Infrastructure/Commerce/CatalogService.cs:41-47`).
  3. `CatalogService.List` chạy trong `BusinessKernel.Read` (`Serializable` + `sp_getapplock` `Shared`), gọi `Query(filter, all: false)` (`L21-39`): lọc `Active` ở cả `SAN_PHAM`, `HANG`, `DANH_MUC` và `BIEN_THE`, tìm kiếm không dấu collation `Latin1_General_100_CI_AI_SC` (kèm chuẩn hóa `đ→d`) trên tên, mã SP, thông số, tên hãng, SKU (`L26-30`), sắp xếp (`name`, `newest`, `price-asc`, `price-desc` `L31-38`), phân trang qua `Rules.Offset` (`L43`) và đọc từng sản phẩm qua `Read` (`L45`).
  4. Mỗi thẻ sản phẩm trên UI chọn biến thể giá thấp nhất để hiển thị "Giá từ" qua `ProductCardViewModel.From` (`src/PhoneStore.Web/Models/CatalogViewModels.cs:67-74`).
- **Luồng thay thế (Alternative):**
  - `A1` (khách bấm xóa từng bộ lọc / xóa toàn bộ bộ lọc): `CatalogQuery.PageUrl(page, remove)` (`src/PhoneStore.Web/Models/CatalogViewModels.cs:41-58`) xóa `BrandId`, `MinPrice`/`MaxPrice`, `StorageGb`, `Color`, `Q` hoặc `"filters"` và dựng lại query string `/Catalog/Index`.
- **Luồng ngoại lệ (Exception):**
  - `E1` (giá âm, `MinPrice > MaxPrice`, dung lượng/hãng <= 0, kiểu sắp xếp sai hoặc phân trang sai):
    - Kiểm tra tầng ViewModel: `CatalogQuery.Validate` (`CatalogViewModels.cs:30-39`).
    - Kiểm tra tầng Service: `CatalogService.Variants` (`CatalogService.cs:13-16`), `CatalogService.Query` (`L37`), `Rules.Offset` (`Rules.cs:46-51`).
    - Phân biệt HTTP status tại `CatalogController.cs:21-26`: lỗi `Page`/`PageSize` trả **HTTP 400** (`L24-25`), lỗi điều kiện lọc/sắp xếp giữ form với **soft 200** và `FormErrors.AddSummary` (`L21`).
  - `E2` (không có sản phẩm khớp): `total == 0`, `CatalogService.List` trả `Page<PublicProduct>` rỗng (`L46`) để Razor View `Views/Catalog/Index.cshtml` hiển thị trạng thái rỗng và gợi ý đổi từ khóa.

#### UC02.02 — Xem chi tiết điện thoại và biến thể
- **Luồng chính (Basic):**
  1. `GET /Catalog/Details/{id}?variantId=...`: `CatalogController.Details` (`src/PhoneStore.Web/Controllers/CatalogController.cs:30-38`) → `MvcStoreAdapter.ReadProductAsync` (`MvcStoreAdapter.cs:15-20`) → `CatalogService.Read(id)` (`src/PhoneStore.Infrastructure/Commerce/CatalogService.cs:58-71`).
  2. `CatalogService.Read` kiểm tra `SAN_PHAM`, `HANG`, `DANH_MUC` đều `Active` (`L62-64`), tải toàn bộ biến thể `Active` join `TON_KHO` tính `Available = OnHand - Reserved` (`L66-67`), tải ảnh phụ từ `SAN_PHAM_ANH` theo `SortOrder` (`L68`).
  3. Nếu không truyền `variantId`, controller tự chọn biến thể đầu tiên có `Available > 0` (hoặc biến thể đầu tiên nếu đều hết hàng) (`CatalogController.cs:35-36`).
- **Luồng thay thế (Alternative):** Chuyển biến thể qua link/query `?variantId=...` sẽ kiểm tra `RequireSelection` (`MvcStoreAdapter.cs:24-28`); khi `Available == 0`, giao diện chi tiết khóa nút Thêm vào giỏ.
- **Luồng ngoại lệ (Exception):**
  - `E1` (sản phẩm không tồn tại, ngừng kinh doanh hoặc `variantId` không thuộc sản phẩm/ngừng bán):
    - Ném lỗi `NOT_FOUND` tại `CatalogService.cs:65` hoặc `MvcStoreAdapter.RequireSelection` (`L27`).
    - `StorefrontErrors` (`src/PhoneStore.Web/Storefront/StorefrontErrors.cs:37-42, 57`) chuyển `NOT_FOUND` thành **HTTP 404** và render `WebsiteError.cshtml`.

---

### UC03: Giỏ hàng & Đặt hàng COD

#### UC03.01 — Quản lý giỏ hàng (thêm, sửa, xóa, hợp nhất)
- **Luồng chính (Basic):**
  1. *Khách đã đăng nhập (`CUSTOMER`):*
     - Xem giỏ (`GET /Cart/Index`): `CartController.Index` → `Page` (`src/PhoneStore.Web/Controllers/CartController.cs:20-25`) → `CartService.Read` (`src/PhoneStore.Infrastructure/Commerce/CartService.cs:27-32`) → `CartService.Preview` (`L9-25`). Badge số lượng trên header: `CartBadgeViewComponent.InvokeAsync` (`src/PhoneStore.Web/Storefront/CartBadgeViewComponent.cs:10-16`).
     - Thêm vào giỏ (`POST /Cart/Add`): `CartController.Add` (`CartController.cs:99-107`) → `MvcStoreAdapter.AddToCartAsync` (`MvcStoreAdapter.cs:63-68`, kiểm tra `ReadProductAsync(productId, variantId)` trước khi gọi `CartService.Set(..., add: true)` tại `CartService.cs:54-55` → `SetOne` `L38-53` lưu vào `GIO_HANG_CT`).
     - Sửa số lượng / Xóa dòng (`POST /Cart/Update`, `POST /Cart/Remove`): `CartController.Update` (`L111-119`) và `CartController.Remove` (`L122-129`) gọi `CartService.Set(..., add: false)` (`Quantity = 0` thì `Remove` khỏi `GIO_HANG_CT` tại `L42`).
  2. *Khách vãng lai (localStorage trình duyệt):*
     - Frontend gửi danh sách biến thể lên `POST /Cart/Quote` (`CartController.Quote` `L131-139`) hoặc API `POST /api/cart/preview` (`CartEndpoints.cs:10`) → `CartService.Preview` (`CartService.cs:9-25`) kiểm tra bán được & tồn khả dụng, trả đơn giá + tổng tiền.
- **Luồng thay thế (Alternative):**
  - `A1` (hợp nhất giỏ tạm `localStorage` khi vừa đăng nhập): `POST /Cart/Merge` (`CartController.Merge` `L141-149`) hoặc `POST /api/cart/merge` (`CartEndpoints.cs:15`) → `CartService.Merge` (`CartService.cs:57-84`) trong `BusinessKernel.Write`:
    - Với từng `VariantId`, lấy `Math.Max(số lượng máy chủ, số lượng tạm)` (`L61-63`), cắt xuống `Math.Min(Quantity, Available)` nếu còn bán hoặc `0` nếu ngừng bán (`L70`), ghi danh sách điều chỉnh `CartAdjustment` (`DISCONTINUED` / `OUT_OF_STOCK` / `QUANTITY_ADJUSTED` tại `L73`), cập nhật/xóa `GIO_HANG_CT` (`L74-79`) và đảm bảo kết quả idempotent khi gọi lặp.
- **Luồng ngoại lệ (Exception):**
  - `E1` (số lượng <= 0, vượt `Available`, giỏ vượt 100 biến thể, biến thể ngừng kinh doanh hoặc không tồn tại):
    - Kiểm tra & ném lỗi: `CartService.Validate` (`L35`, `INVALID_INPUT`), `CartService.SetOne` (`L40-49`: `INVALID_INPUT` khi ngừng bán/`> 100` biến thể, `OUT_OF_STOCK` khi `Available < quantity`), `CartService.Merge` (`L81`: `INVALID_INPUT` nếu sau hợp nhất `> 100`).
    - Bắt & hiển thị: `CartController.Add/Update` (`L104, L116`) giữ dòng lỗi `ErrorVariant`/`AttemptedQuantity` và hiển thị lỗi trên trang Giỏ; `CartController.Quote/Merge` (`L136, L145`) trả JSON lỗi với status từ `WebsiteServiceErrors.Status` (400 / 409).

#### UC03.02 — Đặt hàng thanh toán khi nhận hàng (COD)
- **Luồng chính (Basic):**
  1. `GET /Cart/Checkout`: `CartController.Checkout` (`src/PhoneStore.Web/Controllers/CartController.cs:28-44`) đọc giỏ (`L30`), lấy hồ sơ khách để điền sẵn người nhận/SĐT/địa chỉ (`L32-33`), sinh `RequestKey = Guid.NewGuid()` và mã hóa danh sách giá đã hiển thị vào `Confirmation` bằng `CheckoutConfirmation.Protect` (`src/PhoneStore.Web/Storefront/CheckoutConfirmation.cs:14-15`).
  2. `POST /Cart/Checkout`: `CartController.Checkout` (`CartController.cs:46-96`) giải mã `Confirmation` (`L49`), gọi `MvcStoreAdapter.PlaceOrderAsync` (`MvcStoreAdapter.cs:47`) → `CheckoutService.Place` (`src/PhoneStore.Infrastructure/Commerce/CheckoutService.cs:17-62`) trong `BusinessKernel.Write` (`Serializable` + `sp_getapplock Exclusive`).
  3. Kiểm tra `PaymentMethod == "COD"`, người nhận/SĐT/địa chỉ và danh sách `PriceConfirmation` (`L21-26`), kiểm tra `RequestKey` (`L28-36`), kiểm tra giỏ không rỗng & khớp giá xác nhận & đủ tồn (`L38-46`).
  4. Thực thi trong 1 giao dịch (`L47-60`): thêm `Order` (`DON_HANG`, `Status = "CHO_XAC_NHAN"`, `Code = "DH" + RequestKey`), `Payment` (`THANH_TOAN`, `Method = "COD"`, `Status = "CHUA_THU"`), vòng lặp từng dòng theo `VariantId` tăng dần thực hiện SQL nguyên tử `UPDATE dbo.TON_KHO SET Reserved = Reserved + Qty WHERE VariantId = ... AND OnHand - Reserved >= Qty` (`L53`), thêm `OrderLine` (snapshot `Name`, `Sku`, `Color / StorageGb`, `Price` `L55`), thêm `StockMovement` (`Kind = "GIU"`, `DeltaReserved = Qty`, `OperationKey = RequestKey` `L56`), thêm `OrderHistory` (`To = "CHO_XAC_NHAN"` `L58`), xóa toàn bộ `GIO_HANG_CT` (`L59`), commit và redirect sang `/Orders/Details/{id}` (`CartController.cs:57`).
- **Luồng thay thế (Alternative):**
  - `A1` (gửi lặp cùng `RequestKey` với đúng khách hàng & khớp nội dung người nhận/dòng hàng):
    - `CheckoutService.Place` (`L28-36`) phát hiện `previous` theo `RequestKey`, đối chiếu `CustomerId`, `Recipient`, `Phone`, `Address`, `OrderLines` và trả về ngay `previous.Id` mà không giữ kho thêm hay tạo đơn thứ hai.
    - Nếu người dùng nhấn gửi lại gây lỗi `INVALID_STATE` tại controller, `CartController.cs:82-90` gọi `store.ResolveOrderAsync` (`CheckoutService.Resolve` `L9-15`) để tự động chuyển hướng tới đơn đã tạo trước đó.
- **Luồng ngoại lệ (Exception):**
  - `E1` (giá biến thể hoặc giỏ vừa thay đổi so với token `Confirmation` đã ký):
    - Service kiểm tra `!confirmed.SequenceEqual(input.Lines...)` tại `CheckoutService.cs:42` → rollback toàn bộ transaction và ném `PriceChangedException(current)` (`src/PhoneStore.Core/Commerce/Orders.cs:6-9`).
    - Controller bắt tại `CartController.cs:59-64` (hoặc `L65-70` với mã `PRICE_CHANGED`), bật `model.PriceChanged = true`, giữ trang Checkout và gọi `CheckoutPage(model, changed.Cart)` để ký lại token mới theo giá hiện hành cho khách xác nhận lại.
  - `E2` (giỏ rỗng, biến thể ngừng bán, không đủ tồn `OnHand - Reserved < Quantity`, hoặc `UPDATE TON_KHO` trả `!= 1`, hoặc phương thức không phải COD / xung đột `RequestKey`):
    - Service ném `INVALID_INPUT` / `OUT_OF_STOCK` / `FORBIDDEN` / `INVALID_STATE` tại `CheckoutService.cs:22-54` → `BusinessKernel.Write` rollback toàn bộ giao dịch.
    - Controller bắt `REQUEST_KEY_CONFLICT`/`PAYMENT_METHOD_UNSUPPORTED` để cấp `RequestKey` mới (`CartController.cs:71-76`), bắt `CartIssue` (`L77-81`), `INVALID_INPUT`/`OUT_OF_STOCK`/`CONCURRENT_UPDATE` (`L92`) và giữ trang Checkout cùng thông báo lỗi.

---

### UC04: Đơn hàng của khách hàng

#### UC04.01 — Xem lịch sử và chi tiết đơn hàng của khách
- **Luồng chính (Basic):**
  1. Xem danh sách (`GET /Orders/Index`): `OrdersController.Index` (`src/PhoneStore.Web/Controllers/OrdersController.cs:21-33`) → `MvcStoreAdapter.ListCustomerOrdersAsync` (`MvcStoreAdapter.cs:55-58`) → `OrderQueryService.List(..., admin: false)` (`src/PhoneStore.Infrastructure/Commerce/OrderQueryService.cs:14-24`). Luôn ép lọc `x.CustomerId == actor.AccountId` (`L19`), lọc trạng thái, ngày UTC+7 qua `DateWindow.Parse` (`L15`), từ khóa và phân trang.
  2. Xem chi tiết (`GET /Orders/Details/{id}`): `OrdersController.Details` (`OrdersController.cs:35-39`) → `MvcStoreAdapter.ReadCustomerOrderAsync` (`MvcStoreAdapter.cs:49-54`) → `OrderQueryService.Read(actor, id, admin: false)` (`OrderQueryService.cs:8-12`) → `OrderReadModel.Load` (`src/PhoneStore.Infrastructure/Commerce/OrderReadModel.cs:8-18`) trả về snapshot mặt hàng, thanh toán và timeline `LICH_SU_DON` (ẩn `ActorId`/`OperationKey` nội bộ qua `CustomerOrderSnapshot`).
- **Luồng thay thế (Alternative):**
  - `A1` (khách chưa có đơn hoặc không khớp bộ lọc): `OrderQueryService.List` trả `Total = 0`, danh sách rỗng để `Views/Orders/Index.cshtml` hiển thị trạng thái danh sách trống.
- **Luồng ngoại lệ (Exception):**
  - `E1` (mã đơn không tồn tại hoặc thuộc tài khoản khác): `OrderQueryService.Read` (`OrderQueryService.cs:11`) kiểm tra `x.Id == id && x.CustomerId == actor.AccountId`, nếu không khớp ném `NOT_FOUND` → `StorefrontErrors` render trang **HTTP 404**. (Ngoài ra nếu lọc `Status` sai hoặc `From > To`, `OrdersController.Index` bắt `INVALID_INPUT` ở `L31-32`).

#### UC04.02 — Khách hàng hủy đơn chờ xác nhận
- **Luồng chính (Basic):**
  1. Gửi `POST /Orders/Cancel`: `OrdersController.Cancel` (`src/PhoneStore.Web/Controllers/OrdersController.cs:42-63`) đọc trước `ReadCustomerOrderAsync` (`L45`) để đảm bảo ranh giới chủ sở hữu.
  2. Gọi `MvcStoreAdapter.CancelCustomerOrderAsync` (`MvcStoreAdapter.cs:60-61`) → `OrderWorkflow.Cancel(actor, id, VersionedCommand, admin: false)` (`src/PhoneStore.Infrastructure/Commerce/OrderDecisions.cs:35-47`) trong `BusinessKernel.Write`.
  3. `Load` kiểm tra chủ đơn, `OperationKey` và `Rules.Version` (`L9-17`), `Unpaid` kiểm tra `COD/CHUA_THU` (`L18-21`), kiểm tra `order.Status == "CHO_XAC_NHAN"` (`L38`), kiểm tra lý do & `KeyUnused` (`L39`).
  4. Duyệt từng dòng `DON_HANG_CT` theo `VariantId` tăng dần: trừ `TON_KHO.Reserved -= Quantity WHERE Reserved >= Quantity` (`L43`) và ghi `NHAT_KY_KHO` (`Kind = "HUY_GIU"`, `DeltaReserved = -Quantity` `L44`).
  5. Chuyển `order.Status = "DA_HUY"`, ghi `LICH_SU_DON` kèm lý do (`L46`), commit và redirect về `Details` (`OrdersController.cs:52-53`).
- **Luồng thay thế (Alternative):** Không có luồng thay thế riêng.
- **Luồng ngoại lệ (Exception):**
  - `E1` (thiếu lý do, lệch `Version` do Admin vừa xử lý đơn, lặp `OperationKey` hoặc trạng thái khác `CHO_XAC_NHAN`):
    - Service kiểm tra & ném lỗi: `OrderDecisions.cs:12-43` (`NOT_FOUND`, `CONCURRENT_UPDATE`, `INVALID_STATE`, `INVALID_INPUT`).
    - Controller bắt & hiển thị: `OrdersController.cs:55-59` bắt `CONCURRENT_UPDATE`/`STALE_VERSION`/`INVALID_STATE`, thông báo "Đơn đã thay đổi, vui lòng tải lại..." và redirect để tải trạng thái mới; `L60` bắt `INVALID_INPUT` (lý do không hợp lệ) hiển thị ngay trên form chi tiết.

---

### UC05: Phiên đăng nhập & bảo mật quản trị

#### UC05.01 — Đăng nhập quản trị trên ứng dụng WinForms
- **Luồng chính (Basic):**
  1. Khởi chạy `Program.Main` → `AdminApplication.ShowLogin` (`src/PhoneStore.Admin/Program.cs:9-50`): nạp cấu hình từ `SettingsStore.Load` (`src/PhoneStore.Admin/SettingsStore.cs:10-17`), khởi tạo `ApiClient(settings)` (validate URL qua `ServerAddress.Validate` tại `src/PhoneStore.Admin.Client/ApiClient.cs:23` → `src/PhoneStore.Admin.Client/ApiErrors.cs:41-47`) và mở `LoginForm`.
  2. Khi nhấn Đăng nhập (`LoginForm.SubmitAsync` tại `src/PhoneStore.Admin/LoginForm.cs:64-72`), gọi `api.ReadinessAsync` (`src/PhoneStore.Admin.Client/ApiClient.cs:30-37`) → `GET /health/ready` (`src/PhoneStore.Web/Program.cs:223-235`, dùng `DatabaseMigrator.IsCurrentAsync` và `BackendIdentity.Fingerprint` tại `src/PhoneStore.Web/BackendIdentity.cs:9-15`).
  3. Gọi `api.LoginAsync` (`ApiClient.cs:40-58`) → `WriteAsync` (`L208-224`) gọi `GET /api/security/token` (`Program.cs:207-211`) lấy `X-CSRF-TOKEN`, rồi `POST /api/auth/login` (`Program.cs:142-157`) với `LoginInput(..., Surface: "admin")`.
  4. `AccountSessions.LoginAsync` (`src/PhoneStore.Infrastructure/Accounts/AccountSessions.cs:18-52`) xác minh mật khẩu, `Active == true`, `Role == "ADMIN"`, tạo bản ghi `PHIEN` 8 giờ; API ký cookie `PhoneStore.Session` (lưu trong `CookieContainer` RAM của `ApiClient.MemoryCookies`, `ApiClient.cs:256-264`).
  5. `LoginForm` phát sự kiện `Authenticated` (`LoginForm.cs:69`), `AdminApplication` đóng `LoginForm` và mở `MainForm` (`Program.cs:44-49`).
- **Luồng thay thế (Alternative):**
  - `A1` (cấu hình máy chủ từ xa & fingerprint): Admin bấm `ConfigureButton` mở `ServerSettingsForm` (`src/PhoneStore.Admin/ServerSettingsForm.cs:14-33`), nhập `BaseUrl`, `ExpectedEnvironment`, `ExpectedFingerprint` và lưu qua `SettingsStore.Save` (`SettingsStore.cs:19-26`).
- **Luồng ngoại lệ (Exception):**
  - `E1` (URL HTTP cho máy không phải localhost, backend chưa sẵn sàng, hoặc lệch environment/fingerprint):
    - `ServerAddress.Validate` (`ApiErrors.cs:41-47`) ném `INVALID_SERVER` (400/client); `ApiClient.ReadinessAsync` (`ApiClient.cs:34-36`) ném `SERVER_MISMATCH` hoặc `INVALID_RESPONSE`; `/health/ready` lỗi trả 503 `DATABASE_UNAVAILABLE` (`Program.cs:234`).
    - Bắt & hiển thị tại `ServerSettingsForm.cs:31` và `LoginForm.CheckAsync/SubmitAsync` (`L61, L70`) qua `AdminTheme.Failure`.
  - `E2` (sai tài khoản/mật khẩu, tài khoản bị khóa hoặc tài khoản `CUSTOMER`):
    - `AccountSessions.LoginAsync` (`AccountSessions.cs:39-40`) ném `UNAUTHORIZED` → HTTP 401; `ApiClient.LoginAsync` (`ApiClient.cs:56`) đổi mã thành `LOGIN_FAILED` (401) và `LoginForm.SubmitAsync` (`L70`) hiển thị `MessageLabel`, xóa `PasswordBox` (`L68, L71`).

#### UC05.02 — Đổi mật khẩu và đăng xuất quản trị
- **Luồng chính (Basic):**
  - *Đổi mật khẩu:* nhấn `PasswordButton` trên `MainForm` (`MainForm.cs:75`) mở `PasswordForm` (`src/PhoneStore.Admin/PasswordForm.cs:15-34`). `SubmitAsync` (`L27-33`) gọi `api.ChangePasswordAsync` (`ApiClient.cs:94-98`) → `POST /api/account/password` (`Program.cs:175-184`) + `X-CSRF-TOKEN` → `AccountPasswords.ChangeAsync` (`AccountPasswords.cs:14-45`) cập nhật hash và **xóa toàn bộ phiên trong `PHIEN`**, rồi `ApiClient.Reset()` xóa cookie/Identity cục bộ (`ApiClient.cs:97, 253`) và phát `SessionEnded` để `AdminApplication.ReturnToLogin` quay về `LoginForm`.
  - *Đăng xuất:* nhấn `LogoutButton` hoặc đóng `MainForm` → `MainForm.LogoutAsync` (`src/PhoneStore.Admin/MainForm.cs:126-134`) → `api.LogoutAsync` (`ApiClient.cs:79-91`) → `POST /api/auth/logout` (`Program.cs:165-173`) → `AccountSessions.RevokeAsync` xóa `PHIEN`, `ApiClient.Reset()` dọn cookie RAM và `AdminApplication.ReturnToLogin` (`Program.cs:53-58`) mở lại `LoginForm`.
- **Luồng thay thế (Alternative):**
  - `A1` (đang mở cửa sổ `PurchaseEditor` có nháp chưa lưu khi phiên kết thúc / hết hạn): `AdminApplication.ReturnToLogin` (`src/PhoneStore.Admin/Program.cs:56, 59`) kiểm tra `HasPurchaseWindow(oldShell)`, hiển thị cảnh báo "Nháp chưa lưu" (`MessageBox.Show`) và dứt khoát đóng bằng `UnsavedChanges.DiscardAndClose` (`src/PhoneStore.Admin/Navigation/UnsavedChanges.cs:55-56`) để không gửi lại lệnh ghi bằng phiên cũ. Khi đóng form/chuyển trang bình thường khi phiên còn sống, `UnsavedChanges.TryLeaveAsync` (`L41-53`) hỏi Lưu / Bỏ / Ở lại.
- **Luồng ngoại lệ (Exception):**
  - `E1` (mật khẩu hiện tại sai hoặc mật khẩu mới sai chính sách): `AccountPasswords.ChangeAsync` (`AccountPasswords.cs:17-18, 33`) ném `INVALID_INPUT` (HTTP 400); `PasswordForm.SubmitAsync` (`PasswordForm.cs:32`) bắt `AdminApiException` và hiển thị trên `MessageLabel`, phiên hiện tại vẫn giữ nguyên.

---

### UC06: Quản lý điện thoại, biến thể, hãng & danh mục

#### UC06.01 — Tra cứu điện thoại nội bộ
- **Luồng chính (Basic):**
  1. Mở `ProductListForm` từ thanh điều hướng `MainForm` (`MainForm.cs:50`): `LoadAsync` (`src/PhoneStore.Admin/ProductListForm.cs:40-55`) tải hãng/danh mục và gọi `CatalogWorkspace.Products` (`src/PhoneStore.Admin.Client/Catalog.cs:91-92`) → `GET /api/admin/products` (`src/PhoneStore.Web/Endpoints/CatalogEndpoints.cs:13-15`) → `CatalogService.Internal` (`src/PhoneStore.Infrastructure/Commerce/CatalogService.cs:49-56`, truyền `all: true` để thấy cả sản phẩm/biến thể ngừng kinh doanh).
  2. Mở chi tiết nội bộ qua `DetailsButton`: `ProductDetailsForm.LoadAsync` (`ProductListForm.cs:74-94`) → `CatalogWorkspace.Product(id)` (`Catalog.cs:93-94`) → `GET /api/admin/products/{id}` (`ProductEndpoints.cs:11`) → `ProductService.Read` (`src/PhoneStore.Infrastructure/Commerce/ProductService.cs:11-20`) trả `AdminProduct` kèm giá vốn `Cost`, `ProfitPercent`, `Price`, `OnHand`, `Reserved`, `Version`.
- **Luồng thay thế (Alternative):** Không có luồng thay thế riêng (hỗ trợ đối soát nếu `api.NeedsReconciliation` ở `ProductListForm.cs:48`).
- **Luồng ngoại lệ (Exception):**
  - `E1` (không có sản phẩm khớp bộ lọc hoặc lỗi tải): `total == 0` hiển thị thông báo rỗng tại `ProductListForm.cs:52`; ngoại lệ API được bắt ở `ProductListForm.cs:54` và `ProductDetailsForm.cs:93`.

#### UC06.02 — Thêm mới, chỉnh sửa sản phẩm, biến thể và giá bán
- **Luồng chính (Basic):**
  1. Mở `ProductEditor` (`src/PhoneStore.Admin/ProductEditor.cs:7-180`) từ nút Thêm/Sửa. Nếu tải ảnh từ máy, `UploadImagesAsync` (`L100-132`) gọi `api.UploadProductImageAsync` → `POST /api/admin/product-images` (`ProductImageEndpoints.cs:13-34`) → `ProductImageStorage.SaveAsync` (`src/PhoneStore.Web/ProductImages/ProductImageStorage.cs:17-56`) chuẩn hóa PNG tối đa 1600px, lưu theo SHA-256.
  2. Khi nhấn Lưu (`ProductEditor.SaveAsync` `L161-168`), `CatalogForms.Validate` (`src/PhoneStore.Admin.Client/Catalog.cs:58-68`) kiểm tra trước ở client, sau đó `CatalogWorkspace.Save` (`Catalog.cs:95-102`) gửi `POST /api/admin/products` hoặc `PUT /api/admin/products/{id}` (`ProductEndpoints.cs:12-18`) → `ProductService.Save` (`src/PhoneStore.Infrastructure/Commerce/ProductService.cs:22-66`).
  3. `ProductService.Save` kiểm tra `old.Any(x => !input.Variants.Any(...))` (không cho xóa biến thể cũ `L28-30`), kiểm tra `Rules.Version` từng biến thể cũ (`L31`), kiểm tra hãng/danh mục (`L32-34`), lưu `SAN_PHAM` (`L35-38`).
  4. Với mỗi biến thể (`L40-54`): biến thể mới tính `Price = Rules.Price(v.Cost, item.ProfitPercent)` (`L45`) và tạo `TON_KHO` với `OnHand = 0, Reserved = 0` (`L56`); biến thể cũ giữ nguyên `Cost` và chỉ tính lại `Price` khi đổi `ProfitPercent` (`L48-51`). Cuối cùng đồng bộ tối đa 12 ảnh vào `SAN_PHAM_ANH` (`L57-64`).
  5. `CatalogWorkspace.Save` đọc lại ngay `GET /api/admin/products/{id}` (`Catalog.cs:101`) để hiển thị giá bán và `Version` mới trên `ProductEditor`.
- **Luồng thay thế (Alternative):**
  - `A1` (nhấn nút Lập phiếu nhập sau khi lưu): `ProductEditor` kiểm tra `SavedMatches` & lượng gợi ý dương (`ProductEditor.cs:81-86, 134`), dựng `PurchaseHandoff` và mở `PurchaseEditor(api, handoff: handoff)` (`L84`); sau khi đóng phiếu nhập tự tải lại tồn & giá mới (`L85`).
- **Luồng ngoại lệ (Exception):**
  - `E1` (trùng Code/SKU/(Màu, GB), thiếu biến thể cũ, tỷ lệ ngoài 0–10.000%, sai định dạng ảnh, > 12 ảnh, hoặc lệch `Version`):
    - Client validate: `CatalogForms.Validate` (`Catalog.cs:58-68`), `ProductEditor.UploadImagesAsync` (`L111-112`: `IMAGE_TOO_LARGE` / `IMAGE_INVALID`).
    - Server validate & SQL: `ProductService.Save` (`ProductService.cs:24-62`, `Rules.Version` tại `Rules.cs:38-45`), trùng `Code`/`SKU`/`(ProductId, Color, StorageGb)` bị `BusinessKernel.Write` bắt SQL `2601/2627` tại `BusinessKernel.cs:74` và rollback toàn bộ.
    - Hiển thị lỗi tại `ProductEditor.cs:130, 167` và giữ nguyên dữ liệu đã nhập trên form.

#### UC06.03 — Ngừng hoặc mở lại kinh doanh sản phẩm
- **Luồng chính (Basic):**
  1. Từ `ProductListForm.ChangeStateAsync` (`src/PhoneStore.Admin/ProductListForm.cs:56-68`) hoặc bỏ/chọn cột `Active` trên `ProductEditor.SaveAsync` (`src/PhoneStore.Admin/ProductEditor.cs:165-167`), hiển thị hộp thoại xác nhận `ConfirmationDialog` (`CatalogUi.Confirm`).
  2. Gửi `PUT /api/admin/products/{id}` (`ProductEndpoints.cs:14-18`) → `ProductService.Save` (`ProductService.cs:31, 37, 51, 53`): kiểm tra `Rules.Version`, cập nhật `SAN_PHAM.Active` và `BIEN_THE.Active`, đánh dấu `db.Entry(v).Property(x => x.Active).IsModified = true` (`L51`) để luôn tăng `Version` và tải lại sản phẩm.
- **Luồng thay thế (Alternative):** Không có luồng thay thế riêng (hệ thống không xóa vật lý để giữ toàn vẹn `NO ACTION` ở `StoreDbContext.cs:141-142`).
- **Luồng ngoại lệ (Exception):**
  - `E1` (lệch `Version`): `Rules.Version` (`ProductService.cs:31` → `Rules.cs:44`) ném `CONCURRENT_UPDATE` (HTTP 409) → rollback và báo lỗi ở `ProductListForm.cs:65` hoặc `ProductEditor.cs:167`.

#### UC06.04 — Quản lý hãng và danh mục
- **Luồng chính (Basic):**
  1. Chọn "Hãng" hoặc "Danh mục" trên `MainForm` (`MainForm.cs:54`) mở `LookupForm(api, kind)` (`src/PhoneStore.Admin/LookupForm.cs:6-60`).
  2. `ReloadAsync` (`L34-39`) gọi `CatalogWorkspace.Lookups(kind)` (`Catalog.cs:81-90`) → `GET /api/admin/lookups/{kind}` (`LookupEndpoints.cs:13-14`) → `LookupService.List` (`LookupService.cs:18-23`).
  3. `SaveAsync` (`LookupForm.cs:40-58`) gửi `POST` / `PUT /api/admin/lookups/{kind}` (`LookupEndpoints.cs:15-21`) → `LookupService.Save` (`LookupService.cs:24-39`) cập nhật bảng `HANG` hoặc `DANH_MUC` và tải lại toàn danh sách.
- **Luồng thay thế (Alternative):** Không có luồng thay thế riêng (có hộp thoại xác nhận khi ngừng/mở kinh doanh ở `LookupForm.cs:44`).
- **Luồng ngoại lệ (Exception):**
  - `E1` (tên rỗng > 100 ký tự hoặc trùng tên hãng/danh mục): kiểm tra client ở `LookupForm.cs:43`; phía server ở `LookupService.cs:27` và SQL `2601/2627` trong `BusinessKernel.Write` (`BusinessKernel.cs:74`); `LookupForm.cs:57` khôi phục text đã nhập và báo lỗi trên `MessageLabel`.

---

### UC07: Quản lý khách hàng

#### UC07.01 — Tra cứu khách hàng và xem đơn của khách
- **Luồng chính (Basic):**
  1. `CustomerListForm` (`src/PhoneStore.Admin/ContactLists.cs:38-55`) gọi `ContactsWorkspace.Customers` (`src/PhoneStore.Admin.Client/Contacts.cs:45-46`) → `GET /api/admin/customers` (`CustomerAdminEndpoints.cs:12`) → `CustomerAdminService.List` (`src/PhoneStore.Infrastructure/Commerce/CustomerAdminService.cs:10-17`, luôn lọc `a.Role == "CUSTOMER"`).
  2. `CustomerDetailsForm.LoadAsync` (`src/PhoneStore.Admin/CustomerForms.cs:70-75`) → `GET /api/admin/customers/{id}` (`CustomerAdminEndpoints.cs:13`) → `CustomerAdminService.Read` (`CustomerAdminService.cs:18-24`).
  3. Nhấn "Đơn của khách" (`CustomerForms.cs:65`) mở `CustomerOrdersForm` (`L86-114`) gọi `ContactsWorkspace.Orders` (`Contacts.cs:64-67`) → `GET /api/admin/customers/{id}/orders` (`OrderQueryEndpoints.cs:16`) → `OrderQueryService.List(..., admin: true)` (`OrderQueryService.cs:14-24`). Chọn đơn mở `OrderDetailForm`.
- **Luồng thay thế (Alternative):** Không có luồng thay thế riêng.
- **Luồng ngoại lệ (Exception):**
  - `E1` (khách hàng không tồn tại): `CustomerAdminService.Read` (`CustomerAdminService.cs:22`) hoặc `OrderQueryService.List` (`OrderQueryService.cs:19`) ném `NOT_FOUND` (HTTP 404); `CustomerDetailsForm.LoadAsync` (`CustomerForms.cs:74`) xóa màn hình chi tiết và hiển thị lỗi.

#### UC07.02 — Cập nhật hồ sơ khách hàng
- **Luồng chính (Basic):**
  1. Mở `CustomerProfileEditor` (`src/PhoneStore.Admin/CustomerForms.cs:6-42`), bấm Lưu gọi `SaveAsync` (`L35-40`).
  2. Kiểm tra client bằng `ContactForms.ProfileError` (`src/PhoneStore.Admin.Client/Contacts.cs:19-25`), gọi `ContactsWorkspace.SaveProfile` (`Contacts.cs:49-53`) → `PUT /api/admin/customers/{id}` (`CustomerAdminEndpoints.cs:14`) → `CustomerAdminService.Update` (`CustomerAdminService.cs:26-30`).
  3. `CustomerAdminService.Update` validate qua `ProfileInput.Validate` (`L28`), chỉ cập nhật 4 trường `FullName`, `Phone`, `Email`, `Address` trên bảng `KHACH_HANG` (`L29`) và tải lại `AdminCustomer`.
- **Luồng thay thế (Alternative):** Không có luồng thay thế riêng.
- **Luồng ngoại lệ (Exception):**
  - `E1` (SĐT/Email trùng khách khác hoặc sai định dạng): client chặn tại `CustomerForms.cs:37`; server chặn bởi `ProfileInput.Validate` và SQL `2601/2627` trong `BusinessKernel.Write` (`BusinessKernel.cs:74`), rollback và báo lỗi ở `CustomerForms.cs:39`.

#### UC07.03 — Khóa hoặc mở khóa tài khoản khách hàng
- **Luồng chính (Basic):**
  1. Trên `CustomerDetailsForm`, nhấn `StateButton` → `ChangeStateAsync` (`src/PhoneStore.Admin/CustomerForms.cs:76-84`), xác nhận qua `ConfirmationDialog` (`L79`).
  2. Gửi `POST /api/admin/customers/{id}/state` kèm `{ Active, ExpectedActive }` (`CustomerAdminEndpoints.cs:15`) → `CustomerAdminService.SetActive` (`CustomerAdminService.cs:33-40`).
  3. Kiểm tra `current.Active == input.ExpectedActive` (`L36`), cập nhật `TAI_KHOAN.Active = input.Active` (`L38`); **nếu khóa (`!input.Active`), xóa ngay toàn bộ bản ghi của tài khoản trong bảng `PHIEN` qua `ExecuteDeleteAsync`** (`L39`), commit và tải lại `AdminCustomer`.
- **Luồng thay thế (Alternative):**
  - `A1` (Admin hủy xác nhận trên `ConfirmationDialog`): điều kiện `!confirm(...)` ở `CustomerForms.cs:79` trả về ngay, không gửi API và giữ nguyên trạng thái.
- **Luồng ngoại lệ (Exception):**
  - `E1` (trạng thái tài khoản vừa bị Admin khác thay đổi): `CustomerAdminService.SetActive` (`L36`) phát hiện `current.Active != input.ExpectedActive` và ném `CONCURRENT_UPDATE` (HTTP 409) → `CustomerForms.cs:82` báo lỗi yêu cầu tải lại.

---

### UC08: Nhà cung cấp, Nhập hàng & Tồn kho

#### UC08.01 — Quản lý nhà cung cấp
- **Luồng chính (Basic):**
  1. `SupplierListForm` (`src/PhoneStore.Admin/ContactLists.cs:57-78`) gọi `GET /api/admin/suppliers` (`SupplierEndpoints.cs:11-12`) → `SupplierService.List` (`SupplierService.cs:8-14`).
  2. `SupplierEditor.SaveAsync` (`src/PhoneStore.Admin/SupplierEditor.cs:51-57`) validate qua `ContactForms.SupplierError` (`Contacts.cs:27-32`), gọi `ContactsWorkspace.SaveSupplier` (`Contacts.cs:96-102`) → `POST` / `PUT /api/admin/suppliers` (`SupplierEndpoints.cs:13-19`) → `SupplierService.Save` (`src/PhoneStore.Infrastructure/Commerce/SupplierService.cs:15-22`) lưu bảng `NHA_CUNG_CAP` và tìm đọc lại từ danh sách.
- **Luồng thay thế (Alternative):** Không có luồng thay thế riêng (có hộp thoại xác nhận khi chuyển trạng thái `Active` ở `SupplierEditor.cs:54`).
- **Luồng ngoại lệ (Exception):**
  - `E1` (trùng mã NCC hoặc SĐT không hợp lệ): client chặn tại `SupplierEditor.cs:53`; server chặn bởi `Rules.Phone` (`SupplierService.cs:19`) và SQL `2601/2627` trong `BusinessKernel.Write` (`BusinessKernel.cs:74`); báo lỗi tại `SupplierEditor.cs:56`.

#### UC08.02 — Lập, sửa và hủy phiếu nhập nháp
- **Luồng chính (Basic):**
  1. `PurchaseListForm` (`src/PhoneStore.Admin/PurchaseForms.cs:7-30`) liệt kê `PHIEU_NHAP` qua `GET /api/admin/purchases` (`PurchaseEndpoints.cs:11`).
  2. Mở `PurchaseEditor` (`PurchaseForms.cs:33-103`), chọn NCC hoạt động (`SupplierPickerForm` / `ComboBox`), chọn biến thể từ `SupplyVariantPicker` (`src/PhoneStore.Admin/SupplyVariantPicker.cs:6-23`), nhấn Lưu nháp (`SaveAsync` `L88-91`) → `PurchaseDraftSession.Save` (`src/PhoneStore.Admin.Client/Supply.cs:149-156`) gửi `POST` (kèm `RequestKey`) hoặc `PUT` (kèm `Version`) tới `PurchaseEndpoints` (`PurchaseEndpoints.cs:13-18`) → `PurchaseService.Save` (`src/PhoneStore.Infrastructure/Commerce/PurchaseService.cs:32-64`).
  3. `PurchaseService.Save` kiểm tra NCC đang `Active` (`L55`), biến thể tồn tại (`L57`), tổng hợp `Total` và cập nhật `PHIEU_NHAP` + `PHIEU_NHAP_CT` (`L58-62`).
- **Luồng thay thế (Alternative):**
  - `A1` (lập phiếu từ `ProductEditor` qua `PurchaseHandoff`): `PurchaseEditor.LoadAsync` (`PurchaseForms.cs:84`) gọi `SupplyWorkspace.FromProduct` (`Supply.cs:98-103`) điền sẵn biến thể và `SuggestedQuantity` vào lưới nháp (chưa lưu cho đến khi bấm Lưu nháp).
  - `A2` (hủy phiếu nháp `NHAP -> DA_HUY`): nhấn `CancelDraftButton` → `PurchaseEditor.ActAsync("cancel")` (`PurchaseForms.cs:93-102`) → `SupplyWorkspace.Execute` (`Supply.cs:104-109`) → `POST /api/admin/purchases/{id}/cancel` (`PurchaseEndpoints.cs:19-20`) → `PurchaseService.Cancel` (`PurchaseService.cs:66-72`).
- **Luồng ngoại lệ (Exception):**
  - `E1` (phiếu không còn `NHAP`, NCC đã ngừng, trùng `RequestKey` khác nội dung hoặc lệch `Version`):
    - Client validate: `SupplyPresentation.LinesError` (`Supply.cs:36-42`).
    - Server kiểm tra: `PurchaseService.Save` (`PurchaseService.cs:35-58`) kiểm tra `RequestKey` (`L41-49`), `Rules.Version` (`L53`), `row.Status != "NHAP"` (`L54`), `Suppliers.Any(Active)` (`L55`), `PurchaseService.Cancel` (`L69-71`).
    - Hiển thị lỗi tại `PurchaseForms.cs:90, 102` (kèm cơ chế đối soát `ResolveCreation` / `Resolve` nếu lỗi mạng/5xx).

#### UC08.03 — Xác nhận phiếu nhập kho
- **Luồng chính (Basic):**
  1. Trên `PurchaseEditor` (phiếu `NHAP` đã lưu khớp thay đổi), Admin nhấn "Xác nhận phiếu" → `ActAsync("confirm")` (`src/PhoneStore.Admin/PurchaseForms.cs:93-102`), xác nhận trên `ConfirmationDialog` (`L98`), tạo `PurchaseOperation` kèm `OperationKey` (`Intent.Key`).
  2. `SupplyWorkspace.Execute` (`Supply.cs:104-109`) gửi `POST /api/admin/purchases/{id}/confirm` (`PurchaseEndpoints.cs:21-22`) → `PurchaseService.Confirm` (`src/PhoneStore.Infrastructure/Commerce/PurchaseConfirmation.cs:9-31`) trong `BusinessKernel.Write` (`Serializable` + `sp_getapplock Exclusive`).
  3. Kiểm tra `Version` (`L13`), `Status == "NHAP"` (`L14`), có dòng nhập và NCC đang `Active` (`L16`), `total == receipt.Total` (`L18`), `OperationKey` chưa dùng (`L19`).
  4. Lặp từng dòng theo `VariantId` tăng dần (`L20-29`): tăng `TON_KHO.OnHand += Quantity` (`L25`), cập nhật `BIEN_THE.Cost` và tính lại `Price = Rules.Price(line.Cost, variant.ProfitPercent)` (`L24, L27`), ghi `NHAT_KY_KHO` (`Kind = "NHAP"`, `DeltaOnHand = Quantity`, `OperationKey`, `L28`).
  5. Chuyển `PHIEU_NHAP.Status = "DA_XAC_NHAN"`, ghi `ConfirmerId`, `ConfirmedAt` (`L30`), commit và `SupplyWorkspace.Readback` (`Supply.cs:88-97`) tải lại tồn & giá mới về tab tác động.
- **Luồng thay thế (Alternative):** Không có luồng thay thế riêng.
- **Luồng ngoại lệ (Exception):**
  - `E1` (phiếu rỗng, NCC ngừng, tổng lệch, `Version` lệch, phiếu đã chốt, vượt `int.MaxValue`, hoặc lặp `OperationKey`):
    - Ném lỗi `NOT_FOUND` / `CONCURRENT_UPDATE` / `INVALID_STATE` / `INVALID_INPUT` tại `PurchaseConfirmation.cs:12-26` → `BusinessKernel.Write` rollback toàn bộ, không tăng tồn kho lần hai.
    - Client bắt tại `PurchaseForms.cs:102` (hiển thị `PublicValidation` hoặc yêu cầu đối soát nếu `NeedsReadback`).

#### UC08.04 — Tra cứu tồn kho và nhật ký biến động kho
- **Luồng chính (Basic):**
  1. Mở `StockListForm` (`src/PhoneStore.Admin/StockForms.cs:16-42`), `LoadAsync` (`L33-42`) gọi `SupplyWorkspace.Stock` (`Supply.cs:79-80`) → `GET /api/admin/stock` (`InventoryEndpoints.cs:11`) → `InventoryService.List` (`src/PhoneStore.Infrastructure/Commerce/InventoryService.cs:9-20`) trả `OnHand`, `Reserved`, `Available = OnHand - Reserved`.
  2. Lọc hàng sắp hết theo ngưỡng tùy chỉnh được lưu cục bộ tại `StockPreferenceStore` (`StockForms.cs:7-13`, tệp `stock-settings.json`).
  3. Nhấn "Nhật ký kho" (`StockForms.cs:29`) mở `StockJournalForm` (`L83-97`) → `GET /api/admin/stock/movements` (`InventoryEndpoints.cs:12`) → `InventoryService.Movements` (`InventoryService.cs:22-31`) đọc bảng `NHAT_KY_KHO`.
- **Luồng thay thế (Alternative):** Không có luồng thay thế riêng.
- **Luồng ngoại lệ (Exception):**
  - `E1` (ngưỡng lọc âm `< 0` hoặc `brandId <= 0`): UI dùng `NumericUpDown` `Minimum = 0` (`StockForms.cs:20`), `StockPreferenceStore.Save` kiểm tra `value < 0` (`L11`), server kiểm tra tại `InventoryService.cs:12` ném `INVALID_INPUT` (HTTP 400); báo lỗi tại `StockForms.cs:41, 96`.

#### UC08.05 — Kiểm kê và điều chỉnh tồn kho
- **Luồng chính (Basic):**
  1. Chọn biến thể trên `StockListForm`, mở `StockCountDialog` (`src/PhoneStore.Admin/StockForms.cs:46-79`), nhập số thực tế `Counted` và lý do, bấm Gửi (`SubmitAsync` `L61-69`) và xác nhận qua `ConfirmationDialog`.
  2. `SupplyWorkspace.Adjust` (`Supply.cs:115-120`) gửi `POST /api/admin/stock/{variantId}/adjust` (`InventoryEndpoints.cs:13`) → `InventoryService.Adjust` (`src/PhoneStore.Infrastructure/Commerce/InventoryService.cs:33-43`).
  3. Kiểm tra `Counted >= 0 && Counted >= row.Reserved && Counted != row.OnHand` (`L38`), khớp `Version` (`L37`), chưa dùng `OperationKey` (`L39`), cập nhật nguyên tử `UPDATE dbo.TON_KHO SET OnHand = Counted WHERE VariantId = ... AND Version = ... AND Reserved <= Counted` (`L40`), ghi `NHAT_KY_KHO` (`Kind = "DIEU_CHINH"`, `DeltaOnHand = Counted - row.OnHand`, `Reason`, `OperationKey` `L42`), commit và tải lại dòng kho mới.
- **Luồng thay thế (Alternative):** Không có luồng thay thế riêng (có `ResolveAsync` tại `StockForms.cs:70-78` đối soát nhật ký theo `OperationKey` khi gặp lỗi mạng/không chắc chắn).
- **Luồng ngoại lệ (Exception):**
  - `E1` (thiếu lý do, `Counted < Reserved`, `Counted == OnHand`, lệch `Version`, lặp `OperationKey`):
    - Client validate ở `SupplyPresentation.CountError` (`Supply.cs:43-48`).
    - Server ném `INVALID_INPUT` / `CONCURRENT_UPDATE` / `INVALID_STATE` ở `InventoryService.Adjust` (`InventoryService.cs:35-41`) → rollback và hiển thị lỗi tại `StockForms.cs:68`.

---

### UC09: Xử lý đơn hàng & Hóa đơn quản trị

#### UC09.01 — Tra cứu đơn hàng quản trị và in hóa đơn bán hàng
- **Luồng chính (Basic):**
  1. `OrderListForm` (`src/PhoneStore.Admin/OrderForms.cs:6-28`) gọi `OrdersWorkspace.List` (`src/PhoneStore.Admin.Client/Orders.cs:55-60`) → `GET /api/admin/orders` (`OrderQueryEndpoints.cs:14`) → `OrderQueryService.List(..., admin: true)` (`OrderQueryService.cs:14-24`).
  2. `OrderDetailForm` (`OrderForms.cs:32-71`) gọi `GET /api/admin/orders/{id}` (`OrderQueryEndpoints.cs:15`) → `OrderQueryService.Read(..., admin: true)` hiển thị dòng snapshot, thanh toán COD và lịch sử `LICH_SU_DON`.
  3. Nhấn "Xem hóa đơn" mở `InvoiceForm` (`src/PhoneStore.Admin/InvoiceForm.cs:31-58`) → `OrdersWorkspace.Invoice` (`Orders.cs:62`) → `GET /api/admin/orders/{id}/invoice` (`InvoiceEndpoints.cs:7`) → `InvoiceService.Read` (`src/PhoneStore.Infrastructure/Commerce/InvoiceService.cs:6-11`) tạo chứng từ nội bộ `SalesInvoice`. Bấm `PrintPreview` gọi `InvoicePrinter.Preview` (`InvoiceForm.cs:14-29`).
- **Luồng thay thế (Alternative):**
  - `A1` (máy Admin chưa cài máy in/driver): `InvoiceForm.Preview` (`InvoiceForm.cs:54`) kiểm tra `!printer.HasInstalledPrinter` (`PrinterSettings.InstalledPrinters.Count > 0` tại `L13`), không crash mà giữ nguyên toàn bộ nội dung hóa đơn snapshot trên `ContentBox` và thông báo rõ cho Admin.
- **Luồng ngoại lệ (Exception):**
  - `E1` (đơn ở trạng thái `CHO_XAC_NHAN`, `DA_HUY` hoặc `GIAO_THAT_BAI`): `InvoiceService.Read` (`src/PhoneStore.Infrastructure/Commerce/InvoiceService.cs:9`) ném `BusinessException("INVALID_STATE", ...)` (HTTP 409) → `InvoiceForm.LoadAsync` (`InvoiceForm.cs:49`) bắt lỗi và hiển thị trên `MessageLabel`. (Lỗi `From > To` khi lọc đơn bị chặn ở `Orders.cs:57`).

#### UC09.02 — Xác nhận hoặc hủy đơn hàng (Quản trị)
- **Luồng chính (Basic):**
  1. Trên `OrderDetailForm`, bấm "Xác nhận đơn" (`OrderAction.Confirm`) hoặc nhập lý do rồi bấm "Hủy đơn" (`OrderAction.Cancel`) → `OrderDetailForm.ActAsync` (`src/PhoneStore.Admin/OrderForms.cs:59-68`), xác nhận qua `ConfirmationDialog` (`L63`).
  2. `OrdersWorkspace.Execute` (`src/PhoneStore.Admin.Client/Orders.cs:63-69`) gửi `POST /api/admin/orders/{id}/confirm` hoặc `/cancel` (`OrderDecisionEndpoints.cs:13-14`) kèm `OrderCommand(Version, OperationKey, Reason)`.
  3. `OrderWorkflow.Confirm` (`OrderDecisions.cs:28-33`) kiểm tra `Status == "CHO_XAC_NHAN"` → chuyển sang `DA_XAC_NHAN` và ghi `LICH_SU_DON`.
  4. `OrderWorkflow.Cancel(..., admin: true)` (`OrderDecisions.cs:35-47`) kiểm tra `Status is "CHO_XAC_NHAN" or "DA_XAC_NHAN"`, bắt buộc lý do, giảm `TON_KHO.Reserved`, ghi `NHAT_KY_KHO` (`Kind = "HUY_GIU"`), chuyển sang `DA_HUY` và ghi `LICH_SU_DON`.
  5. Tải lại `AdminOrder` mới về `OrderDetailForm`.
- **Luồng thay thế (Alternative):** Không có luồng thay thế riêng (UI chỉ bật các nút hợp lệ theo `OrderPresentation.Allowed` tại `Orders.cs:28-36`).
- **Luồng ngoại lệ (Exception):**
  - `E1` (sai trạng thái, thiếu lý do hủy, lệch `Version`, hoặc lặp `OperationKey`):
    - Client validate lý do: `OrderPresentation.ReasonError` (`Orders.cs:40`), chặn ở `OrderForms.cs:61`.
    - Server kiểm tra: `OrderDecisions.cs:11-43` (`NOT_FOUND`, `CONCURRENT_UPDATE`, `INVALID_STATE`, `INVALID_INPUT`) → rollback toàn bộ.
    - Client bắt HTTP 409 tại `OrderForms.cs:67` và tự động gọi `workspace.Resolve(operation)` để tải lại trạng thái mới nhất lên màn hình.

#### UC09.03 — Xuất giao, hoàn tất thu COD, giao thất bại và nhận lại hàng
- **Luồng chính (Basic):**
  - Toàn bộ qua `OrderDetailForm.ActAsync` (`OrderForms.cs:59-68`) → `OrdersWorkspace.Execute` (`Orders.cs:63-69`) → `OrderFulfillmentEndpoints` (`src/PhoneStore.Web/Endpoints/OrderFulfillmentEndpoints.cs:11-14`) → `OrderWorkflow` (`src/PhoneStore.Infrastructure/Commerce/OrderFulfillment.cs:6-55`) trong `BusinessKernel.Write`:
  1. *Xuất giao (`Dispatch`, `OrderFulfillment.cs:9-21`):* kiểm tra `Status == "DA_XAC_NHAN"`, trừ đồng thời `TON_KHO.OnHand` và `Reserved` (`L16`), ghi `NHAT_KY_KHO` (`Kind = "XUAT_GIAO"`, `DeltaOnHand = -Qty`, `DeltaReserved = -Qty` `L18`), chuyển đơn sang `DANG_GIAO`, ghi `LICH_SU_DON`.
  2. *Hoàn tất & thu COD (`Complete`, `OrderFulfillment.cs:23-31`):* kiểm tra `Status == "DANG_GIAO"`, cập nhật `THANH_TOAN` (`Status = "DA_THU"`, `Amount = order.Total`, `ActorId`, `CollectedAt = completedAt` `L29`), chuyển đơn sang `HOAN_TAT`, gán `order.CompletedAt = completedAt` (`L30`) và ghi `LICH_SU_DON`.
  3. *Giao thất bại (`Fail`, `OrderFulfillment.cs:33-39`):* kiểm tra `Status == "DANG_GIAO"`, bắt buộc lý do (`L37`), chuyển đơn sang `GIAO_THAT_BAI`, ghi `LICH_SU_DON` (chưa cộng lại tồn kho).
  4. *Nhận lại hàng (`ReceiveReturn`, `OrderFulfillment.cs:41-55`):* kiểm tra `Status == "GIAO_THAT_BAI"` và chưa tồn tại `NHAT_KY_KHO` có `Kind == "NHAN_LAI"` cho đơn (`L44`), cộng lại `TON_KHO.OnHand += Quantity` (`L49`), ghi `NHAT_KY_KHO` (`Kind = "NHAN_LAI"` `L50`), đánh dấu `Property(x => x.Status).IsModified = true` để **tăng `Version` nhưng giữ nguyên trạng thái `GIAO_THAT_BAI`** (`L53`), ghi `LICH_SU_DON`.
- **Luồng thay thế (Alternative):**
  - `A1` (nhận lại hàng với lý do tùy chọn & chỉ mở nút 1 lần): `ReceiveReturn` dùng `optional: true` cho `Reason` (`OrderFulfillment.cs:45`), và `OrderPresentation.Allowed` (`Orders.cs:34`) chỉ bật nút khi `status == "GIAO_THAT_BAI" && !returned`.
- **Luồng ngoại lệ (Exception):**
  - `E1` (sai trạng thái, thiếu lý do `Fail`, lặp `NHAN_LAI` / `OperationKey`, hoặc lệch `Version`):
    - Kiểm tra & ném lỗi tại `OrderWorkflow.Load/Unpaid/KeyUnused` (`OrderDecisions.cs:9-26`) và `OrderFulfillment.cs:12-49` (`INVALID_STATE`, `CONCURRENT_UPDATE`, `INVALID_INPUT`) kèm chỉ mục duy nhất `(OrderId, VariantId, Kind)` trong `StoreDbContext.cs:136`.
    - Bắt lỗi & tự đối soát lại đơn khi 409 tại `OrderForms.cs:67`.

---

### UC10: Tổng quan & Báo cáo

#### UC10.01 — Tổng quan và xem báo cáo doanh thu, bán chạy, tồn kho
- **Luồng chính (Basic):**
  1. *Màn hình Tổng quan (`DashboardView`, `src/PhoneStore.Admin/DashboardView.cs:6-41`):* `LoadAsync` (`L30-41`) chạy song song `ReportsWorkspace.Orders("CHO_XAC_NHAN")`, `Orders("DANG_GIAO")`, `Unreceived()`, `LowStock(threshold)`, `Revenue(from, to)`, `Revenue(today, today)` và biểu đồ doanh thu 7 ngày (`SevenDays` tại `Reports.cs:30`). Bấm từng nút chỉ số mở ngay màn hình tương ứng (`L24`).
  2. *Màn hình Báo cáo (`ReportScreen`, `src/PhoneStore.Admin/ReportScreen.cs:42-73`):* chọn loại báo cáo (`revenue` / `bestsellers` / `stock`), khoảng ngày UTC+7 và tùy chọn Nhóm theo tháng (`MonthlyBox`).
  3. `LoadAsync` (`ReportScreen.cs:67-73`) → `ReportsWorkspace.Read(Kind, from, to)` (`src/PhoneStore.Admin.Client/Reports.cs:35-38`) → `GET /api/admin/reports?kind=...&from=...&to=...` (`ReportEndpoints.cs:7`) → `ReportService.Read` (`src/PhoneStore.Infrastructure/Commerce/ReportService.cs:8-27`).
  4. `ReportService.Read` trong `BusinessKernel.Read`:
     - Nếu `kind == "stock"` (`L11-15`): đọc `TON_KHO` join `BIEN_THE`, `SAN_PHAM`, tính `Available = OnHand - Reserved`.
     - Nếu `kind is "revenue" or "bestsellers"` (`L16-26`): quy đổi khoảng ngày UTC+7 qua `DateWindow.Parse` (`L17`), lọc đơn `HOAN_TAT` join `THANH_TOAN` (`COD`, `DA_THU`, `Amount == Total`) theo `CompletedAt` (`L18-19`), gom nhóm doanh thu theo ngày UTC+7 (`L22`) và bán chạy theo biến thể (`L23-25`).
  5. Trả về một đối tượng `StoreReport` → `AdminReport`; `ReportPresentation.Project` (`Reports.cs:19-29`) sinh đồng bộ cả `Rows` cho `DataGridView` (phân trang 20 dòng tại `ReportScreen.cs:60-64`) và `Points` cho `ReportChart` (`ReportScreen.cs:6-39`).
- **Luồng thay thế (Alternative):**
  - `A1` (chọn báo cáo "Tồn hiện tại" `kind == "stock"`): `ReportScreen.Actions` (`ReportScreen.cs:59`) vô hiệu `FromBox`, `ToBox` (`Kind != "stock"`) và `MonthlyBox` (`Kind == "revenue"`); `ReportPresentation.DateError` (`Reports.cs:15`) bỏ qua kiểm tra ngày và `ReportsWorkspace.Read` (`L38`) không gửi `from`/`to` lên API.
- **Luồng ngoại lệ (Exception):**
  - `E1` (loại báo cáo không hợp lệ hoặc `Từ ngày > Đến ngày` / thiếu ngày):
    - Chặn sớm tại client ở `ReportPresentation.DateError` (`Reports.cs:15`, hiển thị `DateErrorLabel` tại `ReportScreen.cs:69`, không gọi máy chủ) và `ReportsWorkspace.Read` (`L37`).
    - Chặn độc lập ở server tại `ReportService.Read` (`ReportService.cs:16`) và `DateWindow.Parse` (`DateWindow.cs:9, 17`) ném `INVALID_INPUT` (HTTP 400).
  - `E2` (không có đơn phát sinh trong kỳ): `ReportService.Read` trả `Revenue = 0`, `Quantity = 0`, `Orders = 0`, danh sách `Daily` / `Bestsellers` rỗng (`L26`); `ReportScreen.RenderPage` (`ReportScreen.cs:65`) và `ReportChart.Bind` (`L20`) hiển thị thông báo không có dữ liệu & tổng doanh thu bằng 0.

---

## 4. Trace luồng dữ liệu 10 Sequence Diagram (2.6.1 – 2.6.10)

### 2.6.1 Sequence UC01 — Đăng nhập khách hàng
- **Participants thật:** `Khách hàng` → `AccountController` (`src/PhoneStore.Web/Controllers/AccountController.cs`) → `AccountSessions` (`src/PhoneStore.Infrastructure/Accounts/AccountSessions.cs`) → `CSDL (TAI_KHOAN, PHIEN)` (`StoreDbContext.cs`).

| Bước | Chiều gọi | Dữ liệu đầu vào | Vị trí mã nguồn | Xử lý & Tác động CSDL / Lock | Dữ liệu trả về |
| :---: | :--- | :--- | :--- | :--- | :--- |
| 1 | Khách → Controller | `GET /Account/Login?returnUrl=...` | `AccountController.cs:55-57` | Kiểm tra chưa đăng nhập, lọc `LocalDestination(returnUrl)` (`L24`) | `View(LoginViewModel)` (HTTP 200 HTML form + Antiforgery hidden field) |
| 2 | Khách → Controller | `LoginViewModel { Username, Password, ReturnUrl }` + `__RequestVerificationToken` (`POST /Account/Login`) | `AccountController.cs:60-70` | Validate Antiforgery + `ModelState.IsValid` | Gọi `MvcStoreAdapter.LoginAsync(username, password)` (`MvcStoreAdapter.cs:31-32`) |
| 3 | Adapter → Service | `LoginRequest(Username, Password, Surface: "customer")` | `AccountSessions.cs:18-29` | Ánh xạ `Surface -> "CUSTOMER"`, chuẩn hóa `username` chữ thường | Ném `UNAUTHORIZED` ngay nếu rỗng/quá dài (`L28`) |
| 4 | Self (Service) | — | `AccountSessions.cs:30-35` | `BeginTransactionAsync(Serializable)` + `sp_getapplock ('PhoneStore:write', Exclusive)` | Giao dịch độc quyền đã khóa |
| 5 | Service ↔ CSDL | `username` | `AccountSessions.cs:37` | `SELECT ... FROM TAI_KHOAN WHERE Username = @username` | `Account?` (`Id, PasswordHash, Active, Role`) |
| 6 | Self (Service) | `account`, `request.Password`, `DummyHash` | `AccountSessions.cs:38-40` | `VerifyHashedPassword`, kiểm tra `Active == true && Role == "CUSTOMER"` | Tiếp tục hoặc ném `BusinessException("UNAUTHORIZED")` (`L40`) |
| 7 | `alt` E1 (Lỗi) | `BusinessException("UNAUTHORIZED")` | `AccountController.cs:79-85` | Không ghi `PHIEN`; xóa sạch password trên `ModelState` (`L83`) | `View(LoginViewModel)` kèm thông báo `LoginError` (`L18`) |
| 8 | `opt` Rehash | `PasswordVerificationResult.SuccessRehashNeeded` | `AccountSessions.cs:42-43` | Cập nhật `account.PasswordHash = passwords.HashPassword(...)` | Đánh dấu entity modified |
| 9 | Service ↔ CSDL | `Account.Id`, `SessionPolicy.Lifetime` (8h) | `AccountSessions.cs:46-51` | `INSERT INTO PHIEN(Id, AccountId, ExpiresAt)` + `CommitAsync` | `AccountSession(AccountIdentity, SessionId, ExpiresAt)` |
| 10 | Controller → Khách | `AccountSession` | `AccountController.cs:71-77` | `ClaimsPrincipal` + `SignInAsync` cấp cookie `PhoneStore.Session` (`HttpOnly, Lax, AllowRefresh=false`) | `Set-Cookie` + `LocalRedirect(model.ReturnUrl)` |

---

### 2.6.2 Sequence UC02 — Tìm kiếm, lọc & xem chi tiết điện thoại
- **Participants thật:** `Khách hàng` → `CatalogController` (`src/PhoneStore.Web/Controllers/CatalogController.cs`) → `LookupService & CatalogService` (`src/PhoneStore.Infrastructure/Commerce/`) → `CSDL (HANG, DANH_MUC, SAN_PHAM, BIEN_THE, TON_KHO, SAN_PHAM_ANH)`.

| Bước | Chiều gọi | Dữ liệu đầu vào | Vị trí mã nguồn | Xử lý & Tác động CSDL / Lock | Dữ liệu trả về |
| :---: | :--- | :--- | :--- | :--- | :--- |
| 1 | Khách → Controller | `GET /Catalog/Index` + `CatalogQuery` (`q, brandId, minPrice, maxPrice, storageGb, color, sort, page, pageSize`) | `CatalogController.cs:13-16` | Kiểm tra `CatalogQuery.Validate` (`CatalogViewModels.cs:30-39`) | — |
| 2 | Controller → LookupService | `kind = "brands"` | `MvcStoreAdapter.cs:21-22` → `LookupService.cs:10-11` | `SELECT Id, Name, Active FROM HANG WHERE Active = 1 ORDER BY Name` | `List<CatalogBrand>` |
| 3 | Controller → CatalogService | `CatalogFilter` (từ `query.ToFilter()`) | `CatalogController.cs:20` → `CatalogService.cs:41-47` | `BusinessKernel.Read` (`Serializable` + `sp_getapplock Shared`) | — |
| 4 | `alt` E1 (Lọc/Sắp xếp sai) | `MinPrice < 0` hoặc `MinPrice > MaxPrice` hoặc `Sort` lạ | `CatalogService.cs:13-16, 37`; `CatalogController.cs:21-26` | Ném `INVALID_INPUT`; controller giữ form (200 nếu lỗi lọc, 400 nếu lỗi `Page`/`PageSize`) | HTML trang danh mục kèm thông báo lỗi |
| 5 | Service ↔ CSDL | `CatalogFilter` hợp lệ | `CatalogService.cs:17-46` | Lọc `SAN_PHAM`, `HANG`, `DANH_MUC`, `BIEN_THE` đều `Active = 1`; đọc `TON_KHO` (`Available = OnHand - Reserved`) | `Page<PublicProduct>` (mỗi SP gồm biến thể + tồn + ảnh) |
| 6 | Controller → Khách | `CatalogIndexViewModel(query, brands, results)` | `CatalogController.cs:20`; `CatalogViewModels.cs:67-74` | `ProductCardViewModel.From` chọn giá bán thấp nhất để hiển thị | HTML danh sách điện thoại phân trang |
| 7 | `opt` Chi tiết: Khách → Controller | `GET /Catalog/Details/{id}?variantId=...` | `CatalogController.cs:30-38` | Gọi `store.ReadProductAsync(id, variantId)` (`MvcStoreAdapter.cs:15-20`) | — |
| 8 | Service ↔ CSDL | `id`, `variantId?` | `CatalogService.cs:58-71`; `MvcStoreAdapter.cs:24-28` | Đọc `SAN_PHAM` + `HANG` + `DANH_MUC` (`Active = 1`), `BIEN_THE` + `TON_KHO`, `SAN_PHAM_ANH` | `PublicProduct` hoặc ném `NOT_FOUND` (HTTP 404 qua `StorefrontErrors`) |
| 9 | Controller → Khách | `ProductDetailsViewModel(product, selected)` | `CatalogController.cs:35-37` | Chọn `variantId` truyền vào hoặc biến thể đầu tiên `Available > 0` | HTML trang chi tiết hoặc trang lỗi 404 |

---

### 2.6.3 Sequence UC03 — Đặt hàng COD
- **Participants thật:** `Khách hàng` → `CartController` (`src/PhoneStore.Web/Controllers/CartController.cs`) → `CartService & CheckoutService` (`src/PhoneStore.Infrastructure/Commerce/`) → `CSDL (GIO_HANG_CT, DON_HANG, DON_HANG_CT, THANH_TOAN, TON_KHO, NHAT_KY_KHO, LICH_SU_DON)`.

| Bước | Chiều gọi | Dữ liệu đầu vào | Vị trí mã nguồn | Xử lý & Tác động CSDL / Lock | Dữ liệu trả về |
| :---: | :--- | :--- | :--- | :--- | :--- |
| 1 | Khách → Controller | `CheckoutViewModel { RequestKey, Confirmation, Recipient, Phone, Address, PaymentMethod="COD" }` (`POST /Cart/Checkout`) | `CartController.cs:46-55` | Giải mã `Confirmation` qua `CheckoutConfirmation.Read` (`CheckoutConfirmation.cs:16-25`) lấy `List<PriceConfirmation>` đã ký | Gọi `MvcStoreAdapter.PlaceOrderAsync` |
| 2 | Controller → CheckoutService | `Actor(AccountId, SessionId)`, `CheckoutInput` | `CheckoutService.cs:17-27` | `BusinessKernel.Write` (`Serializable` + `sp_getapplock Exclusive`), xác thực `Require(actor, "CUSTOMER")`, kiểm tra `COD`, input & dòng giá | — |
| 3 | Service ↔ CSDL | `input.RequestKey` | `CheckoutService.cs:28-36` | `SELECT * FROM DON_HANG WHERE RequestKey = @key` | `previous?` |
| 4 | `alt` Trùng `RequestKey` hợp lệ | `previous != null`, cùng khách & khớp người nhận + `OrderLines` | `CheckoutService.cs:30-35` | Không trừ kho, không tạo đơn mới (idempotent) | Trả `previous.Id` → `OrderReadModel.Load` → Redirect chi tiết đơn |
| 5 | Service ↔ CSDL | `actor.AccountId` | `CheckoutService.cs:38-40`; `CartService.cs:27-32` | Đọc `GIO_HANG_CT` và chạy `CartService.Read` → `Preview` lấy giá hiện hành & `Available` | `cart`, `current (CartView)` |
| 6 | `alt` Lỗi đặt hàng (`E1`/`E2`) | Giỏ rỗng / Giá đổi / Ngừng bán / Thiếu tồn | `CheckoutService.cs:39-46`; `CartController.cs:59-92` | `BusinessKernel.Write` rollback toàn bộ; ném `PriceChangedException` (`L42`) hoặc `BusinessException` | Giữ trang Checkout; nếu đổi giá bật `PriceChanged = true` & ký lại `Confirmation` mới |
| 7 | Service → CSDL | `Order`, `Payment` | `CheckoutService.cs:47-49` | `INSERT DON_HANG` (`Status="CHO_XAC_NHAN"`) + `INSERT THANH_TOAN` (`Method="COD", Status="CHUA_THU"`) | `order.Id` |
| 8 | `loop` Từng dòng giỏ | `line` (theo `VariantId` tăng dần) | `CheckoutService.cs:51-57` | `UPDATE dbo.TON_KHO SET Reserved += Qty WHERE OnHand - Reserved >= Qty`; nếu `!= 1` ném `OUT_OF_STOCK` (rollback); `INSERT DON_HANG_CT` (snapshot) + `INSERT NHAT_KY_KHO` (`Kind="GIU"`) | — |
| 9 | Service ↔ CSDL | `order.Id`, `cart` | `CheckoutService.cs:58-61` | `INSERT LICH_SU_DON (To="CHO_XAC_NHAN")`, `DELETE GIO_HANG_CT`, `CommitAsync`, sau đó `OrderReadModel.Load` | `OrderView` |
| 10 | Controller → Khách | `id` (`order.Id`) | `CartController.cs:56-57` | Đặt `TempData["Notice"]` thành công | `RedirectToAction("Details", "Orders", new { id })` |

---

### 2.6.4 Sequence UC04 — Khách hàng hủy đơn chờ xác nhận
- **Participants thật:** `Khách hàng` → `OrdersController` (`src/PhoneStore.Web/Controllers/OrdersController.cs`) → `OrderWorkflow` (`src/PhoneStore.Infrastructure/Commerce/OrderDecisions.cs`) → `CSDL (DON_HANG, TON_KHO, NHAT_KY_KHO, LICH_SU_DON)`.

| Bước | Chiều gọi | Dữ liệu đầu vào | Vị trí mã nguồn | Xử lý & Tác động CSDL / Lock | Dữ liệu trả về |
| :---: | :--- | :--- | :--- | :--- | :--- |
| 1 | Khách → Controller | `CancelOrderForm { Id, Reason, Version }` (`POST /Orders/Cancel`) | `OrdersController.cs:42-50` | Đọc trước `ReadCustomerOrderAsync(Current(), form.Id)` (`L45`) đảm bảo đúng chủ đơn (404 nếu lạ) | — |
| 2 | Controller → OrderWorkflow | `Actor`, `id`, `VersionedCommand(Version, Guid.NewGuid(), Reason)`, `admin: false` | `MvcStoreAdapter.cs:60-61` → `OrderDecisions.cs:35-39` | `BusinessKernel.Write` (`Serializable` + `sp_getapplock Exclusive`) | — |
| 3 | Service ↔ CSDL | `actor`, `id`, `command` | `OrderDecisions.cs:9-26, 37-39` | `Load` kiểm tra `PHIEN`, `CustomerId`, `Version`, tổng chốt; `Unpaid` kiểm tra `COD/CHUA_THU`; kiểm tra `Status == "CHO_XAC_NHAN"`, `Reason` & `KeyUnused` | `Order` & `OrderLines` hoặc ném lỗi |
| 4 | `alt` E1 (Lỗi hủy) | Lệch `Version` / Không còn `CHO_XAC_NHAN` / Thiếu lý do / Không phải chủ đơn | `OrderDecisions.cs:12-39`; `OrdersController.cs:55-60` | `BusinessKernel.Write` rollback toàn bộ | Controller thông báo đơn đã thay đổi & tải lại (hoặc lỗi nhập lý do) |
| 5 | `loop` Từng dòng `DON_HANG_CT` | `line` (sắp xếp `OrderBy(VariantId)`) | `OrderDecisions.cs:40-45` | `UPDATE TON_KHO SET Reserved -= Qty WHERE Reserved >= Qty`; `INSERT NHAT_KY_KHO` (`Kind="HUY_GIU"`, `DeltaReserved=-Qty`) | — |
| 6 | Service ↔ CSDL | `order.Id`, `reason` | `OrderDecisions.cs:46`; `BusinessKernel.cs:63` | `UPDATE DON_HANG SET Status='DA_HUY'`, `INSERT LICH_SU_DON`, `CommitAsync` | `true` |
| 7 | Controller → Khách | `form.Id` | `OrdersController.cs:51-53` | Gán `TempData["Notice"]` đã hoàn giữ hàng | `RedirectToAction("Details", new { id = form.Id })` |

---

### 2.6.5 Sequence UC05 — Đăng nhập quản trị WinForms
- **Participants thật:** `Quản trị viên` → `LoginForm` (`src/PhoneStore.Admin/LoginForm.cs`) → `Admin API (Program.cs)` (`src/PhoneStore.Web/Program.cs` via `ApiClient`) → `AccountSessions` (`src/PhoneStore.Infrastructure/Accounts/AccountSessions.cs`) → `CSDL (TAI_KHOAN, PHIEN)`.

| Bước | Chiều gọi | Dữ liệu đầu vào | Vị trí mã nguồn | Xử lý & Tác động CSDL / Lock | Dữ liệu trả về |
| :---: | :--- | :--- | :--- | :--- | :--- |
| 1 | Admin → LoginForm | `UsernameBox.Text`, `PasswordBox.Text` | `LoginForm.cs:64-69` | Kiểm tra không rỗng và xóa ngay `PasswordBox` (`L68`) | Gọi `api.ReadinessAsync` |
| 2 | LoginForm → Admin API | `GET /health/ready` | `ApiClient.cs:30-37`; `Program.cs:223-235` | Kiểm tra `DatabaseMigrator.IsCurrentAsync` và `BackendIdentity.Fingerprint` | `{ status: "ready", environment, fingerprint }` (hoặc 503) |
| 3 | `alt` E1 (Máy chủ không khớp) | `settings.ExpectedEnvironment` / `ExpectedFingerprint` lệch hoặc URL không hợp lệ | `ApiClient.cs:23, 33-36`; `LoginForm.cs:70` | `ServerAddress.Validate` (`INVALID_SERVER`) hoặc `ReadinessAsync` (`SERVER_MISMATCH`) | Hiển thị lỗi chặn đăng nhập trên `MessageLabel` |
| 4 | LoginForm → Admin API | `GET /api/security/token` | `ApiClient.cs:208-212`; `Program.cs:207-211` | `antiforgery.GetAndStoreTokens(context)` | JSON `{ token }` + Cookie `PhoneStore.Antiforgery` vào `CookieContainer` RAM |
| 5 | LoginForm → Admin API | `POST /api/auth/login` + Header `X-CSRF-TOKEN` + `LoginInput(username, password, "admin")` | `ApiClient.cs:40-52, 208-224`; `Program.cs:142-157` | Validate Antiforgery + đảm bảo chưa có cookie khác đang đăng nhập | Gọi `IAccountSessions.LoginAsync` |
| 6 | API → AccountSessions ↔ CSDL | `LoginRequest(..., Surface: "admin")` | `AccountSessions.cs:18-52` | Mở `Serializable` + `sp_getapplock Exclusive`; đọc `TAI_KHOAN`, `VerifyHashedPassword`, kiểm tra `Active && Role == "ADMIN"` | `AccountSession` hoặc ném `UNAUTHORIZED` |
| 7 | `alt` E2 (Từ chối) | Sai mật khẩu / khóa / là `CUSTOMER` | `AccountSessions.cs:39-40`; `ApiClient.cs:56`; `LoginForm.cs:70` | API trả 401; `ApiClient` đổi thành `LOGIN_FAILED` (401) & clear cookie | Hiển thị thông báo từ chối đăng nhập |
| 8 | API → LoginForm → Admin | `AdminIdentity { Id, Username, Role, ExpiresAt }` + `Set-Cookie: PhoneStore.Session` | `Program.cs:148-156`; `ApiClient.cs:53, 256-264`; `Program.cs (Admin):44-49` | Lưu cookie trong RAM (`CookieContainer`), đặt `api.Identity`, phát `Authenticated` | Đóng `LoginForm`, mở `MainForm` (màn hình Tổng quan) |

---

### 2.6.6 Sequence UC06 — Thêm mới / chỉnh sửa sản phẩm & biến thể
- **Participants thật:** `Quản trị viên` → `ProductEditor` (`src/PhoneStore.Admin/ProductEditor.cs`) → `ProductEndpoints` (`src/PhoneStore.Web/Endpoints/ProductEndpoints.cs`) → `ProductService` (`src/PhoneStore.Infrastructure/Commerce/ProductService.cs`) → `CSDL (SAN_PHAM, BIEN_THE, TON_KHO, SAN_PHAM_ANH)`.

| Bước | Chiều gọi | Dữ liệu đầu vào | Vị trí mã nguồn | Xử lý & Tác động CSDL / Lock | Dữ liệu trả về |
| :---: | :--- | :--- | :--- | :--- | :--- |
| 1 | Admin → ProductEditor | `ProductRequest` (Code, Name, BrandId, CategoryId, Active, Variants, Images) | `ProductEditor.cs:135, 161-167` | Kiểm tra client qua `CatalogForms.Validate` (`Catalog.cs:58-68`), xác nhận nếu đổi `Active` (`L166`) | Gọi `CatalogWorkspace.Save` |
| 2 | ProductEditor → Endpoints | `POST /api/admin/products` hoặc `PUT /api/admin/products/{id}` (+ `X-CSRF-TOKEN` + Cookie) | `Catalog.cs:95-100`; `ProductEndpoints.cs:12-18` | `CommerceCsrfFilter` kiểm tra CSRF + `RequireAuthorization("Admin")` | Gọi `ProductService.Save(actor, id, input)` |
| 3 | Endpoints → ProductService ↔ CSDL | `Actor`, `id`, `ProductInput` | `ProductService.cs:22-35` | `BusinessKernel.Write` (`Serializable` + `sp_getapplock Exclusive`); kiểm tra phiên ADMIN, `HANG`/`DANH_MUC` hợp lệ, đủ biến thể cũ, khớp `Version` | Dữ liệu hiện có & kết quả kiểm tra |
| 4 | `alt` E1 (Lỗi nghiệp vụ/SQL) | Lệch `Version`, thiếu biến thể cũ, trùng `Code`/`SKU`/tổ hợp `(Màu, GB)`, sai tỷ lệ/ảnh | `ProductService.cs:24-62`; `BusinessKernel.cs:67-76` | `BusinessKernel.Write` rollback toàn bộ; trả HTTP 400 / 409 JSON `ApiError` | `ProductEditor.SaveAsync` (`L167`) giữ nguyên ô nhập và báo lỗi |
| 5 | Service ↔ CSDL | `Product` (`SAN_PHAM`) | `ProductService.cs:35-38` | Thêm mới hoặc cập nhật `SAN_PHAM` | `p.Id` |
| 6 | `loop` Từng biến thể | `VariantInput` | `ProductService.cs:40-56` | Mới: `Price = Rules.Price(Cost, ProfitPercent)` + tạo `TON_KHO(OnHand=0, Reserved=0)`; Cũ: giữ `Cost`, tính lại `Price` nếu đổi `%`, đánh dấu `IsModified` để tăng `Version` | Các `BIEN_THE` & `TON_KHO` |
| 7 | Service ↔ CSDL | `Images` (<= 12 URL) | `ProductService.cs:57-65`; `BusinessKernel.cs:63` | `ExecuteDeleteAsync` ảnh cũ & thêm `SAN_PHAM_ANH` mới, `CommitAsync` | `productId (long)` |
| 8 | Endpoints → ProductEditor | HTTP `201 Created { id }` hoặc `204 NoContent` | `ProductEndpoints.cs:12-18`; `Catalog.cs:100-101` | `CatalogWorkspace.Save` gọi tiếp `GET /api/admin/products/{id}` đối soát & đọc giá bán mới | `AdminProduct` → `ProductRecord` (kèm `Price` & `Version` mới) |
| 9 | ProductEditor → Admin | `ProductRecord` | `ProductEditor.cs:154-160, 167` | Gọi `Bind(product)` cập nhật bảng biến thể & đánh dấu `MarkClean` | Hiển thị giá bán và `Version` từ máy chủ |

---

### 2.6.7 Sequence UC07 — Quản lý hồ sơ & khóa/mở tài khoản khách hàng
- **Participants thật:** `Quản trị viên` → `CustomerDetailsForm / ProfileEditor` (`src/PhoneStore.Admin/CustomerForms.cs`) → `CustomerAdminEndpoints` (`src/PhoneStore.Web/Endpoints/CustomerAdminEndpoints.cs`) → `CustomerAdminService` (`src/PhoneStore.Infrastructure/Commerce/CustomerAdminService.cs`) → `CSDL (PHIEN, KHACH_HANG, TAI_KHOAN)`.

| Bước | Chiều gọi | Dữ liệu đầu vào | Vị trí mã nguồn | Xử lý & Tác động CSDL / Lock | Dữ liệu trả về |
| :---: | :--- | :--- | :--- | :--- | :--- |
| 1 | Admin → Forms | Sửa 4 trường hồ sơ (`CustomerProfileRequest`) hoặc nhấn Khóa/Mở (`ExpectedActive`, `Active`) | `CustomerForms.cs:35-39` hoặc `L76-83` | Validate hồ sơ (`ContactForms.ProfileError`) hoặc xác nhận qua `ConfirmationDialog` | Gọi `ContactsWorkspace.SaveProfile` / `SetCustomerState` |
| 2 | Forms → Endpoints | `PUT /api/admin/customers/{id}` hoặc `POST /api/admin/customers/{id}/state` (+CSRF) | `Contacts.cs:49-59`; `CustomerAdminEndpoints.cs:14-15` | `RequireAuthorization("Admin")` + `CommerceCsrfFilter` | Gọi `CustomerAdminService.Update` / `SetActive` |
| 3 | Service ↔ CSDL | `Actor`, `id`, input | `CustomerAdminService.cs:26-40` | `BusinessKernel.Write` (`Serializable` + `sp_getapplock Exclusive`); kiểm tra phiên `ADMIN` & đọc `KHACH_HANG` + `TAI_KHOAN (CUSTOMER)` (`Read` `L18-24`) | `AdminCustomer` hiện tại |
| 4 | `alt` E1 (Lỗi) | Không thấy khách (`NOT_FOUND`), trùng SĐT/Email (SQL `2601/2627`), hoặc `current.Active != ExpectedActive` (`CONCURRENT_UPDATE`) | `CustomerAdminService.cs:22, 36-37`; `BusinessKernel.cs:74` | Rollback toàn bộ; trả HTTP 400 / 404 / 409 | Forms (`CustomerForms.cs:39, 82`) hiển thị thông báo lỗi |
| 5 | `alt` Cập nhật hồ sơ | `ProfileChangeRequest` hợp lệ | `CustomerAdminService.cs:28-30` | Cập nhật `FullName`, `Phone`, `Email`, `Address` trên `KHACH_HANG` & `CommitAsync` | `true` → HTTP 204 → `GET /api/admin/customers/{id}` |
| 6 | `alt` Khóa / Mở khóa | `CustomerStateInput(Active, ExpectedActive)` | `CustomerAdminService.cs:38-40` | Cập nhật `TAI_KHOAN.Active = input.Active` | — |
| 7 | `opt` Nếu Khóa (`Active = false`) | `accountId = id` | `CustomerAdminService.cs:39` | `DELETE FROM PHIEN WHERE AccountId = @id` (`ExecuteDeleteAsync`) thu hồi ngay mọi phiên | `CommitAsync` → `true` |
| 8 | Endpoints → Forms → Admin | HTTP 204 → `GET /api/admin/customers/{id}` (`AdminCustomer`) | `Contacts.cs:60-61`; `CustomerForms.cs:38, 81` | Tải lại bản ghi xác nhận từ máy chủ và cập nhật UI | Hiển thị hồ sơ/trạng thái mới; lịch sử đơn cũ giữ nguyên |

---

### 2.6.8 Sequence UC08 — Xác nhận phiếu nhập kho
- **Participants thật:** `Quản trị viên` → `PurchaseEditor` (`src/PhoneStore.Admin/PurchaseForms.cs`) → `PurchaseEndpoints` (`src/PhoneStore.Web/Endpoints/PurchaseEndpoints.cs`) → `PurchaseService` (`src/PhoneStore.Infrastructure/Commerce/PurchaseConfirmation.cs`) → `CSDL (PHIEU_NHAP, BIEN_THE, TON_KHO, NHAT_KY_KHO)`.

| Bước | Chiều gọi | Dữ liệu đầu vào | Vị trí mã nguồn | Xử lý & Tác động CSDL / Lock | Dữ liệu trả về |
| :---: | :--- | :--- | :--- | :--- | :--- |
| 1 | Admin → PurchaseEditor | Nhấn "Xác nhận phiếu" & đồng ý trên `ConfirmationDialog` | `PurchaseForms.cs:93-102` | Tạo `PurchaseOperation(Current, "confirm")` mang `Version` & `OperationKey` | Gọi `SupplyWorkspace.Execute` (`Supply.cs:104-109`) |
| 2 | PurchaseEditor → Endpoints | `POST /api/admin/purchases/{id}/confirm` + `PurchaseTransition(Version, OperationKey)` (+CSRF) | `PurchaseEndpoints.cs:21-22` | `RequireAuthorization("Admin")` + `CommerceCsrfFilter` | Gọi `PurchaseService.Confirm(actor, id, command)` |
| 3 | Service ↔ CSDL | `Actor`, `id`, `VersionedCommand` | `PurchaseConfirmation.cs:9-19` | `BusinessKernel.Write` (`Serializable` + `sp_getapplock Exclusive`); kiểm tra phiên `ADMIN`, `PHIEU_NHAP` (`NHAP`, `Version`), `PHIEU_NHAP_CT` (sắp xếp `VariantId`), `NHA_CUNG_CAP` (`Active`), `Total`, và `OperationKey` | `Purchase` & `PurchaseLines` |
| 4 | `alt` E1 (Lỗi chốt phiếu) | Phiếu rỗng, NCC ngừng, tổng lệch, lệch `Version`, khác `NHAP`, hoặc `OperationKey` lặp | `PurchaseConfirmation.cs:12-19, 26` | `BusinessKernel.Write` rollback; trả HTTP 400 / 404 / 409 | `PurchaseForms.cs:102` báo lỗi; tồn và giá giữ nguyên |
| 5 | `loop` Từng dòng `PHIEU_NHAP_CT` | `line` (theo `VariantId` tăng dần) | `PurchaseConfirmation.cs:20-29` | `UPDATE TON_KHO SET OnHand += Qty WHERE OnHand <= limit`; gán `variant.Cost = line.Cost`, `variant.Price = Rules.Price(Cost, ProfitPercent)`; `INSERT NHAT_KY_KHO` (`Kind="NHAP"`, `OperationKey`) | Tồn thực tăng, giá vốn/bán cập nhật |
| 6 | Service ↔ CSDL | `receipt` | `PurchaseConfirmation.cs:30`; `BusinessKernel.cs:63` | `Status = "DA_XAC_NHAN"`, `ConfirmerId = actor.AccountId`, `ConfirmedAt = Now`, `CommitAsync` | `true` → HTTP 204 |
| 7 | Endpoints → PurchaseEditor | `GET /api/admin/purchases/{id}` + Stock & Product Readback | `Supply.cs:88-97, 107`; `PurchaseForms.cs:67-72, 101` | Tải `PurchaseReadback` gồm `PurchaseRecord` và danh sách `StockPrice` mới nhất | Hiển thị phiếu `Đã xác nhận`, khóa các nút sửa & hiện bảng tồn/giá mới |

---

### 2.6.9 Sequence UC09 — Xử lý vòng đời đơn hàng quản trị
- **Participants thật:** `Quản trị viên` → `OrderDetailForm` (`src/PhoneStore.Admin/OrderForms.cs`) → `OrderDecisionEndpoints / OrderFulfillmentEndpoints` → `OrderWorkflow` (`OrderDecisions.cs` & `OrderFulfillment.cs`) → `CSDL (DON_HANG, TON_KHO, THANH_TOAN, NHAT_KY_KHO, LICH_SU_DON)`.

| Bước | Chiều gọi | Dữ liệu đầu vào | Vị trí mã nguồn | Xử lý & Tác động CSDL / Lock | Dữ liệu trả về |
| :---: | :--- | :--- | :--- | :--- | :--- |
| 1 | Admin → OrderDetailForm | Chọn `OrderAction` (`Confirm`, `Cancel`, `Dispatch`, `Complete`, `Fail`, `ReceiveReturn`) + `ReasonBox.Text` | `OrderForms.cs:59-64` | Kiểm tra lý do bắt buộc nếu `Cancel`/`Fail` (`Orders.cs:40`), xác nhận qua `ConfirmationDialog` | Tạo `OrderOperation` (kèm `Snapshot.Version`, `Intent.Key`) |
| 2 | Form → Endpoints | `POST /api/admin/orders/{id}/{action}` (`confirm`/`cancel`/`dispatch`/`complete`/`fail`/`receive-return`) | `OrderDecisionEndpoints.cs:13-14`; `OrderFulfillmentEndpoints.cs:11-14` | `RequireAuthorization("Admin")` + `CommerceCsrfFilter` | Gọi `OrderWorkflow.<Action>(actor, id, command)` |
| 3 | Service ↔ CSDL | `Actor`, `id`, `VersionedCommand` | `OrderDecisions.cs:9-26` | `BusinessKernel.Write` (`Serializable` + `sp_getapplock Exclusive`); `Load` kiểm tra `ADMIN`, `Version`, tổng khớp; `Unpaid` kiểm tra `COD/CHUA_THU` | `Order` & `OrderLines` & `Payment` |
| 4 | `alt` E1 (Lỗi chuyển trạng thái) | Sai trạng thái nguồn, lệch `Version`, thiếu lý do, lặp `OperationKey` hoặc đã `NHAN_LAI` | `OrderDecisions.cs:11-43`; `OrderFulfillment.cs:12-49`; `OrderForms.cs:67` | Rollback giao dịch; trả HTTP 400 / 409 | `OrderDetailForm` tự gọi `Resolve` tải trạng thái mới nhất & báo lỗi |
| 5a | `alt` Confirm | `Status == "CHO_XAC_NHAN"` | `OrderDecisions.cs:28-33` | `order.Status = "DA_XAC_NHAN"` | — |
| 5b | `alt` Cancel | `Status in ("CHO_XAC_NHAN", "DA_XAC_NHAN")` | `OrderDecisions.cs:35-47` | Trừ `TON_KHO.Reserved`; ghi `NHAT_KY_KHO` (`HUY_GIU`); `Status = "DA_HUY"` | — |
| 5c | `alt` Dispatch | `Status == "DA_XAC_NHAN"` | `OrderFulfillment.cs:9-21` | Trừ đồng thời `TON_KHO.OnHand` & `Reserved`; ghi `NHAT_KY_KHO` (`XUAT_GIAO`); `Status = "DANG_GIAO"` | — |
| 5d | `alt` Complete | `Status == "DANG_GIAO"` | `OrderFulfillment.cs:23-31` | `THANH_TOAN` → `DA_THU` (`Amount=Total`, `ActorId`, `CollectedAt`); `Status = "HOAN_TAT"`, `CompletedAt` | — |
| 5e | `alt` Fail | `Status == "DANG_GIAO"` | `OrderFulfillment.cs:33-39` | `Status = "GIAO_THAT_BAI"` kèm lý do (chưa cộng tồn kho) | — |
| 5f | `alt` ReceiveReturn | `Status == "GIAO_THAT_BAI"` & chưa có `NHAN_LAI` | `OrderFulfillment.cs:41-55` | Cộng `TON_KHO.OnHand`; ghi `NHAT_KY_KHO` (`NHAN_LAI`); giữ `Status = "GIAO_THAT_BAI"` & đánh dấu modified để tăng `Version` | — |
| 6 | Service ↔ CSDL | `order`, `from`, `reason?` | `OrderDecisions.cs:22`; `BusinessKernel.cs:63` | `INSERT LICH_SU_DON` và `CommitAsync` | `true` → HTTP 204 |
| 7 | Endpoints → Form → Admin | HTTP 204 → `GET /api/admin/orders/{id}` (`AdminOrder`) | `Orders.cs:67`; `OrderForms.cs:51, 64` | Đọc lại `AdminOrder` mới nhất & gọi `Bind(row)` | Cập nhật trạng thái đơn, lịch sử & ẩn/hiện nút thao tác tiếp theo |

---

### 2.6.10 Sequence UC10 — Xem báo cáo doanh thu, bán chạy, tồn kho
- **Participants thật:** `Quản trị viên` → `ReportScreen` (`src/PhoneStore.Admin/ReportScreen.cs`) → `ReportEndpoints` (`src/PhoneStore.Web/Endpoints/ReportEndpoints.cs`) → `ReportService` (`src/PhoneStore.Infrastructure/Commerce/ReportService.cs`) → `CSDL (TON_KHO, BIEN_THE, SAN_PHAM, DON_HANG, DON_HANG_CT, THANH_TOAN)`.

| Bước | Chiều gọi | Dữ liệu đầu vào | Vị trí mã nguồn | Xử lý & Tác động CSDL / Lock | Dữ liệu trả về |
| :---: | :--- | :--- | :--- | :--- | :--- |
| 1 | Admin → ReportScreen | Chọn `KindBox` (`revenue` / `bestsellers` / `stock`), `FromBox`, `ToBox`, `MonthlyBox` | `ReportScreen.cs:52-59, 67-69` | Nếu `stock` thì vô hiệu `FromBox`/`ToBox`; kiểm tra ngày qua `ReportPresentation.DateError` (`Reports.cs:15`) | Chặn ngay tại `DateErrorLabel` nếu ngày sai |
| 2 | ReportScreen → Endpoints | `GET /api/admin/reports?kind=...(&from=...&to=...)` | `Reports.cs:35-38`; `ReportEndpoints.cs:7` | `RequireAuthorization("Admin")` + `CommerceCsrfFilter` (`Cache-Control: no-store`) | Gọi `ReportService.Read(actor, kind, from, to)` |
| 3 | Endpoints → ReportService | `Actor`, `kind`, `from?`, `to?` | `ReportService.cs:8-10` | `BusinessKernel.Read` (`Serializable` + `sp_getapplock Shared`) + `kernel.Require(actor, "ADMIN")` | — |
| 4 | `alt` E1 (Chưa xác thực / Tham số sai) | Chưa đăng nhập (`UNAUTHORIZED`), `kind` lạ hoặc `from > to` | `ReportService.cs:10, 16-17`; `DateWindow.cs:9, 17` | Ném `UNAUTHORIZED` (401) hoặc `INVALID_INPUT` (400) | `ReportScreen.LoadAsync` (`L72`) xóa bảng/biểu đồ và hiện lỗi |
| 5a | `alt` `kind == "stock"` | — | `ReportService.cs:11-15` | `SELECT TON_KHO JOIN BIEN_THE JOIN SAN_PHAM` tính `Available = OnHand - Reserved` | `StoreReport` chứa `List<StockReportLine>` và tổng `OnHand` |
| 5b | `alt` `revenue` / `bestsellers` | `DateWindow(Start, End)` (UTC+7 → UTC) | `ReportService.cs:17-25` | Đọc `DON_HANG (HOAN_TAT)` join `THANH_TOAN (COD, DA_THU, Amount == Total)` theo `CompletedAt`; gom `DailyRevenue` (lấy header đơn tránh nhân bội theo dòng) & `Bestseller` (join `DON_HANG_CT`, `BIEN_THE`, `SAN_PHAM`, sắp giảm dần theo `Quantity`) | `StoreReport` chứa `Revenue`, `Quantity`, `Orders`, `Daily`, `Bestsellers` |
| 6 | Service → Endpoints → Screen | `StoreReport` → HTTP 200 JSON → `AdminReport` | `ReportEndpoints.cs:7`; `ReportScreen.cs:71` | `ReportPresentation.Project(Current, MonthlyBox.Checked)` (`Reports.cs:19-29`) | `ReportProjection { Source, Rows, Points }` |
| 7 | ReportScreen → Admin | `ReportProjection` | `ReportScreen.cs:60-66, 6-39` | `RenderPage` nạp tối đa 20 dòng lên `DataGridView` và truyền cùng lát dữ liệu sang `ReportChart.Bind` | Bảng số liệu & biểu đồ cột đồng bộ 1 nguồn từ máy chủ |

---

## 5. Danh mục tệp mã nguồn đã chú thích tiếng Việt

Toàn bộ **133 tệp `.cs`** thuộc 5 dự án trong `src/` đều đã được bổ sung chú thích tiếng Việt trực tiếp (sử dụng chú thích một dòng `//`, giữ nguyên toàn bộ mã thực thi và vượt qua `dotnet build PhoneStore.slnx` với **0 Warning, 0 Error**):

| Dự án | Số tệp đã chú thích | Phạm vi trách nhiệm chính |
| :--- | :---: | :--- |
| `src/PhoneStore.Core/` | 21 | Hợp đồng giao diện (`IStoreService`), `BusinessException`, chính sách phiên `SessionPolicy` và các DTO tài khoản/thương mại |
| `src/PhoneStore.Infrastructure/` | 33 | `Accounts/` (đăng ký, phiên, hồ sơ, mật khẩu), `Commerce/` (20 dịch vụ nghiệp vụ, `BusinessKernel`, `Rules`, `DateWindow`), `Data/` (`Entities`, `StoreDbContext`, `DatabaseMigrator`, `DataAudit`, `DemoSeeder`) |
| `src/PhoneStore.Web/` | 44 | `Program.cs`, `ApiError`, `BackendIdentity`, `Security/`, 5 MVC `Controllers/`, 16 API `Endpoints/`, 11 lớp `Storefront/`, `ProductImages/`, 5 `Models/` |
| `src/PhoneStore.Admin.Client/` | 8 | `ApiClient`, `ApiErrors`, `Contracts`, `Catalog`, `Contacts`, `Orders`, `Reports`, `Supply` |
| `src/PhoneStore.Admin/` | 27 | `Program.cs`, `LoginForm`, `MainForm`, `DashboardView`, toàn bộ form quản trị sản phẩm/khách/NCC/nhập kho/tồn kho/đơn hàng/hóa đơn/báo cáo & `Navigation/` |
