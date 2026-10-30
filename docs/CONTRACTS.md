# Data and service contracts

This is an implementation contract for the clean rebuild, derived from report sections 1.5.2.1, 2.3.12, 2.4 and appendix A. All K01-K15/A01-A22 server flows are implemented. See [accounts](ACCOUNTS.md), [commerce contracts](COMMERCE.md), [database operations and sample manifest](DATABASE.md) and [merged verification evidence](TRACEABILITY.md). UI-only rendering and printer actions remain deferred.

## Persistence

Use a new, explicitly configured SQL Server database. Never run this lineage against legacy `PhoneStore` data. Versioned embedded SQL migrations apply only through an explicit command, with a transaction, migration lock and SHA-256 ledger. Refuse an unmanaged nonempty schema, a foreign lineage or altered applied script. Normal server startup never migrates or seeds. The first new migration includes Cost/ProfitPercent; it is not the legacy migration 003 or a backfill of old sales.

The 18 application tables below and one technical migration ledger retain the source names. All foreign keys use NO ACTION. Do not physically delete historical business records. The service creates inventory with a variant, customer profile with a CUSTOMER account, and payment with a successful order; a foreign key alone does not guarantee the child exists.

| Table | Key and main fields / relationships |
| --- | --- |
| TAI_KHOAN | bigint Id; unique Username, PasswordHash, Role (CUSTOMER/ADMIN), Active, CreatedAt |
| KHACH_HANG | Id is account PK/FK; FullName, unique Phone, optional unique Email, Address |
| PHIEN | GUID Id, AccountId FK, ExpiresAt; expiry/revocation checked for each authenticated request |
| HANG / DANH_MUC | int Id, unique Name, Active |
| SAN_PHAM | bigint Id; unique Code, Name, BrandId/CategoryId FKs, Specifications, Description, ImageUrl, Active |
| SAN_PHAM_ANH | bigint Id; ProductId FK, HTTPS or safe local /images/ Url, SortOrder; computed SHA-256 UrlHash for unique ProductId/URL indexing without exceeding SQL Server key width |
| BIEN_THE | bigint Id; ProductId FK, unique Sku; Color, StorageGb, Cost, ProfitPercent, Price, Active, Version; unique ProductId/Color/StorageGb |
| TON_KHO | VariantId PK/FK; OnHand, Reserved, Version; check 0 <= Reserved <= OnHand |
| GIO_HANG_CT | CustomerId/VariantId compound PK and FKs; Quantity, UpdatedAt; no reservation |
| DON_HANG | bigint Id; unique Code/RequestKey; CustomerId FK; Recipient, Phone, Address, Status, Total, CreatedAt, CompletedAt, Version |
| DON_HANG_CT | bigint Id; OrderId/VariantId FKs; Name, Sku, Description, Quantity, Price snapshots; unique OrderId/VariantId |
| THANH_TOAN | OrderId PK/FK; Method=COD, Status, Amount, CollectedAt, optional ActorId FK; unpaid amount zero, paid amount positive with collector/time |
| LICH_SU_DON | bigint Id; OrderId/ActorId FKs; optional From, To, At, optional Reason |
| NHA_CUNG_CAP | bigint Id; unique Code, Name, Phone, Address, Active |
| PHIEU_NHAP | bigint Id; unique Code; SupplierId/CreatorId/optional ConfirmerId FKs; Status, Total, CreatedAt, ConfirmedAt, Version |
| PHIEU_NHAP_CT | bigint Id; PurchaseId/VariantId FKs; Quantity, Cost; unique PurchaseId/VariantId |
| NHAT_KY_KHO | bigint Id; VariantId/ActorId FKs; optional OrderId/PurchaseId, Kind, DeltaOnHand, DeltaReserved, At, optional Reason, OperationKey; never both document FKs |
| __PhoneStoreMigrations | unique Id, SHA-256, AppliedAt; technical ledger, no business actor |

### Types and constraints

Source-backed conventions: SQL money columns `decimal(18,2)`; percentage range 0-10000 inclusive; physical/reserved quantities are nonnegative integers; quantity and purchase cost are positive; variant/inventory/order/purchase Version is SQL Server rowversion. Timestamps represent UTC. The service calculates price with decimal and `MidpointRounding.AwayFromZero`, validates the rounded result fits its column and is positive, and never changes existing order snapshots.

Implementation choices for the fresh schema: use `decimal(7,2)` for percentage (two fractional digits), `datetime2(7)` for UTC timestamps, bigint business keys, int lookup keys, GUID request/operation/session keys. Persisted username/code/SKU normalization happens in services; uniqueness is also enforced by SQL. Required text limits follow column capacity: username 100, hash 1000, full name 120, phone 20, email 254, address 500, product code 30, product name 200, SKU/color 50, specifications 20000, description 50000, image URL 1000, reason 1000. These are storage/input capacity decisions, not claims of additional source business rules.

Enforce FK/unique/check constraints on SQL as defense in depth. Optional email uses a filtered unique index. Journal uniqueness covers operation/variant, each order/variant/kind, and each purchase/variant/kind. The new internal movement vocabulary is NHAP/GIU/HUY_GIU/XUAT_GIAO/NHAN_LAI/DIEU_CHINH; these are implementation labels for the source actions. Movement signs depend on the action and must be checked by services; SQL also rejects a movement whose physical and reserved deltas are both zero. No aggregate or cross-table payment/order consistency is falsely attributed to a CHECK constraint. Long image URLs use a persisted SHA-256 index key because a full 1000-character URL exceeds SQL Server's index key limit; services still validate and compare complete URLs.

## Shared API conventions

- JSON uses camelCase. IDs are numbers; money and percentages are decimal JSON numbers, never client-authoritative totals. Versions are Base64-encoded 8-byte SQL rowversions. A missing/malformed required version is 400; a correctly formed stale version is 409.
- Authenticated actor/role/session are derived from protected cookies and the stored session, not request IDs. WinForms uses a CookieContainer and the same API. No token/password goes into localStorage. Every cookie-authenticated mutation, including login/registration/logout, requires the anti-forgery token from `GET /api/security/token` in `X-CSRF-TOKEN`.
- Pages use one-based `page` and bounded `pageSize`; technical defaults are 20/max 100. Responses contain `{items,total,page,pageSize}` and stable ordering with an ID tiebreaker. A valid empty query succeeds with an empty list, while network/server/authorization errors remain errors.
- Date filters use ISO `yyyy-MM-dd` in UTC+7. Convert an inclusive local from-date and the day after an inclusive to-date to an exclusive UTC interval. Reject reversed/unrepresentable intervals. Stock is current and ignores sales periods. General reports accept explicit periods; homepage bestseller defaults remain undecided.
- Errors use `{code,message,traceId}` (PRICE_CHANGED additionally supplies `details` containing the current cart) with Vietnamese messages. 400 INVALID_INPUT, 401 UNAUTHORIZED, 403 FORBIDDEN, 404 NOT_FOUND, 409 CONCURRENT_UPDATE/INVALID_STATE/OUT_OF_STOCK/PRICE_CHANGED, 503 DATABASE_UNAVAILABLE and a generic 500 SERVER_ERROR. Nonowners receive 404 for order detail/mutations. PRICE_CHANGED must also return the current cart/price data required for explicit customer reconfirmation, without committing an order.

## Service and route map

All routes below are implemented; see [account contracts](ACCOUNTS.md) and [commerce contracts](COMMERCE.md) for exact query fields, payloads and response semantics.

All write rows require validation, anti-forgery protection and the indicated service transaction. ADMIN can never be publicly registered. A CUSTOMER cannot access internal catalog/purchasing/stock/reports merely by changing query flags.

| Source | Service / HTTP operation | Role / record scope |
| --- | --- | --- |
| K01 | Account: POST `/api/auth/register` with username/password/fullName/phone/optional email/address -> 201 `{id,username,role}`, no login | Guest; role always CUSTOMER |
| K02/A01 | Account: POST `/api/auth/login` with `{username,password,surface}` -> protected session cookie, safe user/expiry; GET `/api/auth/session` and `/api/admin/session` query current identity | Login anonymous; selected active role; queries require live session/ADMIN respectively |
| K03/A02 | Account: POST `/api/auth/logout` -> 204, revoke only current session and clear cookie | Current signed-in actor only; requires CSRF |
| K04 | Account: GET/PUT `/api/account/profile` -> safe own profile / 204 on update; input fullName/phone/email/address only | CUSTOMER self; writes require CSRF |
| K05/A03 | Account: POST `/api/account/password` with currentPassword/newPassword -> 204, atomically replace hash and revoke all related sessions/cookie | CUSTOMER/ADMIN self; requires live session and CSRF |
| K06-K09 | Catalog: GET `/api/catalog`, `/api/catalog/{id}`, `/api/lookups/{kind}` -> public products/variants/filters | Guest/CUSTOMER, active public records |
| A04-A07 | Catalog: GET/POST `/api/admin/products`, GET/PUT `/api/admin/products/{id}`; product edits include every retained variant/version | ADMIN internal records |
| A08 | Catalog: GET/POST `/api/admin/lookups/{kind}`, PUT `/api/admin/lookups/{kind}/{id}` | ADMIN, brands/categories only |
| A09-A11 | Account administration: GET `/api/admin/customers`, GET/PUT `/api/admin/customers/{id}`; explicit lock/unlock action revokes on lock | ADMIN, selected CUSTOMER |
| A12 | Order queries: GET `/api/admin/customers/{id}/orders` | ADMIN, selected existing customer |
| A13 | Purchasing: GET/POST `/api/admin/suppliers`, PUT `/api/admin/suppliers/{id}` | ADMIN |
| A14 | Purchasing: GET/POST `/api/admin/purchases`, GET/PUT `/api/admin/purchases/{id}` | ADMIN, edits only draft/version |
| A15 | Purchasing: POST `/api/admin/purchases/{id}/confirm` or `/cancel`, with version/operationKey | ADMIN, valid draft only |
| A16/A17 | Inventory: GET `/api/admin/stock`, `/api/admin/stock/movements`; POST `/api/admin/stock/{variantId}/adjust` with counted/reason/version/operationKey | ADMIN |
| K10/K11 | Cart: POST `/api/cart/preview` for untrusted guest inputs; GET/POST/PUT `/api/cart`; POST `/api/cart/merge` | Preview public; stored cart CUSTOMER self |
| K12 | Orders: POST `/api/orders` with requestKey, recipient/phone/address, COD and confirmed variant/quantity/price lines -> owned order snapshot | CUSTOMER, own persisted cart |
| K13/K14 | Orders: GET `/api/orders`, `/api/orders/{id}` | CUSTOMER own orders only |
| K15 | Orders: POST `/api/orders/{id}/cancel` with version/operationKey/reason | CUSTOMER, own pending order |
| A18/A19/A20 | Orders: GET `/api/admin/orders`, `/api/admin/orders/{id}`; POST `/api/admin/orders/{id}/{action}` | ADMIN; state/version/action validated |
| A21 | Invoice: GET `/api/admin/orders/{id}/invoice` -> immutable document data | ADMIN; confirmed/shipping/completed only; read only |
| A22 | Reports: GET `/api/admin/reports` with kind and relevant dates -> table/chart result | ADMIN; revenue/bestsellers/current-stock only |

## Command semantics

Account creation + profile are atomic. Password change and lock commit session revocation in the same transaction. Logout revokes only the current session. Unlock never revives an old session. Every authenticated request validates both stored expiry and active account.

K01 input policy retains the previously known validator behavior while replacing its implementation: trimmed/lowercase username uses 3-100 Latin letters/digits or `_ . @ -`; password uses 10-128 characters with a letter and digit; phone has 9-15 digits and optional leading `+`, without guessing country-prefix equivalence. Name/phone/address are trimmed, email is optional and trimmed/lowercased, display-name email syntax is rejected, and storage capacity limits above apply. These are named technical validation policies, not extra original UC branches. Unknown JSON members, including role/passwordHash, are rejected. Missing/malformed bodies and failed CSRF produce structured INVALID_INPUT errors. Successful registration does not create a session or issue a login cookie. Login/session and authenticated-user cross-flow checks remain separate tasks.

Product creation takes cost and percentage per new variant, creates inventory at zero and calculates price. Editing existing variants never accepts an arbitrary cost/sale-price override; a changed percentage recomputes from persisted current cost. New cost comes from confirming a receipt. Preserve all old variants; disable rather than delete. Common product edits must advance/check the submitted versions of retained variants.

Purchase drafts may be empty; duplicate variant IDs are rejected before saving. Confirmation requires active supplier, positive quantities/costs, nonempty lines, current version and a valid computed price for every line. Confirming updates stock/cost/price/receipt/journal in one transaction exactly once; draft saves/cancellation never change stock or price. A finalized receipt cannot be edited/cancelled as a draft.

User-approved DEC.02 merge takes max(server, guest), caps at live availability, removes unavailable/discontinued/missing rows and returns `{cart,adjusted}` atomically. Duplicate guest IDs use max and repeated unchanged inputs do not accumulate quantities. The guest draft remains available if merge fails. Adding/updating carts does not reserve stock. At checkout compare the client's explicitly confirmed variant/quantity/price lines with the current persisted cart and live prices. PRICE_CHANGED commits nothing; the customer must confirm fresh values. Only then snapshot, reserve, create unpaid COD/history/journal and clear cart in one transaction.

A checkout RequestKey is globally unique. Retry by the same owner for the same request returns the existing order, including after a lost response; another owner cannot retrieve it. Reusing a committed key with different immutable checkout data is a conflict. After uncertain write responses, fetch current order/purchase/stock and reconcile before sending a new command/key. Transition/receipt/stock operation keys and versions prevent duplicated effects; a repeated finalized command may return a conflict with no additional side effect.

Order states: CHO_XAC_NHAN -> DA_XAC_NHAN -> DANG_GIAO -> HOAN_TAT or GIAO_THAT_BAI. Customer cancellation is pending-only; ADMIN can cancel pending/confirmed -> DA_HUY. Dispatch reduces physical and reserved together. Completion alone records DA_THU COD; confirmation remains CHUA_THU. Failure requires a reason and does not add stock. Physical return receipt after failure adds stock once while retaining failure status; journal evidence distinguishes already received returns. There is no post-purchase refund flow.

Writes must be correct across multiple server processes: use SQL transactions, SQL rowversion and conditional balance/state updates, backed by unique request/operation indexes. A transaction-owned application lock is a permitted source-backed initial strategy for this one-store scale; no throughput claim is made until measured. Domain unit tests alone never replace the SQL transaction/race tests.
